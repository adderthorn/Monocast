using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ComponentModel;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Automation;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Xaml.Input;
using Windows.UI.Core;
using Windows.ApplicationModel.Core;
using Monocast.Controls;
using Monocast.Views;
using Monosoftware.Podcast;
using System.IO;
using System.Threading.Tasks;
using System;
using System.Diagnostics;
using Monocast.Services;
using Windows.System;
using System.Net.NetworkInformation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace Monocast
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page, INotifyPropertyChanged
    {
        #region Private Variables
        private string _CurrentHeader;
        private Symbol _PlayPauseSymbol = Symbol.Play;
        private string _SyncText = "Sync Subscriptions";
        private bool _IsCurrentEpisodeAvailable;
        private bool _IsCurrentSelected;
        #endregion

        public static MainPage Current { get; private set; }
        public Settings Settings => App.Settings;
        public Frame AppFrame => this.frame;
        public Subscriptions Subscriptions => App.Subscriptions;
        public string CurrentHeader
        {
            get => _CurrentHeader;
            set
            {
                if (value != _CurrentHeader)
                {
                    _CurrentHeader = value;
                    RaisePropertyChanged(nameof(CurrentHeader));
                }
            }
        }
        public Symbol PlayPauseSymbol
        {
            get => _PlayPauseSymbol;
            set
            {
                if (value != _PlayPauseSymbol)
                {
                    _PlayPauseSymbol = value;
                    RaisePropertyChanged(nameof(PlayPauseSymbol), nameof(PlayPauseString));
                }
            }
        }
        public bool IsPlaybackAllowed => PlaybackService.Instance.MediaPlayer.Source != null;
        public string PlayPauseString => _PlayPauseSymbol == Symbol.Play ? "Play" : "Pause";

        public string SyncText
        {
            get => _SyncText;
            set
            {
                if (value != _SyncText)
                {
                    _SyncText = value;
                    RaisePropertyChanged(nameof(SyncButton));
                }
            }
        }

        public bool IsCurrentEpisodeAvailable
        {
            get => _IsCurrentEpisodeAvailable;
            set
            {
                if (value != _IsCurrentEpisodeAvailable)
                {
                    _IsCurrentEpisodeAvailable = value;
                    RaisePropertyChanged(nameof(IsCurrentEpisodeAvailable));
                }
            }
        }

        public bool IsCurrentSelected
        {
            get => _IsCurrentSelected;
            private set
            {
                if (value != _IsCurrentSelected)
                {
                    _IsCurrentSelected = value;
                    RaisePropertyChanged(nameof(IsCurrentSelected));
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public MainPage()
        {
            this.InitializeComponent();
            this.Loaded += (sender, args) =>
            {
                Current = this;
                FinishLoading();
            };
            App.CurrentDownloads = new List<DownloadControl>();

            PlaybackService.Instance.MediaPlayer.PlaybackSession.PlaybackStateChanged += async (sender, e) =>
            {
                /* TODO: Add enablement here. */
                await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    switch (sender.PlaybackState)
                    {
                        case Windows.Media.Playback.MediaPlaybackState.Playing:
                            PlayPauseSymbol = Symbol.Pause;
                            break;
                        case Windows.Media.Playback.MediaPlaybackState.None:
                        case Windows.Media.Playback.MediaPlaybackState.Paused:
                            PlayPauseSymbol = Symbol.Play;
                            break;
                    }
                });
            };
            PlaybackService.Instance.MediaPlayer.MediaOpened += async (sender, e) =>
            {
                await CoreApplication.MainView.CoreWindow.Dispatcher
                    .RunAsync(CoreDispatcherPriority.Normal, () => RaisePropertyChanged(nameof(IsPlaybackAllowed)));
            };
            PlaybackService.Instance.MediaPlayer.MediaEnded += async (sender, e) =>
            {
                //sender.Source = null;
                await CoreApplication.MainView.CoreWindow.Dispatcher
                    .RunAsync(CoreDispatcherPriority.Normal, () => RaisePropertyChanged(nameof(IsPlaybackAllowed)));
            };
            // TODO: Fix go back
            //KeyboardAccelerator GoBack = new KeyboardAccelerator();
            //GoBack.Key = VirtualKey.Back;
            //GoBack.Invoked += OnBack;
            //KeyboardAccelerator AltLeft = new KeyboardAccelerator();
            //AltLeft.Key = VirtualKey.Left;
            //AltLeft.Modifiers = VirtualKeyModifiers.Menu;
            //AltLeft.Invoked += OnBack;
            //this.KeyboardAccelerators.Add(GoBack);
            //this.KeyboardAccelerators.Add(AltLeft);
        }

        private void RaisePropertyChanged(string PropertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(PropertyName));
        private void RaisePropertyChanged(params string[] PropertyNames)
        {
            for (int i = 0; i < PropertyNames.Length; i++)
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(PropertyNames[i]));
        }
        private async void FinishLoading()
        {
            NavigationViewItem itemToNavigateTo = AddFeedItem;
            if (Subscriptions == null)
            {
                AppData appData = new AppData(Utilities.SUBSCRIPTION_FILE, FolderLocation.Roaming);
                if (appData.CheckFileExists())
                {
                    try
                    {
                        App.Subscriptions = await Utilities.LoadSubscriptions();
                        if (App.Subscriptions.Podcasts.Count > 0)
                            itemToNavigateTo = LibraryItem;
                    }
                    catch (SerializationException ex)
                    {
                        MemoryStream stream = await appData.LoadFromFileAsync();
                        if (ex.Message.Contains(VersionConverter.OLD_NAMESPACE) && stream != Stream.Null)
                        {
                            stream.Seek(0, SeekOrigin.Begin);
                            VersionConverter versionConverter = new VersionConverter();
                            await versionConverter.LoadAsync(stream);
                            App.Subscriptions = versionConverter.Subscriptions;
                            if (App.Subscriptions.Podcasts.Count > 0)
                                itemToNavigateTo = LibraryItem;
                        }
                        else
                        {
                            App.Subscriptions = new Subscriptions();
                            Debug.Write(ex.Message);
                        }
                    }
                    catch (Exception ex)
                    {
                        App.Subscriptions = new Subscriptions();
                        Debug.Write(ex.Message);
                    }
                }
                else
                {
                    App.Subscriptions = new Subscriptions();
                }
            }
            itemToNavigateTo.IsSelected = true;
            if (Settings.SyncOnLaunch) SyncButton_Click(new object(), new TappedRoutedEventArgs());
            if (Subscriptions.Podcasts.Count > 0 && Settings.CachePodcastArtwork) await Subscriptions.RefreshPodcastArtworkAsync();
            await Subscriptions.GetLocalImagesAsync();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            if (e.Parameter is Uri)
            {
                AppFrame.Navigate(typeof(AddFeedView), e.Parameter);
            }
            base.OnNavigatedTo(e);
        }

        #region Back Requested Handlers
        private void NavView_BackRequested(NavigationView sender, NavigationViewBackRequestedEventArgs args)
        {
            bool empty = false;
            BackRequested(ref empty);
        }

        private void BackRequested(ref bool handled)
        {
            if (AppFrame == null) return;
            if (AppFrame.CanGoBack && !handled)
            {
                handled = true;
                AppFrame.GoBack();
            }
        }
        #endregion

        private void NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs e)
        {
            if (e.IsSettingsSelected)
            {
                sender.Header = "Settings";
                AppFrame.Navigate(typeof(SettingsView));
            }
            else
            {
                var selected = (NavigationViewItem)e.SelectedItem;
                if (selected == null) return;
                sender.Header = selected.Content.ToString();
                object arg = null;
                if (selected == CurrentItem && MainPage.Current.IsCurrentEpisodeAvailable)
                    arg = Subscriptions.ActiveEpisode;
                AppFrame.Navigate(Type.GetType(selected.Tag.ToString()), arg);
            }
        }

        private async void SyncButton_Click(object sender, TappedRoutedEventArgs e)
        {
            try
            {
                SyncRotateIcon.Begin();
                SyncText = "Syncing...";
                await Subscriptions.RefreshFeedsAsync(Settings.UseEpisodeArtwork, Settings.EpisodesToKeep, AppendToEnd:false);
                SyncText = "Saving...";
                await Utilities.SaveSubscriptionsAsync(Subscriptions);
                SyncText = "Done!";
                SyncRotateIcon.Stop();
                RaisePropertyChanged(nameof(Subscriptions));
                await Task.Delay(5000);
                SyncText = "Sync Podcasts";
            }
            catch (Exception ex)
            {
                SyncText = ex.Message;
#if DEBUG
                throw;
#endif
            }
            finally
            {
                SyncRotateIcon.Stop();
            }
        }

        private void PlayPauseButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            switch (PlaybackService.Instance.MediaPlayer.PlaybackSession.PlaybackState)
            {
                case Windows.Media.Playback.MediaPlaybackState.Playing:
                    PlaybackService.Instance.MediaPlayer.Pause();
                    break;
                case Windows.Media.Playback.MediaPlaybackState.None:
                case Windows.Media.Playback.MediaPlaybackState.Paused:
                    PlaybackService.Instance.MediaPlayer.Play();
                    break;
            }
        }

        private void Frame_Navigated(object sender, NavigationEventArgs e)
        {
            var t = e.SourcePageType;
            if (t == typeof(SettingsView))
            {
                return;
            }
            foreach (NavigationViewItem item in NavView.MenuItems)
            {
                if (item.Tag == null) continue;
                if (t == Type.GetType(item.Tag.ToString()))
                {
                    item.IsSelected = true;
                    return;
                }
            }
            LibraryItem.IsSelected = true;
        }
    }
}
