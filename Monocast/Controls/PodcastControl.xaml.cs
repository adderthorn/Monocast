using System;
using System.ComponentModel;
using System.IO;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Monosoftware.Podcast;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.Graphics.Imaging;
using System.Reflection;
using System.Threading.Tasks;
using System.Net.NetworkInformation;
using System.Linq;
using Windows.ApplicationModel.DataTransfer;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Monocast.Controls
{
    public sealed partial class PodcastControl : UserControl, INotifyPropertyChanged
    {
        public PodcastControl(Podcast Podcast)
        {
            this.Podcast = Podcast;
            RaisePropertyChanged(nameof(Podcast), nameof(Title));
            InitializeComponent();
        }

        private const int LARGE_TITLE = 26;
        private const string HELLIP = "...";
        private const string PLACEHOLDER_ARTWORK = "Monocast.Resources.placeholder_image.bmp";
        private Podcast _Podcast;
        public Podcast Podcast
        {
            get => _Podcast;
            set
            {
                if (_Podcast != value)
                {
                    _Podcast = value;
                    RaisePropertyChanged(nameof(ArtworkSource), nameof(Podcast), nameof(Title));
                }
            }
        }
        public Uri ArtworkSource => Podcast.Artwork.GetBestArtworkSoruce();
        public string Title => FixTitle();
        public bool HasUnreadEpisodes => _Podcast.Episodes.Any(e => !e.IsPlayed && !e.IsArchived);
        public string ToolTip
        {
            get
            {
                string result = Podcast.Title;
                if (HasUnreadEpisodes)
                {
                    int unreadCount = _Podcast.Episodes.Count(e => !e.IsPlayed && !e.IsArchived);
                    result = string.Format("({0:N0}) {1}", unreadCount, result);
                }
                return result;
            }
        }
        public RoutedEventHandler UnsubscribePodcast;
        public delegate void ArtworkCallback();

        public event PropertyChangedEventHandler PropertyChanged;

        private string FixTitle()
        {
            Podcast.Title = Podcast.Title.Trim();
            if (Podcast.Title.Length > LARGE_TITLE)
            {
                return Podcast.Title.Substring(0, LARGE_TITLE - HELLIP.Length).Trim() + HELLIP;
            }
            return Podcast.Title;
        }

        private void UnsubscribeMenuFlyoutItem_Click(object sender, RoutedEventArgs e) => this.UnsubscribePodcast?.Invoke(this, e);

        private void RaisePropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        private void RaisePropertyChanged(params string[] names)
        {
            for (int i = 0; i < names.Length; i++)
            {
                RaisePropertyChanged(names[i]);
            }
        }

        private void MarkAllPlayed_Click(object sender, RoutedEventArgs e)
        {
            foreach (Episode ep in _Podcast.Episodes)
            {
                ep.IsPlayed = true;
            }
            RaisePropertyChanged(nameof(HasUnreadEpisodes));
        }

        private void CopyFeedUrl_Click(object sender, RoutedEventArgs e)
        {
            DataPackage dataPackage = new DataPackage() { RequestedOperation = DataPackageOperation.Copy };
            dataPackage.SetText(_Podcast.FeedUri.AbsoluteUri);
            Clipboard.SetContent(dataPackage);
        }
    }
}
