using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace PlayReadySample
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

        private void OnSmoothStreamButtonClicked(object sender, RoutedEventArgs e)
        {
            // You can get more streams (both protected and unprotected) here: http://playready.directtaps.net/smoothstreaming/
            this.Frame.Navigate(typeof(PlayerPage), new PlayerArguments { StreamUrl = "http://playready.directtaps.net/smoothstreaming/SSWSS720H264PR/SuperSpeedway_720.ism/Manifest", RightsManagerUrl = "http://playready.directtaps.net/pr/svc/rightsmanager.asmx" });
        }

        private void OnDashButtonClicked(object sender, RoutedEventArgs e)
        {
            // We don't need to specify rights manager url with this stream
            this.Frame.Navigate(typeof(PlayerPage), new PlayerArguments { StreamUrl = "http://bitdash-a.akamaihd.net/content/sintel-pr-wv/sintel.mpd" });
        }
    }
}
