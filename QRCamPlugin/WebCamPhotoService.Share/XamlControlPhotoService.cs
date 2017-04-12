using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Linq;
using System.IO;
#if WINDOWS_UWP
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml;
using Windows.Media;
using Windows.Graphics.Imaging;
using Windows.Media.MediaProperties;
using Windows.Devices.Enumeration;
using Windows.Media.Capture;
#endif
namespace WebCamPhotoService
{
    public class XamlControlPhotoService : IDisposable, IPhotoService
    {

        public XamlControlPhotoService()
        {

        }

#if WINDOWS_UWP
        CaptureElement _element;
        public void SetCaptureElement(CaptureElement element)
        {
            _element = element;
        }
#endif


        // Information about the camera device
        private bool _mirroringPreview = false;
        private bool _externalCamera = false;
        private int _photoWidth = 0;
        private int _photoHeight = 0;

        private bool _isInitialized = false;
        private bool _isPreviewing = false;
        private bool _isFailed = false;


#if WINDOWS_UWP

        Task _forCleanUp;
        Task _forInit;
        public void BeginInitialize()
        {
            _forInit?.Wait();
            _forInit = InitializeAsync();
        }
        public void BeginCleanup()
        {
            _forCleanUp?.Wait();
            _forCleanUp = CleanupAsync();
        }
        public void EndInitialize()
        {
            _forInit?.Wait();
            _forInit = null;
        }


        public void EndCleanup()
        {
            _forCleanUp?.Wait();
            _forCleanUp = null;
        }

        // MediaCapture and its state variables
        private MediaCapture _mediaCapture;

        /// <summary>
        /// Initializes the MediaCapture, registers events, gets camera device information for mirroring and rotating, and starts preview
        /// </summary>
        /// <returns></returns>
        public async Task InitializeAsync()
        {
            Debug.WriteLine("InitializeAsync");
            var tcs = new TaskCompletionSource<object>();

            var t = _element.Dispatcher.TryRunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal,
                async () =>
             {
                 try
                 {


                     if (_mediaCapture == null)
                     {
                        // Attempt to get the back camera if one is available, but use any camera device if not
                        var cameraDevice = await FindCameraDeviceByPanelAsync(Windows.Devices.Enumeration.Panel.Back);

                         if (cameraDevice == null)
                         {
                             Debug.WriteLine("No camera device found!");
                             return;
                         }

                        // Create MediaCapture and its settings
                        _mediaCapture = new MediaCapture();

                        // Register for a notification when something goes wrong
                        _mediaCapture.Failed += MediaCapture_Failed;

                         var settings = new MediaCaptureInitializationSettings { VideoDeviceId = cameraDevice.Id };

                        // Initialize MediaCapture
                        //try
                        //{
                        await _mediaCapture.InitializeAsync(settings);
                         IsInitialized = true;
                        //}
                        //catch (UnauthorizedAccessException)
                        //{
                        //    Debug.WriteLine("The app was denied access to the camera");
                        //}

                        // If initialization succeeded, start the preview
                        //if (_isInitialized)
                        //{
                        // Figure out where the camera is located
                        if (cameraDevice.EnclosureLocation == null || cameraDevice.EnclosureLocation.Panel == Windows.Devices.Enumeration.Panel.Unknown)
                         {
                            // No information on the location of the camera, assume it's an external camera, not integrated on the device
                            _externalCamera = true;
                         }
                         else
                         {
                            // Camera is fixed on the device
                            _externalCamera = false;

                            // Only mirror the preview if the camera is on the front panel
                            _mirroringPreview = (cameraDevice.EnclosureLocation.Panel == Windows.Devices.Enumeration.Panel.Front);
                         }

                         await StartPreviewAsync();
                        //}
                    }
                 }
                 catch (Exception ex)
                 {
                     tcs.TrySetException(ex);
                 }
                 tcs.TrySetResult(null);
         
            });
            await tcs.Task;
        }

