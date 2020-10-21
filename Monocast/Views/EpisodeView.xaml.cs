using System;
using System.IO;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Collections.Generic;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Monosoftware.Podcast;
using Windows.UI.Xaml.Navigation;
using System.Linq;
using Windows.Storage.Pickers;
using Monocast.Controls;
using Windows.Storage;
using System.Net.NetworkInformation;
using Windows.Storage.AccessCache;
using Windows.System;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Monocast.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class EpisodeView : Page, INotifyPropertyChanged
    {
        private Episode _Episode;

        public Episode Episode
        {
            get => _Episode;
            set
            {
                if (_Episode != value)
                {
                    _Episode = value;
                    NotifyPropertyChanged(nameof(Episode));
                    NotifyPropertyChanged(nameof(PublishedDateString));
                    NotifyPropertyChanged(nameof(DurationString));
                }
            }
        }

        public string PublishedDateString
        {
            get
            {
                if (_Episode?.PublishDate != null && _Episode.PublishDate != DateTime.MinValue)
                {
                    return _Episode.PublishDate.ToString("D");
                }
                return Utilities.UNKNOWN;
            }
        }
        public string DurationString
        {
            get
            {
                if (Episode?.Duration != null
                    && _Episode?.Duration != TimeSpan.MinValue
                    && _Episode?.Duration != TimeSpan.MaxValue)
                {
                    return _Episode.Duration.ToString(Utilities.DURATION_FORMAT);
                }
                return Utilities.UNKNOWN;
            }
        }
        public Uri Artwork
        {
            get
            {
                if (App.Settings.UseEpisodeArtwork &&
                    NetworkInterface.GetIsNetworkAvailable() &&
                    Episode.HasUniqueArtwork)
                {
                    return Episode.Artwork.MediaSource;
                }
                return Episode.Podcast.Artwork.GetBestArtworkSoruce(); 
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public EpisodeView()
        {
            this.InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            if (e.Parameter is Episode)
            {
                Episode = e.Parameter as Episode;
                DescriptionWebView.NavigateToThemedString(Episode.Description);
            }
            base.OnNavigatedTo(e);
        }

        private void NotifyPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        private async void DownloadButton_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            StorageFile file = null;
            FileSavePicker picker = new FileSavePicker()
            {
                SuggestedStartLocation = PickerLocationId.Desktop,
                SuggestedFileName = Episode.Title,
            };
            picker.FileTypeChoices.Add("Audio Files", new List<string>() { ".mp3" });
            file = await picker.PickSaveFileAsync();
            if (file == null) return;
            var control = new DownloadControl(App.CurrentDownloads, Episode, file,
                Utilities.GetBestArtworkUriForEpisode(Episode));
            var controlList = new List<DownloadControl>() { control };
            this.Frame.Navigate(typeof(DownloadView), controlList,
                new Windows.UI.Xaml.Media.Animation.DrillInNavigationTransitionInfo());
        }

        private void PlayButton_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            if (Episode == null) return;
            this.Frame.Navigate(typeof(PlayerView), Episode,
                new Windows.UI.Xaml.Media.Animation.DrillInNavigationTransitionInfo());
        }

        private async void DescriptionWebView_NavigationStarting(WebView sender, WebViewNavigationStartingEventArgs args)
        {
            if (args.Uri != null)
            {
                args.Cancel = true;
                await Launcher.LaunchUriAsync(args.Uri);
            }
        }
    }
}
