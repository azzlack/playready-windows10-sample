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

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace PlayReadySample
{
    using System.Diagnostics;

    using Windows.Media;
    using Windows.Media.Protection;
    using Windows.Media.Protection.PlayReady;

    using Microsoft.PlayerFramework.Adaptive;

    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class PlayerPage : Page
    {
        private PlayerArguments arguments;

        public PlayerPage()
        {
            this.InitializeComponent();

            Player.MediaOpened += OnMediaOpened;
            Player.MediaFailed += OnMediaFailed;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            arguments = e.Parameter as PlayerArguments;

            if (arguments != null)
            {
                InitializePlugins();
                InitializeMediaExtensionManager();
                InitializeMediaProtectionManager();

                Player.Source = new Uri(arguments.StreamUrl);
            }
        }

        /// <summary>Initializes the Smooth Streaming plugin.</summary>
        private void InitializePlugins()
        {
            var adaptivePlugin = new AdaptivePlugin();

            Player.Plugins.Add(adaptivePlugin);
        }

        /// <summary>Initializes the media extension manager so we can handle PlayReady protected content.</summary>
        private void InitializeMediaExtensionManager()
        {
            var plugins = new MediaExtensionManager();

            // Add support for IIS Smooth Streaming Manifests
            plugins.RegisterByteStreamHandler("Microsoft.Media.AdaptiveStreaming.SmoothByteStreamHandler", ".ism", "text/xml");
            plugins.RegisterByteStreamHandler("Microsoft.Media.AdaptiveStreaming.SmoothByteStreamHandler", ".ism", "application/vnd.ms-sstr+xml");

            // Add support for PlayReady video and audio files
            plugins.RegisterByteStreamHandler("Microsoft.Media.Protection.PlayReady.PlayReadyByteStreamHandler", ".pyv", "");
            plugins.RegisterByteStreamHandler("Microsoft.Media.Protection.PlayReady.PlayReadyByteStreamHandler", ".pya", "");
        }

        /// <summary>Initializes the PlayReady protection manager.</summary>
        private void InitializeMediaProtectionManager()
        {
            var mediaProtectionManager = new MediaProtectionManager();
            mediaProtectionManager.ComponentLoadFailed += OnMediaProtectionManagerComponentLoadFailed;
            mediaProtectionManager.ServiceRequested += OnMediaProtectionManagerServiceRequested;

            // Set up the container GUID for the CFF format (used with DASH streams), see http://uvdemystified.com/uvfaq.html#3.2
            // The GUID represents MPEG DASH Content Protection using Microsoft PlayReady, see http://dashif.org/identifiers/protection/
            mediaProtectionManager.Properties["Windows.Media.Protection.MediaProtectionContainerGuid"] = "{9A04F079-9840-4286-AB92-E65BE0885F95}";

            // Set up the drm layer to use. Hardware DRM is the default, but not all older hardware supports this
            var supportsHardwareDrm = PlayReadyStatics.CheckSupportedHardware(PlayReadyHardwareDRMFeatures.HardwareDRM);
            if (!supportsHardwareDrm)
            {
                mediaProtectionManager.Properties["Windows.Media.Protection.UseSoftwareProtectionLayer"] = true;
            }

            // Set up the content protection manager so it uses the PlayReady Input Trust Authority (ITA) for the relevant media sources
            // The MediaProtectionSystemId GUID is format and case sensitive, see https://msdn.microsoft.com/en-us/library/windows.media.protection.mediaprotectionmanager.properties.aspx
            var cpsystems = new PropertySet();
            cpsystems[PlayReadyStatics.MediaProtectionSystemId.ToString("B").ToUpper()] = "Windows.Media.Protection.PlayReady.PlayReadyWinRTTrustedInput";
            mediaProtectionManager.Properties["Windows.Media.Protection.MediaProtectionSystemIdMapping"] = cpsystems;
            mediaProtectionManager.Properties["Windows.Media.Protection.MediaProtectionSystemId"] = PlayReadyStatics.MediaProtectionSystemId.ToString("B").ToUpper();

            Player.ProtectionManager = mediaProtectionManager;
        }

        private void OnMediaOpened(object sender, RoutedEventArgs e)
        {
            // Start playing the file when ready
            Player.Play();
        }

        private void OnMediaFailed(object sender, ExceptionRoutedEventArgs e)
        {
            Debug.WriteLine("Media Failed: " + e.ErrorMessage);
        }

        private void OnMediaProtectionManagerComponentLoadFailed(MediaProtectionManager sender, ComponentLoadFailedEventArgs e)
        {
            Debug.WriteLine("ProtectionManager ComponentLoadFailed");
            e.Completion.Complete(false);
        }

        private async void OnMediaProtectionManagerServiceRequested(MediaProtectionManager sender, ServiceRequestedEventArgs e)
        {
            Debug.WriteLine("ProtectionManager ServiceRequested");

            var completionNotifier = e.Completion;
            var request = (IPlayReadyServiceRequest)e.Request;

            var result = false;

            if (request.Type == PlayReadyStatics.IndividualizationServiceRequestType)
            {
                result = await PlayReadyLicenseHandler.RequestIndividualizationToken(request as PlayReadyIndividualizationServiceRequest);
            }
            else if (request.Type == PlayReadyStatics.LicenseAcquirerServiceRequestType)
            {
                // NOTE: You might need to set the request.ChallengeCustomData, depending on your Rights Manager.
                if (!string.IsNullOrEmpty(arguments.RightsManagerUrl))
                {
                    request.Uri = new Uri(arguments.RightsManagerUrl);
                }

                result = await PlayReadyLicenseHandler.RequestLicense(request as PlayReadyLicenseAcquisitionServiceRequest);
            }

            completionNotifier.Complete(result);
        }
    }
}
