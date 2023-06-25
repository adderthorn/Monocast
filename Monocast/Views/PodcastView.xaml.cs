using System;
using System.ComponentModel;
using Monosoftware.Podcast;
using Monocast.Controls;
using System.Collections.Generic;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;
using Windows.Web.Http;
using Windows.Storage.Pickers;
using Windows.Storage;
using Windows.System.Profile;
using System.IO;
using Windows.Storage.Streams;
using System.Threading.Tasks;
using System.Net.NetworkInformation;
using System.Linq;
using Windows.UI.Xaml.Controls.Primitives;
using System.Collections.ObjectModel;
using Windows.UI.Xaml.Media.Animation;
using Windows.System;
using Monocast.Services;

namespace Monocast.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class PodcastView : Page, INotifyPropertyChanged
    {
        private const double MAX_WIDTH = 100;
        private const int WAIT_TIME = 3000;
        private const int MAX_TITLE_WIDTH = 30;
        private const string MOBILE_DEVICE_FAMILY = "Windows.Mobile";
        private const string PUBLISHED_STRING = "Published: ";

        private bool selectionToggleChecked = false;
        private Podcast _Podcast;
        private Uri _Artwork;
        private string _Title;
        private string _EpisodeTitle;
        private Visibility _PublishedVisibility = Visibility.Collapsed;
        private List<EpisodeListItem> allEpisodeItems;
        private Visibility _EpisodesCheckBoxVisibility = Visibility.Collapsed;

        public event PropertyChangedEventHandler PropertyChanged;

        public string Message { get; set; }
        private Episode SelectedEpisode { get; set; }
        public EpisodeListItem SelectedEpisodeListItem => EpisodeListView.SelectedItem as EpisodeListItem;
        public Visibility PublishedVisibility { get => _PublishedVisibility; }
        public Podcast Podcast
        {
            get => _Podcast;
            set
            {
                _Podcast = value;
                _Artwork = Podcast.Artwork.GetBestArtworkSoruce();
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                RefreshPodcast();
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                RaisePropertyChanged(nameof(Podcast), nameof(Artwork));
            }
        }
        public Uri Artwork
        {
            get => _Artwork;
            private set
            {
                if (_Artwork != value)
                {
                    _Artwork = value;
                    RaisePropertyChanged(nameof(Artwork));
                }
            }
        }
        public string Title
        {
            get => _Title;
            set
            {
                if (_Title != value)
                {
                    _Title = value;
                    RaisePropertyChanged(nameof(Title));
                }
            }
        }
        public string EpisodeTitle
        {
            get => _EpisodeTitle;
            set
            {
                if (!string.IsNullOrEmpty(value) && _EpisodeTitle != value)
                {
                    _EpisodeTitle = value;
                    RaisePropertyChanged(nameof(EpisodeTitle));
                }
            }
        }

        public bool IsEpisodeSelected => SelectedEpisode != null || selectionToggleChecked;

        public string PublishedDateString
        {
            get
            {
                if (SelectedEpisode?.PublishDate == null) return PUBLISHED_STRING + Utilities.UNKNOWN;
                if (SelectedEpisode?.PublishDate == DateTime.MinValue) return PublishedDateString + Utilities.UNKNOWN;
                return PUBLISHED_STRING + SelectedEpisode?.PublishDate.ToString("D");
            }
        }

        public string DurationString
        {
            get
            {
                if (SelectedEpisode?.Duration != null
                    && SelectedEpisode?.Duration != TimeSpan.MinValue
                    && SelectedEpisode?.Duration != TimeSpan.MaxValue)
                {
                    return SelectedEpisode.Duration.ToString(Utilities.DURATION_FORMAT);
                }
                return Utilities.UNKNOWN;
            }
        }

        public bool ShowArchived
        {
            get => App.Settings.ShowArchived;
            set
            {
                App.Settings.ShowArchived = value;
                toggleArchived();
            }
        }
        private Visibility EpisodesCheckBoxVisibility
        {
            get => _EpisodesCheckBoxVisibility;
            set
            {
                if (value != _EpisodesCheckBoxVisibility)
                {
                    _EpisodesCheckBoxVisibility = value;
                    RaisePropertyChanged(nameof(EpisodesCheckBoxVisibility));
                }
            }
        }

        public ObservableCollection<EpisodeListItem> EpisodeListItems { get; set; }
        public PodcastView()
        {
            EpisodeListItems = new ObservableCollection<EpisodeListItem>();
            allEpisodeItems = new List<EpisodeListItem>();
            this.InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            if (e.Parameter is Podcast)
            {
                Podcast = (Podcast)e.Parameter;
                if (!MainPage.Current.IsCurrentEpisodeAvailable || App.Subscriptions.ActiveEpisode.Podcast != Podcast)
                    setEpisode(Podcast.Episodes.FirstOrDefault(), forceSelect: true);
            }
            else if (e.Parameter is Episode)
            {
                Episode ep = (Episode)e.Parameter;
                Podcast = ep.Podcast;
                setEpisode(ep, true);
            }
            else if (MainPage.Current.IsCurrentEpisodeAvailable && MainPage.Current.IsCurrentSelected)
            {
                Podcast = App.Subscriptions.ActiveEpisode.Podcast;
                setEpisode(App.Subscriptions.ActiveEpisode, true);
            }
            base.OnNavigatedTo(e);
        }

        public async Task RefreshPodcast()
        {
            Title = _Podcast.Title;
            foreach (var episode in _Podcast.Episodes
                .OrderBy(e => !e.IsPinned)
                .ThenByDescending(e => e.PublishDate))
            {
                var episodeListItem = new EpisodeListItem(episode);
                episodeListItem.DownloadEpisode += DownloadButton_Click;
                episodeListItem.MarkEpisodeAsPlayed += MarkEpisodeAsPlayed;
                episodeListItem.ToggleMarkArchived += ToggleMarkEpisodeArchived;
                episodeListItem.PinEpisode += PinEpisode;
                episodeListItem.EpisodeChecked += (s, e) =>
                {
                    if (s == null) return;
                    SetCheckedState(s as EpisodeListItem);
                };
                episodeListItem.GoToEpisodeDetails += (s, e) =>
                {
                    if (s is EpisodeListItem)
                    {
                        var item = s as EpisodeListItem;
                        this.Frame.Navigate(typeof(EpisodeView), item.Episode,
                            new Windows.UI.Xaml.Media.Animation.DrillInNavigationTransitionInfo());
                    }
                };
                EpisodeListItems.Add(episodeListItem);
                allEpisodeItems.Add(episodeListItem);
            }
            // TODO: I need to make this compatible with toggling
            //if (!App.Settings.ShowArchived)
            //{
            //    episodeList = episodeList.Where(e => !e.Episode.IsArchived).ToList();
            //}
            //EpisodeListView.ItemsSource = episodeList;
            if (Podcast.Artwork?.IsDownloaded == true)
            {
                var artworkImage = new BitmapImage();
                Stream temp = Podcast.Artwork.GetStream();
                IRandomAccessStream randomAccessStream = temp.AsRandomAccessStream();
                await artworkImage.SetSourceAsync(randomAccessStream);
                //Artwork = artworkImage;
            }
            if (!App.Settings.ShowArchived)
            {
                toggleArchived();
            }
        }

        private void Bmp_ImageOpened(object sender, RoutedEventArgs e)
        {
            BitmapImage img = sender as BitmapImage;
            double aspectRatio = img.PixelWidth;
            if (aspectRatio == 0) aspectRatio = 1;
            aspectRatio = img.PixelHeight / aspectRatio;
        }

        private void ProgressCallback(HttpProgress progress)
        {
            if (progress.TotalBytesToReceive == null) return;
            decimal current = (decimal)progress.BytesReceived / 1000 / 1000;
            decimal total = (decimal)progress.TotalBytesToReceive / 1000 / 1000;
            StatusText.Text = string.Format("Downloading: {0:N2} MB of {1:N2} MB", current, total);
        }

        private async void DownloadButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is EpisodeListItem)
            {
                setEpisode(((EpisodeListItem)sender).Episode);
            }
            if (SelectedEpisode == null) return;
            StorageFile file = null;
            FileSavePicker picker = new FileSavePicker()
            {
                SuggestedStartLocation = PickerLocationId.Desktop,
                SuggestedFileName = SelectedEpisode.Title,
            };
            picker.FileTypeChoices.Add("Audio Files", new List<string>() { ".mp3" });
            file = await picker.PickSaveFileAsync();
            if (file == null) return;
            var control = new DownloadControl(App.CurrentDownloads, SelectedEpisode, file, Utilities.GetBestArtworkUriForEpisode(SelectedEpisode));
            var controlList = new List<DownloadControl>() { control };
            this.Frame.Navigate(typeof(DownloadView), controlList,
                new Windows.UI.Xaml.Media.Animation.DrillInNavigationTransitionInfo());
        }

        private void PlayButton_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedEpisode == null) return;
            this.Frame.Navigate(typeof(PlayerView), SelectedEpisode,
                new Windows.UI.Xaml.Media.Animation.DrillInNavigationTransitionInfo());
        }

        private void MarkEpisodeAsPlayed(object sender, RoutedEventArgs e)
        {
            if (sender is EpisodeListItem)
            {
                EpisodeListItem item = sender as EpisodeListItem;
                item.Episode.IsPlayed = true;
                item.EpisodeUpdated();
            }
        }

        private void ToggleMarkEpisodeArchived(object sender, RoutedEventArgs e)
        {
            if (sender is EpisodeListItem)
            {
                EpisodeListItem item = sender as EpisodeListItem;
                item.Episode.IsArchived = !item.Episode.IsArchived;
                item.EpisodeUpdated();
            }
        }

        private void PinEpisode(object sender, RoutedEventArgs e)
        {
            if (sender is EpisodeListItem)
            {
                EpisodeListItem item = sender as EpisodeListItem;
                item.Episode.IsPinned = !item.Episode.IsPinned;
                item.EpisodeUpdated();
            }
        }

        private void RaisePropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        private void RaisePropertyChanged(params string[] names)
        {
            for (int i = 0; i < names.Length; i++)
            {
                RaisePropertyChanged(names[i]);
            }
        }

        private void DetailsButton_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedEpisode == null) return;
            PlaybackService.Instance.NowPlayingEpisode = SelectedEpisode;
            Frame.Navigate(typeof(EpisodeView), SelectedEpisode,
                new Windows.UI.Xaml.Media.Animation.DrillInNavigationTransitionInfo());
        }

        private void setEpisode(Episode episode, bool forceSelect = false)
        {
            SelectedEpisode = episode;
            Visibility visibility = Visibility.Collapsed;
            if (episode != null) visibility = Visibility.Visible;
            _PublishedVisibility = visibility;
            RaisePropertyChanged(nameof(SelectedEpisode),
                nameof(PublishedVisibility),
                nameof(PublishedDateString),
                nameof(DurationString),
                nameof(IsEpisodeSelected),
                nameof(EpisodeTitle));
            if (forceSelect)
            {
                EpisodeListView.SelectedItem = EpisodeListItems.FirstOrDefault(ep => ep.Episode == episode);
            }
            App.Subscriptions.ActiveEpisode = episode;
            MainPage.Current.IsCurrentEpisodeAvailable = true;
        }

        private void SetCheckedState(EpisodeListItem toggledItem)
        {
            if (toggledItem.Selected && EpisodeListView.Items.All(i => (i as EpisodeListItem).Selected))
            {
                EpisodesAllCheckBox.IsChecked = true;
            }
            else if (!toggledItem.Selected && EpisodeListView.Items.All(i => !(i as EpisodeListItem).Selected))
            {
                EpisodesAllCheckBox.IsChecked = false;
            }
            else
            {
                EpisodesAllCheckBox.IsChecked = null;
            }
        }

        private void EpisodesAllCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            foreach (EpisodeListItem item in EpisodeListView.Items)
            {
                item.Selected = true;
            }
        }

        private void EpisodesAllCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            foreach (EpisodeListItem item in EpisodeListView.Items)
            {
                item.Selected = false;
            }
        }

        private void EpisodesAllCheckBox_Indeterminate(object sender, RoutedEventArgs e)
        {
            if (EpisodeListView.Items.All(i => (i as EpisodeListItem).Selected))
            {
                EpisodesAllCheckBox.IsChecked = false;
            }
        }

        private void SelectionToggleButton_Toggle(object sender, RoutedEventArgs e)
        {
            EpisodesCheckBoxVisibility = Visibility.Visible;
            bool isChecked = (sender as ToggleButton).IsChecked == true;
            selectionToggleChecked = isChecked;
            EpisodesAllCheckBox.IsChecked = false;
            foreach (EpisodeListItem item in EpisodeListView.Items)
                item.CanSelect = isChecked;
            RaisePropertyChanged(nameof(IsEpisodeSelected));
            int from = isChecked ? -20 : 0;
            int to = isChecked ? 0 : -20;
            SelectionEase.Children[0].SetValue(DoubleAnimation.FromProperty, from);
            SelectionEase.Children[0].SetValue(DoubleAnimation.ToProperty, to);
            SelectionEase.Begin();
        }

        private void MarkEpisodePlayed_Click(object sender, RoutedEventArgs e)
        {
            if (EpisodesAllCheckBox.IsChecked != false)
            {
                foreach (EpisodeListItem item in EpisodeListView.Items.Where(el => (el as EpisodeListItem).Selected))
                {
                    MarkEpisodeAsPlayed(item, e);
                }
                if (!App.Settings.KeepEpisodeSelectionAfterAction)
                {
                    EpisodesAllCheckBox.IsChecked = false;
                    SelectionToggleButton.IsChecked = false;
                }
            }
            else
            {
                MarkEpisodeAsPlayed(SelectedEpisodeListItem, e);
            }
        }

        private void MarkEpisodeArchived_Click(object sender, RoutedEventArgs e)
        {
            if (EpisodesAllCheckBox.IsChecked != false)
            {
                foreach(EpisodeListItem item in EpisodeListView.Items.Where(el => (el as EpisodeListItem).Selected))
                {
                    ToggleMarkEpisodeArchived(item, e);
                }
                if (!App.Settings.KeepEpisodeSelectionAfterAction)
                {
                    EpisodesAllCheckBox.IsChecked = false;
                    SelectionToggleButton.IsChecked = false;
                }
            }
            else
            {
                ToggleMarkEpisodeArchived(SelectedEpisodeListItem, e);
            }
        }

        private void PinEpisode_Click(object sender, RoutedEventArgs e)
        {
            if (EpisodesAllCheckBox.IsChecked != false)
            {
                foreach (EpisodeListItem item in EpisodeListView.Items.Where(el => (el as EpisodeListItem).Selected))
                {
                    PinEpisode(item, e);
                }
                if (!App.Settings.KeepEpisodeSelectionAfterAction)
                {
                    EpisodesAllCheckBox.IsChecked = false;
                    SelectionToggleButton.IsChecked = false;
                }
            }
            else
            {
                PinEpisode(SelectedEpisodeListItem, e);
            }
        }

        private void toggleArchived()
        {
            foreach (var item in allEpisodeItems.Where(e => e.Episode.IsArchived))
            {
                if (!ShowArchived)
                {
                    if (SelectedEpisodeListItem == item) setEpisode(null);
                    EpisodeListItems.Remove(item);
                }
                else
                {
                    EpisodeListItems.Insert(allEpisodeItems.IndexOf(item), item);
                }
            }
        }

        private void SelectionEase_Completed(object sender, object e) =>
            EpisodesCheckBoxVisibility = selectionToggleChecked ? Visibility.Visible : Visibility.Collapsed;

        private async void DescWebView_NavigationStarting(WebView sender, WebViewNavigationStartingEventArgs args)
        {
            if (args.Uri != null)
            {
                args.Cancel = true;
                await Launcher.LaunchUriAsync(args.Uri);
            }
        }

        private void EpisodeListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            AnalyticsVersionInfo analyticsVersionInfo = AnalyticsInfo.VersionInfo;
            EpisodeListItem clickedEpisodeListItem = (EpisodeListItem)EpisodeListView.SelectedItem;
            if (clickedEpisodeListItem == null) return;
            setEpisode(clickedEpisodeListItem.Episode);
            EpisodeTitle = clickedEpisodeListItem.Episode.Title;
            if (analyticsVersionInfo.DeviceFamily == MOBILE_DEVICE_FAMILY)
            {
                clickedEpisodeListItem.GoToEpisodeDetails(clickedEpisodeListItem, e);
                return;
            }
            DescWebView.NavigateToThemedString(SelectedEpisode.Description);

            if (App.Settings.UseEpisodeArtwork &&
                clickedEpisodeListItem.Episode.HasUniqueArtwork &&
                NetworkInterface.GetIsNetworkAvailable())
            {
                Artwork = clickedEpisodeListItem.Episode.Artwork.GetBestArtworkSoruce();
            }
        }
    }
}
