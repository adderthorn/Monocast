using System;
using System.ComponentModel;
using Windows.ApplicationModel.DataTransfer;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Monosoftware.Podcast;
using System.Runtime.Serialization;
using Windows.Storage;
using System.IO;
using System.Xml;
using System.Threading;
using System.Diagnostics;
using Windows.UI.Xaml.Navigation;
using System.Threading.Tasks;
using Windows.Storage.Pickers;
using Windows.UI.Popups;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Monocast.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class AddFeedView : Page, INotifyPropertyChanged
    {
        private Type _PageAfterAdding = typeof(PodcastView);
        private string _StatusText = "...";
        private const string HTTP = "http://";
        private const string HTTPS = "https://";

        public event PropertyChangedEventHandler PropertyChanged;

        public string FeedUri { get; set; }
        public string StatusText
        {
            get => _StatusText;
            set
            {
                if (_StatusText != value)
                {
                    _StatusText = value;
                    RaisePropertyChanged(nameof(StatusText));
                }
            }
        }
        public Subscriptions Subscriptions => App.Subscriptions;

        public AddFeedView()
        {
            this.InitializeComponent();
            StatusText = "Click Subscribe to Add Feed";
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            if (e.Parameter is string)
            {
                FeedUri = (string)e.Parameter;
                _PageAfterAdding = typeof(SubscriptionView);
                await SubscribeToFeedAsync();
            }
            else if (e.Parameter is Uri)
            {
                FeedUri = ((Uri)e.Parameter).AbsoluteUri;
                _PageAfterAdding = typeof(SubscriptionView);
                await SubscribeToFeedAsync();
            }
        }

        private async void SubscribeButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(FeedUri)) return;
            SubscribeButton.IsEnabled = false;
            StatusText = "Checking Feed...";
            if (!FeedUri.ToLower().StartsWith(HTTP) && !FeedUri.ToLower().StartsWith(HTTPS))
                FeedUri = HTTP + FeedUri;
            RaisePropertyChanged(nameof(FeedUri));
            await SubscribeToFeedAsync();
            SubscribeButton.IsEnabled = true;
        }

        private async Task SubscribeToFeedAsync()
        {
            Podcast podcast = null;
            try
            {
                var uriStatus = await CastHelpers.CheckUriValidAsync(FeedUri);
                if (uriStatus.UriStatus == UriStatus.Valid)
                {
                    StatusText = "Fetching Podcast Information...";
                    podcast = await Podcast.GetPodcastAsync(FeedUri);
                    StatusText = "Adding Podcast...";
                    Subscriptions.AddPodcast(podcast);
                }
                switch (uriStatus.UriStatus)
                {
                    case UriStatus.Malformed:
                        StatusText = "The feed entered does not appear to be a valid URL.";
                        return;
                    case UriStatus.NetworkError:
                        StatusText = "There appears to be an issue connecting to the server. Please check connection and try again.";
                        return;
                    case UriStatus.HttpError:
                        StatusText = string.Format("The Server returned an invalid response: {0} {1}", (int)uriStatus.HttpStatusCode, uriStatus.HttpStatusCode);
                        return;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                StatusText = "URI is not a valid RSS feed, please check URL and try again.";
                return;
            }

            if (App.Settings.CachePodcastArtwork && podcast != null)
            {
                StatusText = "Fetching Artwork...";
                if (!podcast.Artwork?.IsDownloaded == true) await podcast.Artwork.DownloadFileAsync();
                podcast.Artwork.SaveToFile(podcast.Title);
            }

            StatusText = "Saving Subscriptions";
            try
            {
                await Utilities.SaveSubscriptionsAsync(Subscriptions);
            }
            catch (Exception ex)
            {
                MessageDialog messageDialog = new MessageDialog(ex.Message, "Error");
                await messageDialog.ShowAsync();
                return;
            }
            StatusText = "Done!";
            this.Frame.Navigate(_PageAfterAdding, podcast);
        }

        private void RaisePropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private async void buttonOpml_Click(object sender, RoutedEventArgs e)
        {
            FileOpenPicker picker = new FileOpenPicker()
            {
                SuggestedStartLocation = PickerLocationId.Desktop,
            };
            picker.FileTypeFilter.Add(".opml");
            picker.FileTypeFilter.Add(".xml");
            picker.ViewMode = PickerViewMode.List;
            buttonOpml.IsEnabled = false;
            StorageFile opmlFile = await picker.PickSingleFileAsync();
            if (opmlFile != null)
            {
                StatusText = "Parsing OPML...";
                var opmlStream = await opmlFile.OpenStreamForReadAsync();
                try
                {
                    var progressIndicator = new Progress<string>(p => StatusText = string.Format("Adding: {0}", p));
                    var errorCount = await Subscriptions.AddPodcastsFromOpmlAsync(opmlStream, progressIndicator);
                    if (errorCount > 0)
                    {
                        string msg = string.Format("Error importing {0} feeds. These feeds have been skipped.", errorCount);
                        var dialog = new MessageDialog(msg, "Warning");
                        await dialog.ShowAsync();
                    }
                }
                catch (XmlException)
                {
                    var dialog = new MessageDialog("Error parsing OPML, please ensure the format is correct.", "Error");
                    await dialog.ShowAsync();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                    var dialog = new MessageDialog("General failure");
                    await dialog.ShowAsync();
                }
                if (App.Settings.CachePodcastArtwork)
                {
                    StatusText = "Fetching Artwork...";
                    await Subscriptions.RefreshPodcastArtworkAsync(ForceUpdate: true);
                }
                StatusText = "Done!";
                await Task.Delay(1000);
                Frame.Navigate(typeof(SubscriptionView));
            }
            buttonOpml.IsEnabled = true;
        }

        private async void PasteButton_Click(object sender, RoutedEventArgs e)
        {
            DataPackageView dataPackageView = Clipboard.GetContent();
            if (dataPackageView.Contains(StandardDataFormats.Text))
            {
                string text = await dataPackageView.GetTextAsync();
                if (!string.IsNullOrWhiteSpace(text))
                {
                    TextBoxFeed.Text = text;
                    TextBoxFeed.Focus(FocusState.Keyboard);
                }
            }
        }

        //private async void buttonOpmlUri_Click(object sender, RoutedEventArgs e)
        //{
        //    buttonOpmlUri.IsEnabled = false;
        //    StatusText = "Checking OPML...";
        //    if (!FeedUri.ToLower().StartsWith(HTTP) && !FeedUri.ToLower().StartsWith(HTTPS))
        //        FeedUri = HTTP + FeedUri;
        //    RaisePropertyChanged(nameof(FeedUri));
        //    var progressIndicator = new Progress<string>(p => StatusText = string.Format("Adding: {0}", p));
        //    if (await CastHelpers.CheckUriValidAsync(FeedUri))
        //    {
        //        var feedUri = new Uri(FeedUri);
        //        var errorCount = await Subscriptions.AddPocastsFromOpmlUriAsync(feedUri, progressIndicator);
        //        if (App.Settings.CachePodcastArtwork)
        //        {
        //            StatusText = "Fetching Artwork...";
        //            await Subscriptions.RefreshPodcastArtworkAsync(ForceUpdate: true);      
        //        }
        //        StatusText = "Done!";
        //        await Task.Delay(1000);
        //        Frame.Navigate(typeof(SubscriptionView));
        //    }
        //    buttonOpmlUri.IsEnabled = true;
        //}
    }
}
