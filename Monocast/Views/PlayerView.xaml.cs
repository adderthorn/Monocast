using System;
using System.ComponentModel;
using System.IO;
using System.Reflection;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Controls;
using Windows.Media.Playback;
using Windows.UI.Xaml.Navigation;
using Monosoftware.Podcast;
using Monocast.Services;
using Monocast.ViewModels;
using System.Threading.Tasks;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Input;
using System.Net.NetworkInformation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Monocast.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class PlayerView : Page, INotifyPropertyChanged
    {
        private ImageSource _PosterSource;
        private Symbol _PlayPauseSymbol;
        private Visibility _SymbolVisibility;

        public Subscriptions Subscriptions => App.Subscriptions;
        public Episode ActiveEpisode { get; set; }
        public Symbol PlayPauseSymbol
        {
            get => _PlayPauseSymbol;
            set
            {
                if (value != _PlayPauseSymbol)
                {
                    _PlayPauseSymbol = value;
                    RaisePropertyChanged(nameof(PlayPauseSymbol));
                }
            }
        }
        public Visibility SymbolVisibility
        {
            get => _SymbolVisibility;
            set
            {
                if (value != _SymbolVisibility)
                {
                    _SymbolVisibility = value;
                    RaisePropertyChanged(nameof(SymbolVisibility), nameof(BufferRingVisibility));
                }
            }
        }
        public Visibility BufferRingVisibility =>
            _SymbolVisibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;

        public MediaPlayer MediaPlayer => PlaybackService.Instance.MediaPlayer;
        public ImageSource PosterSource
        {
            get => _PosterSource;
            set
            {
                if (_PosterSource != value)
                {
                    _PosterSource = value;
                    RaisePropertyChanged(nameof(PosterSource));
                }
            }
        }

        public PlayerViewModel PlayerViewModel { get; set; }

        public PlayerView()
        {
            this.InitializeComponent();

            this.NavigationCacheMode = NavigationCacheMode.Disabled;

            PlayerViewModel = new PlayerViewModel(MediaPlayer, Dispatcher);

            Loaded += PlayerView_Loaded;
            Unloaded += PlayerView_Unloaded;
            PlayerViewModel.PropertyChanged += PlayerViewModel_PropertyChangedAsync;
            PlayerViewModel.PlaybackSession.PropertyChanged += PlayerViewModel_PropertyChangedAsync;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private async void PlayerViewModel_PropertyChangedAsync(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "EpisodePlayerInfo")
            {
                MainMPE.PosterSource = PlayerViewModel.EpisodePlayerInfo.ArtworkPoster;
            }
            else if (e.PropertyName == "PlaybackState" && sender is PlaybackSessionViewModel)
            {
                var sessionViewModel = (PlaybackSessionViewModel)sender;
                if (Math.Abs((sessionViewModel.Position.Seconds
                - ActiveEpisode.PlaybackPosition.Seconds)) >= Subscriptions.MajorTimespanChange.Seconds
                && Subscriptions.LastModifiedDate.AddMinutes(1) < DateTime.Now)
                {
                    ActiveEpisode.PlaybackPosition = sessionViewModel.Position;
                    if (Subscriptions != null) await Utilities.SaveSubscriptionsAsync(Subscriptions);
                }
                ShowPlaybackNotification();
            }
            else if (e.PropertyName == "MediaEnded")
            {
                ActiveEpisode.PlaybackPosition = ActiveEpisode.Duration;
                await Utilities.SaveSubscriptionsAsync(Subscriptions);
                Frame.Navigate(typeof(PodcastView), ActiveEpisode.Podcast);
            }
        }

        private void PlayerView_Unloaded(object sender, RoutedEventArgs e)
        {
            PlayerViewModel.Dispose();
            PlayerViewModel = null;
            GC.Collect();
        }

        private async void PlayerView_Loaded(object sender, RoutedEventArgs e)
        {
            if (ActiveEpisode == null) return;
            if (PlayerViewModel.PlaybackSession.PlaybackState == MediaPlaybackState.None
                || PlaybackService.Instance.NowPlayingEpisode != ActiveEpisode)
            {
                if (ActiveEpisode.IsPlayed)
                    ActiveEpisode.IsPlayed = false;
                PlayerViewModel.SetNewEpisodePlayerInfo(await EpisodePlayerInfo.CreateFromEpisodeAsync(ActiveEpisode));
                PlaybackService.Instance.NowPlayingEpisode = ActiveEpisode;
            }
            else
            {
                PlayerViewModel.EpisodePlayerInfo = new EpisodePlayerInfo(ActiveEpisode);
                ShowPlaybackNotification();
            }
            await SetPoster();
            MainMPE.SetMediaPlayer(MediaPlayer);
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            if (e.Parameter is Episode)
            {
                ActiveEpisode = (Episode)e.Parameter;
            }
            else
            {
                ActiveEpisode = PlaybackService.Instance.NowPlayingEpisode;
            }
            base.OnNavigatedTo(e);
        }

        private async Task SetPoster()
        {
            if (ActiveEpisode == null) return;
            var tempBitamp = new BitmapImage();
            Podcast pcast = ActiveEpisode.Podcast;
            if (ActiveEpisode.Artwork?.IsDownloaded == true)
            {
                await tempBitamp.SetSourceAsync(ActiveEpisode.Artwork.GetStream().AsRandomAccessStream());
                PosterSource = tempBitamp;
                return;
            }
            else if (pcast.Artwork?.IsDownloaded == true)
            {
                await tempBitamp.SetSourceAsync(pcast.Artwork.GetStream().AsRandomAccessStream());
                PosterSource = tempBitamp;
                return;
            }
            try
            {
                // Don't bother trying if network is down
                if (!NetworkInterface.GetIsNetworkAvailable()) throw new Exception();
                await pcast.Artwork.DownloadFileAsync();
                var s = pcast.Artwork.GetStream();
                await tempBitamp.SetSourceAsync(s.AsRandomAccessStream());
            }
            catch
            {
                const string fileName = "Monocast.Resources.placeholder_image.bmp";
                var assembly = typeof(Monocast.Program).GetTypeInfo().Assembly;
                using (MemoryStream memoryStream = new MemoryStream())
                {
                    await assembly.GetManifestResourceStream(fileName).CopyToAsync(memoryStream);
                    memoryStream.Seek(0, SeekOrigin.Begin);
                    await tempBitamp.SetSourceAsync(memoryStream.AsRandomAccessStream());
                }
            }
            PosterSource = tempBitamp;
        }

        private void RaisePropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        private void RaisePropertyChanged(params string[] propertyNames)
        {
            for (int i = 0; i < propertyNames.Length; i++)
            {
                RaisePropertyChanged(propertyNames[i]);
            }
        }

        private void OverallArea_Tapped(object sender, TappedRoutedEventArgs e)
        {
            var point = e.GetPosition(MainMPE);
            // 48 = CommandBar size + 60 for extra padding
            double maxY = Window.Current.Bounds.Height - 48 - 60;
            if (point.Y < maxY)
            {
                PlayerViewModel.TogglePlayPause();
            }
        }

        private async void ShowPlaybackNotification()
        {
            SymbolVisibility = Visibility.Visible;
            switch (PlayerViewModel.PlaybackSession.PlaybackState)
            {
                case MediaPlaybackState.Playing:
                    PlayPauseSymbol = Symbol.Play;
                    if (ActiveEpisode.Duration != PlayerViewModel.TotalDuration
                        && PlayerViewModel.TotalDuration > TimeSpan.Zero)
                    {
                        ActiveEpisode.Duration = PlayerViewModel.TotalDuration;
                    }

                    break;
                case MediaPlaybackState.Paused:
                    PlayPauseSymbol = Symbol.Pause;
                    break;
                default:
                    SymbolVisibility = Visibility.Collapsed;
                    RaiseNotification.Begin();
                    return;
            }
            RaiseNotification.Begin();
            await Task.Delay(4000);
            ExitNotification.Begin();
        }
    }
}
