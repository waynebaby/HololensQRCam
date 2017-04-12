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
    public class SimplePhotoService : IDisposable, IPhotoService
    {

        public SimplePhotoService()
        {

        }



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

            }
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
            // Get information about the preview
            var previewProperties = _mediaCapture.VideoDeviceController.GetMediaStreamProperties(MediaStreamType.VideoPreview) as VideoEncodingProperties;
            PhotoWidth = (int)previewProperties.Width;
            PhotoHeight = (int)previewProperties.Height;
            // Create the video frame to request a SoftwareBitmap preview frame

            var ms = new Windows.Storage.Streams.InMemoryRandomAccessStream();
            // Capture the preview frame
            await _mediaCapture.CapturePhotoToStreamAsync(
                         new ImageEncodingProperties
                         {
                             Height = (uint)PhotoHeight,
                             Width = (uint)PhotoWidth,
                             Subtype = "PNG"
                         },
                        ms);
            {
                // Collect the resulting frame
                //SoftwareBitmap previewFrame = new SoftwareBitmap(BitmapPixelFormat.Bgra8, PhotoWidth, PhotoHeight);

                // this is a PNG file so we need to decode it to raw pixel data.
                var bitmapDecoder = await BitmapDecoder.CreateAsync(ms);

                // grab the pixels in a byte[] array.
                var pixelProvider = await bitmapDecoder.GetPixelDataAsync();
                var bits = pixelProvider.DetachPixelData();
                var stm = new MemoryStream(bits);
                // Show the frame information
                //FrameInfoTextBlock.Text = String.Format("{0}x{1} {2}", previewFrame.PixelWidth, previewFrame.PixelHeight, previewFrame.BitmapPixelFormat);

                // Add a simple green filter effect to the SoftwareBitmap
                //if (GreenEffectCheckBox.IsChecked == true)
                //{
                //    ApplyGreenFilter(previewFrame);
                //}y

                // Show the frame (as is, no rotation is being applied)
                //if (ShowFrameCheckBox.IsChecked == true)
                //{
                // Create a SoftwareBitmapSource to display the SoftwareBitmap to the user
                //var sbSource = new SoftwareBitmapSource();
                //await sbSource.SetBitmapAsync(previewFrame);

                //    // Display it in the Image control
                //    PreviewFrameImage.Source = sbSource;
                //}
                //var stm = await SaveSoftwareBitmapAsync(previewFrame);
                return stm;
            }
        }

        private static async Task<MemoryStream> SaveSoftwareBitmapAsync(SoftwareBitmap bitmap)
        {
            var rs = new Windows.Storage.Streams.InMemoryRandomAccessStream();
            var bf = new Windows.Storage.Streams.Buffer(2u << 16);
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
    
}
