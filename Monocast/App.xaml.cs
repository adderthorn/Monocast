using System;
using System.Collections.Generic;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using System.ComponentModel;
#if DEBUG
//using Microsoft.AppCenter;
//using Microsoft.AppCenter.Analytics;
#endif
using Monosoftware.Podcast;
using Monocast.Controls;
using System.Diagnostics;

namespace Monocast
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    sealed partial class App : Application
    {
        private static Subscriptions _Subscriptions;
        private const float SAVE_TIMEOUT = 3f;
        private static DispatcherTimer SaveSubscriptionTimer;
        private static bool isSaving = false;

        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            this.InitializeComponent();
            //this.Suspending += OnSuspending;
            Settings = new Settings();
#if DEBUG
            //AppCenter.Start("29dd4058-5e1b-4688-a8ee-b35a247437c4", typeof(Analytics));
#endif
            SaveSubscriptionTimer = new DispatcherTimer()
            {
                Interval = TimeSpan.FromSeconds(SAVE_TIMEOUT)
            };
            SaveSubscriptionTimer.Tick += async (s, e) =>
            {
                DispatcherTimer timer = s as DispatcherTimer;
                timer.Stop();
                if (!isSaving)
                {
                    isSaving = true;
                    await Utilities.SaveSubscriptionsAsync(Subscriptions);
                    isSaving = false;
                }
            };
        }

        /// <summary>
        /// The Subscriptions that will be used to share data between
        /// the various pages in the applicaion.
        /// </summary>
        public static Subscriptions Subscriptions
        {
            get => _Subscriptions;
            set
            {
                _Subscriptions = value;
                _Subscriptions.PropertyChanged += Subscriptions_PropertyChanged;
            }
        }

        private static void Subscriptions_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            SaveSubscriptionTimer.Stop();
            SaveSubscriptionTimer.Start();
        }

        /// <summary>
        /// Application settings to be used within the application
        /// </summary>
        public static Settings Settings { get; private set; }

        /// <summary>
        /// Current list of active downloads
        /// </summary>
        public static List<DownloadControl> CurrentDownloads { get; set; }

        /// <summary>
        /// The page the application was on before a sync or download event so we can
        /// navigate back to it when we are finished.
        /// </summary>
        //  public static Page OnSyncResumePage { get; set; }

        /// <summary>
        /// Invoked when the application is activated by the system rather than by the end user.
        /// </summary>
        /// <param name="args">Detailed about the specific activation.</param>
        protected override void OnActivated(IActivatedEventArgs args)
        {
            if (args.Kind == ActivationKind.Protocol)
            {
                Frame rootFrame = Window.Current.Content as Frame;

                if (rootFrame == null)
                {
                    rootFrame = getRootFrame(args);
                }

                ProtocolActivatedEventArgs eventArgs = args as ProtocolActivatedEventArgs;
                // TODO: Handle URI activation
                // The received URI is eventArgs.Uri.AbsoluteUri
                
                string newFeed = eventArgs.Uri.AbsoluteUri.ToString();
                if (newFeed.StartsWith("pcast", StringComparison.CurrentCultureIgnoreCase))
                {
                    newFeed = "http" + newFeed.Remove(0, "pcast".Length);
                }
                rootFrame.Navigate(typeof(MainPage), new Uri(newFeed));
                Window.Current.Activate();
            }
            base.OnActivated(args);
        }

        /// <summary>
        /// Invoked when the application is launched normally by the end user.  Other entry points
        /// will be used such as when the application is launched to open a specific file.
        /// </summary>
        /// <param name="e">Details about the launch request and process.</param>
        protected override void OnLaunched(LaunchActivatedEventArgs e)
        {
            Frame rootFrame = Window.Current.Content as Frame;

            // Do not repeat app initialization when the Window already has content,
            // just ensure that the window is active
            if (rootFrame == null)
            {
                rootFrame = getRootFrame(e);
            }

            if (e.PrelaunchActivated == false)
            {
                if (rootFrame.Content == null)
                {
                    // When the navigation stack isn't restored navigate to the first page,
                    // configuring the new page by passing required information as a navigation
                    // parameter
                    rootFrame.Navigate(typeof(MainPage), e.Arguments);
                }
                // Ensure the current window is active
                Window.Current.Activate();
            }

            // Manage the subscriptions

        }

        private Frame getRootFrame(IActivatedEventArgs e)
        {
            // Create a Frame to act as the navigation context and navigate to the first page
            Frame rootFrame = new Frame();
            rootFrame.NavigationFailed += OnNavigationFailed;

            if (e.PreviousExecutionState == ApplicationExecutionState.Terminated)
            {
                //TODO: Load state from previously suspended application
            }

            // Place the frame in the current Window
            Window.Current.Content = rootFrame;
            return rootFrame;
        }

        /// <summary>
        /// Invoked when Navigation to a certain page fails
        /// </summary>
        /// <param name="sender">The Frame which failed navigation</param>
        /// <param name="e">Details about the navigation failure</param>
        void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            throw new Exception("Failed to load Page " + e.SourcePageType.FullName);
        }

        /// <summary>
        /// Invoked when application execution is being suspended.  Application state is saved
        /// without knowing whether the application will be terminated or resumed with the contents
        /// of memory still intact.
        /// </summary>
        /// <param name="sender">The source of the suspend request.</param>
        /// <param name="e">Details about the suspend request.</param>
        private async void OnSuspending(object sender, SuspendingEventArgs e)
        {
            var deferral = e.SuspendingOperation.GetDeferral();
            //TODO: Save application state and stop any background activity
            if (!isSaving) await Utilities.SaveSubscriptionsAsync(Subscriptions);
            deferral.Complete();
        }
    }
}