    /// <summary>
    /// Starts the preview and adjusts it for for rotation and mirroring after making a request to keep the screen on and unlocks the UI
    /// </summary>
    /// <returns></returns>
    private async Task StartPreviewAsync()
    {
        Debug.WriteLine("StartPreviewAsync");


        //// Set the preview source in the UI and mirror it if necessary
        var PreviewControl = _element;
        PreviewControl.Source = _mediaCapture;
        PreviewControl.FlowDirection = _mirroringPreview ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;

        // Start the preview
        await _mediaCapture.StartPreviewAsync();
        IsPreviewing = true;


    }

    private async void MediaCapture_Failed(MediaCapture sender, MediaCaptureFailedEventArgs errorEventArgs)
    {
        Debug.WriteLine("MediaCapture_Failed: (0x{0:X}) {1}", errorEventArgs.Code, errorEventArgs.Message);

        await CleanupAsync();
        IsFailed = true;
        //TODO: Inform Exception;
    }

    /// <summary>
    /// Starts the preview and adjusts it for for rotation and mirroring after making a request to keep the screen on and unlocks the UI
    /// </summary>
    /// <returns></returns>

    public async Task CleanupAsync()
    {
        if (IsInitialized)
        {
            if (IsPreviewing)
            {
                // The call to stop the preview is included here for completeness, but can be
                // safely removed if a call to MediaCapture.Dispose() is being made later,
                // as the preview will be automatically stopped at that point
                //await StopPreviewAsync();
                this._mediaCapture.Dispose();
            }

            IsInitialized = false;
        }

        if (_mediaCapture != null)
        {
            _mediaCapture.Failed -= MediaCapture_Failed;
            _mediaCapture.Dispose();
            _mediaCapture = null;
        }
    }



    /// <summary>
    /// Queries the available video capture devices to try and find one mounted on the desired panel
    /// </summary>
    /// <param name="desiredPanel">The panel on the device that the desired camera is mounted on</param>
    /// <returns>A DeviceInformation instance with a reference to the camera mounted on the desired panel if available,
    ///          any other camera if not, or null if no camera is available.</returns>
    private static async Task<DeviceInformation> FindCameraDeviceByPanelAsync(Windows.Devices.Enumeration.Panel desiredPanel)
    {
        // Get available devices for capturing pictures
        var allVideoDevices = await DeviceInformation.FindAllAsync(DeviceClass.VideoCapture);

        // Get the desired camera by panel
        DeviceInformation desiredDevice = allVideoDevices.FirstOrDefault(x => x.EnclosureLocation != null && x.EnclosureLocation.Panel == desiredPanel);

        // If there is no device mounted on the desired panel, return the first device found
        return desiredDevice ?? allVideoDevices.FirstOrDefault();
    }

    /// <summary>
    /// Gets the current preview frame as a SoftwareBitmap, displays its properties in a TextBlock, and can optionally display the image
    /// in the UI and/or save it to disk as a jpg
    /// </summary>
    /// <returns></returns>
    public async Task<MemoryStream> GetPhotoStreamAsync()
    {

        TaskCompletionSource<MemoryStream> ms = new TaskCompletionSource<MemoryStream>();
            var noT = _element.Dispatcher.TryRunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal,
                 async () =>
                   {
                       Debug.WriteLine(_mediaCapture.CameraStreamState);
                       // Get information about the preview
                       var previewProperties = _mediaCapture.VideoDeviceController.GetMediaStreamProperties(MediaStreamType.VideoPreview) as VideoEncodingProperties;
                       Debug.WriteLine($"{nameof(previewProperties) }");
                       PhotoWidth = (int)previewProperties.Width;
                       PhotoHeight = (int)previewProperties.Height;
                       // Create the video frame to request a SoftwareBitmap preview frame

                       var videoFrame = new VideoFrame(BitmapPixelFormat.Bgra8, (int)previewProperties.Width, (int)previewProperties.Height);
                       Debug.WriteLine(_mediaCapture.CameraStreamState);
                       Debug.WriteLine($"{nameof(videoFrame) }");
                       var currentFrame = await _mediaCapture.GetPreviewFrameAsync(videoFrame);
                       Debug.WriteLine($"{nameof(_mediaCapture.GetPreviewFrameAsync) }");





                       using (currentFrame)
                       {
                           SoftwareBitmap previewFrame = currentFrame.SoftwareBitmap;

                           var stm= await SaveSoftwareBitmapAsync(previewFrame);
                           ms.SetResult(stm);


                       }

                   }            

            );
        return await ms.Task;
    }

    private static async Task<MemoryStream> SaveSoftwareBitmapAsync(SoftwareBitmap bitmap)
    {
        var rs = new Windows.Storage.Streams.InMemoryRandomAccessStream();
        var bf = new Windows.Storage.Streams.Buffer(1024 * 1024 * 10);
        bitmap.CopyToBuffer(bf);
        //var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.BmpEncoderId, rs);
        await rs.WriteAsync(bf);

        var ms = new MemoryStream();
        await rs.GetInputStreamAt(0).AsStreamForRead().CopyToAsync(ms);
        ms.Position = 0;
        return ms;
    }

