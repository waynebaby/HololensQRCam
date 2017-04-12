using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Graphics.Imaging;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace WebCamPhotoService.UWP.TestApp
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();
            
        }



        public ImageSource ImageSource
        {
            get { return (ImageSource)GetValue(ImageSourceProperty); }
            set { SetValue(ImageSourceProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ImageSource.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ImageSourceProperty =
            DependencyProperty.Register("ImageSource", typeof(ImageSource), typeof(MainPage), new PropertyMetadata(null));

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            using (var sps=new XamlControlPhotoService())
            {
                sps.SetCaptureElement(Capature);
                await sps.InitializeAsync();
                var photo=await sps.GetPhotoStreamAsync();
                var bms = new BitmapImage();
                var ra = new Windows.Storage.Streams.InMemoryRandomAccessStream();
                await photo.CopyToAsync(ra.AsStreamForWrite());
                
                await bms.SetSourceAsync(ra);
                Image.Source = bms;
                await sps.CleanupAsync();
            }
        }
    }
}
