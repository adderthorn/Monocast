using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using System.Xml;
using Microsoft.SyndicationFeed;
using Microsoft.SyndicationFeed.Rss;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace Monosoftware.Podcast
{
    [DataContract(IsReference = true)]
    public class Podcast: INotifyPropertyChanged
    {
        #region Class Constructors
        /// <summary>
        /// Default constructor.
        /// </summary>
        public Podcast()
        {
            Episodes = new ObservableCollection<Episode>();
        }

        /// <summary>
        /// Creates an instance fo the Podcast class with a FeedUri specified.
        /// </summary>
        /// <param name="FeedUri">RSS URI for the podcast.</param>
        public Podcast(Uri FeedUri)
        {
            this.FeedUri = FeedUri;
            Episodes = new ObservableCollection<Episode>();
        }
        #endregion Class Constructors

        #region Event Handlers
        public event PropertyChangedEventHandler PropertyChanged;

        private void raisePropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void raiseEpisodeChanged(object sender, PropertyChangedEventArgs e)
        {
            raisePropertyChanged(e.PropertyName);
        }

        private void episodes_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.OldItems != null)
            {
                foreach (INotifyPropertyChanged item in e.OldItems)
                {
                    item.PropertyChanged -= raiseEpisodeChanged;
                }
            }
            if (e.NewItems != null)
            {
                foreach (INotifyPropertyChanged item in e.NewItems)
                {
                    item.PropertyChanged += raiseEpisodeChanged;
                }
            }
        }
        #endregion

        #region Private Variables
        private const int DEFAULT_MAX_EPISODES = 20;

        private DateTime _LastBuildDate = DateTime.MinValue;
        private DateTime _LastRefreshDate = DateTime.MinValue;
        private int _MaxEpisodes = DEFAULT_MAX_EPISODES;
        private string _Title;
        private Uri _FeedUri;
        private Uri _Link;
        private string _Author;
        private string _Generator;
        private string _Language;
        private string _Editor;
        private string _Webmaster;
        private int _Ttl;
        private string _Description;
        private ArtworkInfo _Artwork;
        private string _Copyright;
        private uint _SortOrder;
        private ObservableCollection<Episode> _Episodes;
        #endregion

        #region Public Properties
        /// <summary>
        /// Uri to the RSS feed; this value can only be set when creating a new
        /// instance of the Podcast class.
        /// </summary>

        [DataMember]
        public Uri FeedUri
        {
            get => _FeedUri;
            private set
            {
                if (value != _FeedUri)
                {
                    _FeedUri = value;
                    raisePropertyChanged(nameof(FeedUri));
                }
            }
        }

        /// <summary>
        /// The title of the podcast.
        /// </summary>
        [DataMember]
        public string Title
        {
            get => _Title;
            set
            {
                value = value.Trim();
                if (value != _Title)
                {
                    _Title = value;
                    raisePropertyChanged(nameof(Title));
                }
            }
        }

        /// <summary>
        /// Link to the podcast's homepage.
        /// </summary>
        [DataMember]
        public Uri Link
        {
            get => _Link;
            set
            {
                if (value != _Link)
                {
                    _Link = value;
                    raisePropertyChanged(nameof(Link));
                }
            }
        }

        /// <summary>
        /// Podcast author.
        /// </summary>
        [DataMember]
        public string Author
        {
            get => _Author;
            set
            {
                if (value != _Author)
                {
                    _Author = value;
                    raisePropertyChanged(nameof(Author));
                }
            }
        }

        /// <summary>
        /// Generator of the podcast feed.
        /// </summary>
        [DataMember]
        public string Generator
        {
            get => _Generator;
            set
            {
                if (value != _Generator)
                {
                    _Generator = value;
                    raisePropertyChanged(nameof(Generator));
                }
            }
        }

        /// <summary>
        /// Language of the podcast.
        /// </summary>
        [DataMember]
        public string Language
        {
            get => _Language;
            set
            {
                if (value != _Language)
                {
                    _Language = value;
                    raisePropertyChanged(nameof(Language));
                }
            }
        }

        /// <summary>
        /// Copyright information about the podcast.
        /// </summary>
        [DataMember]
        public string Copyright
        {
            get => _Copyright;
            set
            {
                if (value != _Copyright)
                {
                    _Copyright = value;
                    raisePropertyChanged(nameof(Copyright));
                }
            }
        }

        /// <summary>
        /// Editor of the podcast.
        /// </summary>
        [DataMember]
        public string Editor
        {
            get => _Editor;
            set
            {
                if (value != _Editor)
                {
                    _Editor = value;
                    raisePropertyChanged(nameof(Editor));
                }
            }
        }

        /// <summary>
        /// Webmaster of the podcast.
        /// </summary>
        [DataMember]
        public string Webmaster
        {
            get => _Webmaster;
            set
            {
                if (value != _Webmaster)
                {
                    _Webmaster = value;
                    raisePropertyChanged(nameof(Webmaster));
                }
            }
        }

        /// <summary>
        /// Podcast TTL (Time to live).
        /// </summary>
        [DataMember]
        public int Ttl
        {
            get => _Ttl;
            set
            {
                if (value != _Ttl)
                {
                    _Ttl = value;
                    raisePropertyChanged(nameof(Ttl));
                }
            }
        }

        /// <summary>
        /// Max number of episodes of the podcast to be stored.
        /// </summary>
        [DataMember]
        public int MaxEpisodes
        {
            get => _MaxEpisodes;
            set
            {
                if (value != _MaxEpisodes && value > -1)
                {
                    _MaxEpisodes = value;
                    raisePropertyChanged(nameof(MaxEpisodes));
                }
            }
        }

        /// <summary>
        /// Last build date of the podcast from the RSS feed.
        /// </summary>
        [DataMember]
        public DateTime LastBuildDate
        {
            get => _LastBuildDate;
            set
            {
                if (value != _LastBuildDate)
                {
                    _LastBuildDate = value;
                    raisePropertyChanged(nameof(LastBuildDate));
                }
            }
        }

        /// <summary>
        /// Last refreshed date of the podcast (to be set by the application using this library).
        /// </summary>
        [DataMember]
        public DateTime LastRefreshDate
        {
            get => _LastRefreshDate;
            set
            {
                if (value != _LastRefreshDate)
                {
                    _LastRefreshDate = value;
                    raisePropertyChanged(nameof(LastRefreshDate));
                }
            }
        }

        /// <summary>
        /// Podcast description.
        /// </summary>
        [DataMember]
        public string Description
        {
            get => _Description;
            set
            {
                if (value != _Description)
                {
                    _Description = value;
                    raisePropertyChanged(nameof(Description));
                }
            }
        }

        /// <summary>
        /// Podcast artwork information.
        /// </summary>
        [DataMember]
        public ArtworkInfo Artwork
        {
            get => _Artwork;
            set
            {
                if (value != _Artwork)
                {
                    _Artwork = value;
                    raisePropertyChanged(nameof(Artwork));
                }
            }
        }

        /// <summary>
        /// All the episodes that belong to this podcast.
        /// </summary>
        [DataMember]
        public ObservableCollection<Episode> Episodes
        {
            get => _Episodes;
            set
            {
                _Episodes = value;
                _Episodes.CollectionChanged += episodes_CollectionChanged;
            }
        }

        /// <summary>
        /// Sort order for the podcasts.
        /// </summary>
        [DataMember(Order = 1)]
        public uint SortOrder
        {
            get => _SortOrder;
            set
            {
                if (value != _SortOrder)
                {
                    _SortOrder = value;
                    raisePropertyChanged(nameof(SortOrder));
                }
            }
        }

        /// <summary>
        /// A count of the number of episodes currently saved with this podcast.
        /// </summary>
        public int EpisodeCount => Episodes.Count;
        #endregion

        #region Public Methods
        public Episode GetMostCurrentEpisodes()
        {
            Episode returnEpisode = (from e in Episodes
                                     select e).FirstOrDefault();
            return returnEpisode;
        }

        public List<Episode> GetMostCurrentEpisodes(int NumberOfEpisodes)
        {
            List<Episode> returnEpisodes = Episodes.Take(NumberOfEpisodes).ToList();
            return returnEpisodes;
        }

        public async Task RefreshPodcastAsync(bool UseEpisodeArtwork, bool AppendToEnd)
        {
            int AppendIndex = AppendToEnd ? -1 : 0;
            HttpClient httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add(CastHelpers.USER_AGENT_TEXT, CastHelpers.UserAgent);
            var request = new HttpRequestMessage(HttpMethod.Get, FeedUri);
            var responseMessage = await httpClient.SendAsync(request, HttpCompletionOption.ResponseContentRead);
            RssFeedReader feed;
            int episodeCount = 0;
            using (Stream responseStream = await responseMessage.Content.ReadAsStreamAsync())
            {
                using (var reader = XmlReader.Create(responseStream, new XmlReaderSettings() { Async = true, DtdProcessing = DtdProcessing.Ignore }))
                {
                    feed = new RssFeedReader(reader);
                    try
                    {
                        while (await feed.Read())
                        {
                            if (feed.ElementType == SyndicationElementType.Item)
                            {
                                if (episodeCount++ >= MaxEpisodes) break;
                                AddEpisodeFromSyndicationContent(await feed.ReadContent(), AppendIndex);
                            }
                        }
                    }
                    catch (XmlException)
                    {
                        // TODO: Probably need to handle this somehow to alert the user the feed no longer exists
                        return;
                    }
                }
            }
            if (!AppendToEnd) Episodes = new ObservableCollection<Episode>(Episodes.OrderByDescending(e => e.PublishDate));
        }

        public void ShrinkEpisodesToCount(int count)
        {
            if (EpisodeCount <= count) return;
            Episodes = new ObservableCollection<Episode>(Episodes.OrderByDescending(ep => ep.PublishDate).Take(count));
        }

        public void SetFeedUri(Uri FeedUri)
        {
            this.FeedUri = FeedUri;
        }

        public void SetFeedUri(string FeedUri)
        {
            this.FeedUri = new Uri(FeedUri);
        }

        public void AddEpisodeFromSyndicationContent(ISyndicationContent syndicationContent, int InsertIndex)
        {
            ISyndicationItem item = new RssParser(AllowNullLinks: true).CreateItem(syndicationContent);
            if (this.Episodes.Count(e => string.Equals(e.Title, item.Title.Trim(), StringComparison.OrdinalIgnoreCase) && e.PublishDate == item.Published) > 0) return;
            Episode episode = new Episode(this)
            {
                Title = item.Title,
                Description = item.Description,
                PublishDate = item.Published,
                Duration = TimeSpan.MinValue
            };
            ISyndicationContent content = syndicationContent.Fields.FirstOrDefault(c => string.Equals(c.Name, RssElementNamesExt.Encoded, StringComparison.OrdinalIgnoreCase)
                && string.Equals(c.Namespace, XmlNamespaces.Content, StringComparison.OrdinalIgnoreCase));
            if (content != null) episode.Description = content.Value;

            ISyndicationLink mediaUri = item.Links.FirstOrDefault(l => string.Equals(l.RelationshipType, RssLinkTypes.Enclosure, StringComparison.OrdinalIgnoreCase));
            if (mediaUri == null)
                mediaUri = item.Links.FirstOrDefault(l => string.Equals(l.MediaType, RssElementNamesExt.MP3, StringComparison.OrdinalIgnoreCase));
            if (mediaUri == null)
                mediaUri = new SyndicationLink(new Uri(CastHelpers.DEFAULT_MP3));
            episode.MediaSource = mediaUri.Uri;
            TimeSpan epDuration = TimeSpan.MinValue;
            //if (TimeSpan.TryParse(syndicationContent.Fields.FirstOrDefault(c => string.Equals(c.Name, RssElementNamesExt.Duration, StringComparison.OrdinalIgnoreCase)
            //        && string.Equals(c.Namespace, XmlNamespaces.iTunes, StringComparison.OrdinalIgnoreCase))?.Value, out epDuration))
            //    episode.Duration = epDuration;
            string ts = syndicationContent.Fields.FirstOrDefault(c => string.Equals(c.Name, RssElementNamesExt.Duration, StringComparison.OrdinalIgnoreCase)
                    && string.Equals(c.Namespace, XmlNamespaces.iTunes, StringComparison.OrdinalIgnoreCase))?.Value;
            if (!string.IsNullOrEmpty(ts))
            {
                string[] tsArr = ts.Split(':');
                int hours, minutes, seconds;
                switch (tsArr.Length)
                {
                    case 2:
                        if (int.TryParse(tsArr[0], out minutes) && int.TryParse(tsArr[1], out seconds))
                        {
                            episode.Duration = new TimeSpan(0, minutes, seconds);
                        }
                        break;
                    case 3:
                        if (int.TryParse(tsArr[0], out hours) && int.TryParse(tsArr[1], out minutes) && int.TryParse(tsArr[2], out seconds))
                        {
                            episode.Duration = new TimeSpan(hours, minutes, seconds);
                        }
                        break;
                    case 1:
                        if (int.TryParse(tsArr[0], out seconds))
                        {
                            episode.Duration = new TimeSpan(0, 0, seconds);
                        }
                        break;
                }
            }
            if (episode.Duration == TimeSpan.MinValue)
                episode.Duration = TimeSpan.MaxValue;
            Uri artworkUri = CastHelpers.CheckForUri(syndicationContent.Fields.FirstOrDefault(c => string.Equals(c.Name, RssElementNames.Image, StringComparison.OrdinalIgnoreCase)
                && string.Equals(c.Namespace, XmlNamespaces.iTunes, StringComparison.OrdinalIgnoreCase)));
            if (artworkUri == null)
                artworkUri = CastHelpers.CheckForUri(syndicationContent.Fields.FirstOrDefault(c => string.Equals(c.Name, RssElementNamesExt.Artwork, StringComparison.OrdinalIgnoreCase)));
            if (artworkUri != null)
                episode.Artwork = new ArtworkInfo(artworkUri);
            episode.GUID = syndicationContent.Fields.FirstOrDefault(c => string.Equals(c.Name, RssElementNames.Guid, StringComparison.OrdinalIgnoreCase)).Value;
            episode.GenerateGUID();
            if (episode.DurationLong == 0) episode.Duration = TimeSpan.MaxValue;
            if (InsertIndex < 0 || InsertIndex >= this.EpisodeCount)
            {
                this.Episodes.Add(episode);
            }
            else
            {
                this.Episodes.Insert(InsertIndex, episode);
            }
        }
        #endregion

        #region Static Methods
        /// <summary>
        /// Gets a new instance of a podcast asynchronously from a feed Uri of the Uri class.
        /// </summary>
        /// <param name="FeedUri">Uri of the podcast's RSS feed.</param>
        /// <returns>An awaitable podcast.</returns>
        public static async Task<Podcast> GetPodcastAsync(Uri FeedUri) => await GetPodcastAsync(FeedUri, DEFAULT_MAX_EPISODES);

        /// <summary>
        /// Gets a new instance of a podcast asynchronously from a feed Uri of the string class.
        /// </summary>
        /// <param name="FeedUri">Uri of the podcast's RSS feed.</param>
        /// <returns>An awaitable podcast.</returns>
        public static async Task<Podcast> GetPodcastAsync(string FeedUri)
        {
            Uri newUri = new Uri(FeedUri);
            return await GetPodcastAsync(newUri, DEFAULT_MAX_EPISODES);
        }

        /// <summary>
        /// Gets a new instance of a podcast asynchronously from a feed Uri of the string class
        /// with a specified number of maximum episodes.
        /// </summary>
        /// <param name="FeedUri">Uri of the podcast's RSS feed.</param>
        /// <param name="MaxEpisodes">Maximum number of episodes of the podcast to be stored.</param>
        /// <returns>An awaitable podcast.</returns>
        public static async Task<Podcast> GetPodcastAsync(string FeedUri, int MaxEpisodes)
        {
            Uri newUri = new Uri(FeedUri);
            return await GetPodcastAsync(newUri, MaxEpisodes);
        }

        /// <summary>
        /// Gets a new instance of a podcast asynchronously from a feed Uri of the Uri class
        /// with a specified number of maximum episodes.
        /// </summary>
        /// <param name="FeedUri">Uri of the podcast's RSS feed.</param>
        /// <param name="MaxEpisodes">Maximum number of episodes of the podcast to be stored.</param>
        /// <returns>An awaitable podcast.</returns>
        public static async Task<Podcast> GetPodcastAsync(Uri FeedUri, int MaxEpisodes)
        {
            HttpClient httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add(CastHelpers.USER_AGENT_TEXT, CastHelpers.UserAgent);
            var httpRequest = new HttpRequestMessage(HttpMethod.Get, FeedUri);
            var responseMessage = await httpClient.SendAsync(httpRequest, HttpCompletionOption.ResponseContentRead);
            Stream s = await responseMessage.Content.ReadAsStreamAsync();
            return await GetPodcastFromStreamAsync(s, MaxEpisodes, FeedUri);
        }

        /// <summary>
        /// Gets a new instance of a podcast asynchornously from a stream with the specified
        /// number of episodes and the optionally specified Feed URI.
        /// </summary>
        /// <param name="RssStream">Stream of the RSS content for the podcast.</param>
        /// <param name="MaxEpisodes">Maximum number of episodes of the podcast to be stored.</param>
        /// <param name="FeedUri">Uri of the podcast's RSS feed.</param>
        /// <returns>An awaitable podcast.</returns>
        public static async Task<Podcast> GetPodcastFromStreamAsync(Stream RssStream, int MaxEpisodes, Uri FeedUri = null)
        {
            RssFeedReader feedReader;
            var podcast = new Podcast(FeedUri)
            {
                LastBuildDate = DateTime.Now,
                LastRefreshDate = DateTime.Now
            };
            var readerSettings = new XmlReaderSettings()
            {
                Async = true,
                DtdProcessing = DtdProcessing.Ignore
            };
            using (var reader = XmlReader.Create(RssStream, readerSettings))
            {
                feedReader = new RssFeedReader(reader);
                bool foundHighResImage = false;
                bool foundBuildDate = false;
                bool foundLink = false;
                int episodeCount = 0;
                try
                {
                    while (await feedReader.Read())
                    {
                        switch (feedReader.ElementName)
                        {
                            case RssElementNames.Title:
                                podcast.Title = await feedReader.ReadValue<string>();
                                continue;
                            case RssElementNames.Link:
                                // workaround for Megaphone.FM links that contain RSS feed in an atom link
                                var content = await feedReader.ReadContent();
                                if (content.Attributes.Any(a => a.Name == RssElementNamesExt.Type && a.Value == RssElementNamesExt.RssXml))
                                {
                                    continue;
                                }
                                if (!foundLink)
                                {
                                    Uri linkUri = CastHelpers.CheckForUri(content);
                                    if (linkUri != null)
                                    {
                                        podcast.Link = linkUri;
                                        foundLink = true;
                                    }
                                }
                                continue;
                            case RssElementNamesExt.Subtitle:
                                podcast.Description = await feedReader.ReadValue<string>();
                                continue;
                            case RssElementNamesExt.Artwork:
                            case RssElementNames.Image:
                                Uri artworkUri = CastHelpers.CheckForUri(await feedReader.ReadContent());
                                if (artworkUri == null) continue;
                                var artworkUriStatus = await CastHelpers.CheckUriValidAsync(artworkUri);
                                if (artworkUriStatus.UriStatus == UriStatus.Valid)
                                {
                                    foundHighResImage = true;
                                    podcast.Artwork = new ArtworkInfo(artworkUri);
                                    await podcast.Artwork.DownloadFileAsync();
                                }
                                continue;
                            case RssElementNames.Copyright:
                                podcast.Copyright = await feedReader.ReadValue<string>();
                                continue;
                            case RssElementNames.Language:
                                podcast.Language = await feedReader.ReadValue<string>();
                                continue;
                            case RssElementNames.LastBuildDate:
                            case RssElementNames.PubDate:
                                if (!foundBuildDate)
                                {
                                    DateTime buildDate;
                                    try
                                    {
                                        buildDate = await feedReader.ReadValue<DateTime>();
                                        foundBuildDate = true;
                                    }
                                    catch
                                    {
                                        buildDate = DateTime.MinValue;
                                    }
                                    podcast.LastBuildDate = buildDate;
                                }
                                continue;
                            case RssElementNames.Author:
                                podcast.Author = await feedReader.ReadValue<string>();
                                continue;
                            case RssElementNames.TimeToLive:
                                podcast.Ttl = await feedReader.ReadValue<int>();
                                continue;
                            case RssElementNames.Generator:
                                podcast.Generator = await feedReader.ReadValue<string>();
                                continue;
                            default:
                                break;
                        }
                        switch (feedReader.ElementType)
                        {
                            case SyndicationElementType.Image:
                                if (!foundHighResImage)
                                {
                                    ISyndicationImage artworkImage = await feedReader.ReadImage();
                                    podcast.Artwork.MediaSource = artworkImage.Url;
                                    await podcast.Artwork.DownloadFileAsync();
                                }
                                continue;
                            case SyndicationElementType.Item:
                                if (episodeCount == 0 || episodeCount < MaxEpisodes)
                                {
                                    podcast.AddEpisodeFromSyndicationContent(await feedReader.ReadContent(), -1);
                                    episodeCount++;
                                }
                                continue;
                            case SyndicationElementType.Link:
                                if (!foundLink)
                                {
                                    Uri linkUri = CastHelpers.CheckForUri(await feedReader.ReadContent());
                                    if (linkUri != null)
                                    {
                                        podcast.Link = linkUri;
                                        foundLink = true;
                                    }
                                }
                                continue;
                            default:
                                break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    throw ex;
                }
            }
            return podcast;
        }
        #endregion

        #region Private Methods
        [OnDeserialized()]
        internal void OnDeserialized(StreamingContext context)
        {
            foreach (var episode in Episodes)
            {
                episode.PropertyChanged += raiseEpisodeChanged;
            }
        }
        #endregion
    }
}