#else

        public Task InitializeAsync()
        {
            return Task.Factory.StartNew(() => { });
        }
        public Task CleanupAsync()
        {
            return Task.Factory.StartNew(() => { });

        }
        public void BeginInitialize()
        {

        }
        public void EndInitialize()
        {

        }
        public void BeginCleanup()
        {

        }
        public void EndCleanup()
        {

        }
        public Task<MemoryStream> GetPhotoStreamAsync()
        {
            return Task.Factory.StartNew(() => new MemoryStream() );
        }


#endif

















    public bool IsInitialized { get => _isInitialized; private set => _isInitialized = value; }
    public bool IsPreviewing { get => _isPreviewing; private set => _isPreviewing = value; }
    public bool IsFailed { get => _isFailed; private set => _isFailed = value; }
    public bool IsDisposed { get => _isDisposed; private set => _isDisposed = value; }
    public bool ExternalCamera { get => _externalCamera; private set => _externalCamera = value; }
    public bool MirroringPreview { get => _mirroringPreview; private set => _mirroringPreview = value; }
    public int PhotoWidth { get => _photoWidth; private set => _photoWidth = value; }
    public int PhotoHeight { get => _photoHeight; private set => _photoHeight = value; }

    public event EventHandler<PhotoServiceStateChangedEventArg> StateChanged;
    private void RaiseItemChangedEvents()
    {
        StateChanged?.Invoke(this, new PhotoServiceStateChangedEventArg(this));
    }

    #region IDisposable Support
    private bool _isDisposed = false; // To detect redundant calls

    protected virtual void Dispose(bool disposing)
    {
        if (!IsDisposed)
        {
            if (disposing)
            {
                // TODO: dispose managed state (managed objects).
#if WINDOWS_UWP
                var t = CleanupAsync();
                t.Wait();
#endif
            }

            // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
            // TODO: set large fields to null.

            IsDisposed = true;
        }
    }

    // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
    // ~SimplePhotoService() {
    //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
    //   Dispose(false);
    // }

    // This code added to correctly implement the disposable pattern.
    public void Dispose()
    {
        // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        Dispose(true);
        // TODO: uncomment the following line if the finalizer is overridden above.
        // GC.SuppressFinalize(this);
    }



    #endregion


}

public class PhotoServiceStateChangedEventArg : EventArgs
{
    private bool _isInitialized;
    private bool _isPreviewing;
    private bool _isFailed;
    private bool _isDisposed;

    public PhotoServiceStateChangedEventArg(IPhotoService service)
    {

        IsInitialized = service.IsInitialized;
        IsPreviewing = service.IsPreviewing;
        IsFailed = service.IsFailed;
        IsDisposed = service.IsDisposed;
    }


    public bool IsInitialized { get => _isInitialized; private set => _isInitialized = value; }
    public bool IsPreviewing { get => _isPreviewing; private set => _isPreviewing = value; }
    public bool IsFailed { get => _isFailed; private set => _isFailed = value; }
    public bool IsDisposed { get => _isDisposed; private set => _isDisposed = value; }


}
}
