using System;
using Windows.ApplicationModel.Core;
using Windows.System.Threading;
using Windows.UI.Core;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;

namespace IoTDisplay
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private readonly PhotoReader _reader;

        private ThreadPoolTimer _timer1, _timer2;
        private bool _firstTimer;

        private string _clockText;

        public MainPage()
        {
            this.InitializeComponent();

            // create photo reader
            _reader = new PhotoReader(
                "http://my-cloud.kmb.home:8080", 
                "/Public/Shared%20Pictures/Slideshow"
                );

            // cycle images
            _firstTimer = true;
            _timer1 = ThreadPoolTimer.CreatePeriodicTimer(NextImage, TimeSpan.FromSeconds(8));

            // show clock
            _timer2 = ThreadPoolTimer.CreatePeriodicTimer(ShowTime, TimeSpan.FromSeconds(1));
        }

        private async void ShowTime(ThreadPoolTimer timer)
        {
            string nowText = DateTime.Now.ToString("h:mm");
            if (nowText == _clockText) return;

            _clockText = nowText;

            // we have to update UI in UI thread only
            await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(
                CoreDispatcherPriority.Normal, () => {
                    textTime.Text = _clockText;
                    textTimeShadow.Text = _clockText;
                });
        }

        private async void NextImage(ThreadPoolTimer timer)
        {
            if (_firstTimer)
            {
                timer.Cancel();
                _timer1 = null;

                _firstTimer = false;
            }

            // get next image
            var image = await _reader.GetImage();

            if (image != null)
            {
                // we have to update UI in UI thread only
                await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(
                    CoreDispatcherPriority.Normal, 
                    () => ShowImage(image)
                );
            }

            if (_timer1 == null)
                _timer1 = ThreadPoolTimer.CreatePeriodicTimer(NextImage, TimeSpan.FromSeconds(42));
        }

        private void ShowImage(string imageUri)
        {
            // create and load bitmap
            BitmapImage bitmap = new BitmapImage();
            bitmap.UriSource = new Uri(imageUri, UriKind.Absolute);

            // display image
            splashImage.Source = bitmap;
        }
    }
}
