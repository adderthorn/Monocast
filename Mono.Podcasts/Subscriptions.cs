using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using System.IO;
using System.Xml.Linq;
using System.Diagnostics;
using System.ComponentModel;
using System.Text;
using System.Xml;
using System.Net.Http;
using System.Collections.Specialized;

namespace Monosoftware.Podcast
{
    /// <summary>
    /// Class that stores subscriptions to podcasts.
    /// </summary>
    [DataContract]
    public class Subscriptions : INotifyPropertyChanged
    {
        #region Constants
        /// <summary>
        /// Span of 10-seconds, used to indicate a major time of listenting has occured; can be used to 
        /// determine if the playback position of the podcast has changed and therefore should be saved.
        /// </summary>
        public static readonly TimeSpan MajorTimespanChange = new TimeSpan(0, 0, 10); // 10 Seconds

        /// <summary>
        /// Constant value used in OPML files to specify the URL to the RSS feed.
        /// </summary>
        private const string xmlUrl = "xmlUrl";

        /// <summary>
        /// Version of OPML.
        /// </summary>
        private const string opmlVersion = "1.1";
        #endregion Constants

        #region Private Variables
        private ObservableCollection<Podcast> _Podcasts = new ObservableCollection<Podcast>();
        private Episode _ActiveEpisode;
        #endregion Private Variables

        #region Public Properties
        /// <summary>
        /// Event handler for when a property of the subscriptions has changed.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Gets or sets the last modified date of the subscriptions.
        /// </summary>
        [DataMember(Order = 0)]
        public DateTime LastModifiedDate { get; set; }

        /// <summary>
        /// Gets or sets the collection of all of the subscribed podcasts.
        /// </summary>
        [DataMember(Order = 1)]
        public ObservableCollection<Podcast> Podcasts
        {
            get => _Podcasts;
            set
            {
                _Podcasts = value;
                _Podcasts.CollectionChanged += podcasts_CollectionChanged;
            }
        }

        public Episode ActiveEpisode
        {
            get => _ActiveEpisode;
            set
            {
                if (value != _ActiveEpisode)
                {
                    _ActiveEpisode = value;
                    raiseNotifyPropertyChanged(nameof(ActiveEpisode));
                }    
            }
        }
        #endregion

        #region Constructors
        /// <summary>
        /// Initializes an empty subscriptions class.
        /// </summary>
        public Subscriptions()
        {
            Podcasts = new ObservableCollection<Podcast>();
        }
        #endregion Constructors

        #region Public Methods
        /// <summary>
        /// Adds a podcast to the current subscrition.
        /// </summary>
        /// <param name="podcast">Podcast to add.</param>
        public void AddPodcast(Podcast podcast)
        {
            if (podcast.SortOrder == 0)
            {
                podcast.SortOrder = getNextSortOrder();
            }
            Podcasts.Add(podcast);
        }

        /// <summary>
        /// Removes a podcast from the current subscription.
        /// </summary>
        /// <param name="podcast">Podcast to be removed.</param>
        public void RemovePodcast(Podcast podcast)
        {
            Podcasts.Remove(podcast);
        }

        /// <summary>
        /// Gets all of the subscribes podcasts where the LocalArtworkPath is empty.
        /// </summary>
        /// <returns>Podcasts with no artwork.</returns>
        public ObservableCollection<Podcast> GetPodcastsWithNoLocalArtworkUri()
        {
            var resultList = new ObservableCollection<Podcast>();
            Podcasts.Where(p => string.IsNullOrEmpty(p.Artwork.LocalArtworkPath)).ToList().ForEach(a => resultList.Add(a));
            return resultList;
        }

        /// <summary>
        /// Refreshes the podcast feeds and adds any new episodes that have been added since the last refresh.
        /// </summary>
        /// <param name="UseEpisodeArtwork">If true, the episode artwork will be downloaded and used if available.</param>
        /// <param name="TotalEpisodesToKeep">Number of episodes to keep for each podcast.</param>
        /// <param name="AppendToEnd">If true, new podcast episodes will be added to the end of the list rather than at the beginning.</param>
        /// <returns></returns>
        public async Task RefreshFeedsAsync(bool UseEpisodeArtwork = false, int TotalEpisodesToKeep = 0, bool AppendToEnd = true)
        {
            var errorPodcasts = new Dictionary<Podcast, string>();
            int notResolvedCount = 0;
            foreach (var podcast in Podcasts)
            {
                try
                {
                    if (TotalEpisodesToKeep > 0) podcast.ShrinkEpisodesToCount(TotalEpisodesToKeep);
                    await podcast.RefreshPodcastAsync(UseEpisodeArtwork, AppendToEnd);
                }
                catch (NullReferenceException ex)
                {
                    if (ex.Message.ToLower().Contains("uri not resolved"))
                        notResolvedCount++;
                    Debug.WriteLine("NullReferenceException! Got error '{0}' on podcast {1}.", ex.Message, podcast.Title);
                    errorPodcasts.Add(podcast, ex.Message);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("General Exception! Got error '{0}' on podcast {1}.", ex.Message, podcast.Title);
                    errorPodcasts.Add(podcast, ex.Message);
                }
                //if (errorPodcasts.Count > 1) break;
            }
            if (notResolvedCount > 0)
                throw new Exception("Network connection appears to be offline!");
            else if (errorPodcasts.Count > 0 && notResolvedCount == 0)
            {
                var messageBuilder = new StringBuilder("Error parsing podcast(s):");
                foreach (var kvp in errorPodcasts)
                {
                    messageBuilder.AppendLine(string.Format("{0} for reason {1}", kvp.Key.Title, kvp.Value));
                }
                throw new Exception(messageBuilder.ToString());
            }
            raiseNotifyPropertyChanged(nameof(Podcasts));
        }

        /// <summary>
        /// Gets the episodes that are currently pending download and have not already been downloaded.
        /// </summary>
        /// <param name="MaxCount">Maximum number of episodes to download for each podcast.</param>
        /// <returns>Collection of episodes to be downloaded.</returns>
        public IEnumerable<Episode> GetPendingDownloadEpisodes(int MaxCount = 0)
        {
            var episodeList = new List<Episode>();
            episodeList = (from ep in Podcasts.SelectMany(p => p.Episodes)
                           where ep.PendingDownload == true
                           && ep.Downloaded == false
                           select ep).ToList();
            if (MaxCount > 0)
            {
                episodeList = episodeList.OrderBy(ep => ep.PublishDate).Reverse().Take(MaxCount).ToList();
            }
            return episodeList;
        }

        /// <summary>
        /// Generates a new GUID for each podcast episode.
        /// </summary>
        /// <param name="OverwriteCurrent">If true, any episodes with exiting GUIDs will be overwritten, 
        /// otherwise existing GUIDs will be kept.</param>
        public void GenerateEpisodeGuids(bool OverwriteCurrent = false)
        {
            foreach (var p in Podcasts)
            {
                if (OverwriteCurrent)
                {
                    foreach (Episode e in p.Episodes)
                    {
                        e.GUID = new Guid().ToString();
                    }
                }
                else
                {
                    p.Episodes.Where(e => string.IsNullOrWhiteSpace(e.GUID)).ToList().ForEach(e => e.GUID = new Guid().ToString());
                }
            }
        }

        /// <summary>
        /// Returns the episode with the specified GUID; this will search through all podcasts.
        /// </summary>
        /// <param name="GUID">The GUID of the podcast to find.</param>
        /// <returns>The episode.</returns>
        public Episode GetEpisodeFromGuid(string GUID)
        {
            Episode episode = null;
            episode = Podcasts.SelectMany(p => p.Episodes).Where(e => e.GUID == GUID).FirstOrDefault();
            return episode;
        }

        /// <summary>
        /// Gets an enumeration of all episodes that have been downloaded and played to completion;
        /// useful to determine which episodes should be deleted from local storage.
        /// </summary>
        /// <param name="Count">Maximum number of episodes to be deleted.</param>
        /// <returns>List of episodes.</returns>
        public IEnumerable<Episode> GetEpisodesToRemove(int Count)
        {
            var totalEpisodeList = new List<Episode>();
            foreach (var p in Podcasts)
            {
                var episodeList = (from e in p.Episodes
                                   where e.Downloaded && (e.PlaybackPosition == e.Duration
                                   || e.PlaybackPosition == TimeSpan.MinValue)
                                   && !string.IsNullOrEmpty(e.LocalFileToken)
                                   orderby e.PublishDate ascending
                                   select e).ToList();
                if (episodeList.Count > Count)
                {
                    totalEpisodeList.AddRange(episodeList);
                }
            }
            return totalEpisodeList;
        }

        /// <summary>
        /// Finds a podcast in the subscription list by the specified name.
        /// </summary>
        /// <param name="PodcastName">The name of the podcast to find.</param>
        /// <returns>The podcast.</returns>
        public Podcast GetPodcastByName(string PodcastName)
        {
            Podcast podcast = null;
            podcast = (from p in Podcasts
                       where p.Title.Equals(PodcastName, StringComparison.CurrentCultureIgnoreCase)
                       orderby p.Title ascending
                       select p).FirstOrDefault();
            return podcast;
        }

        /// <summary>
        /// From the specified URI to an OPML on the internet, add each podcast that doesn't already exist to
        /// the current subscription list.
        /// </summary>
        /// <param name="OpmlUri">URI to the OPML feed.</param>
        /// <param name="progress">IProgress to present the current podcast being added while running.</param>
        /// <returns>Count of podcasts not added due to errors.</returns>
        public async Task<int> AddPocastsFromOpmlUriAsync(Uri OpmlUri, IProgress<string> progress)
        {
            progress?.Report("Downloading OPML...");
            HttpClient httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add(CastHelpers.USER_AGENT_TEXT, CastHelpers.UserAgent);
            var httpRequest = new HttpRequestMessage(HttpMethod.Get, OpmlUri);
            var responseMessage = await httpClient.SendAsync(httpRequest, HttpCompletionOption.ResponseContentRead);
            Stream s = await responseMessage.Content.ReadAsStreamAsync();
            progress?.Report("Parsing OPML...");
            return await AddPodcastsFromOpmlAsync(s, progress);
        }

        /// <summary>
        /// From the specified OPML stream, add each podcast that doesn't already exist to the current subscription list.
        /// </summary>
        /// <param name="OpmlStream">Stream of the OPML file.</param>
        /// <param name="progress">IProgress to present the current podcast being added while running.</param>
        /// <returns>Count of podcasts not added due to errors.</returns>
        public async Task<int> AddPodcastsFromOpmlAsync(Stream OpmlStream, IProgress<string> progress)
        {
            using (var reader = XmlReader.Create(OpmlStream))
            {
                if (!reader.IsStartElement(OpmlElementNames.Opml))
                {
                    throw new XmlException("Invalid OPML file");
                }
            }
            OpmlStream.Seek(0, SeekOrigin.Begin);
            XDocument doc = XDocument.Load(OpmlStream);
            IEnumerable<XElement> opmlElements = from d in doc.Descendants("outline")
                                                 where d.Attributes().Any(a => a.Name == xmlUrl)
                                                 select d;
            int errorCount = 0;
            foreach (XElement element in opmlElements)
            {
                string url = element.Attribute(xmlUrl).Value;
                string title = element.Attribute("title")?.Value;
                title = title ?? element.Attribute("text")?.Value;
                if (string.IsNullOrWhiteSpace(title)) title = url;
                progress?.Report(title);
                var uriStatus = await CastHelpers.CheckUriValidAsync(url);
                if (uriStatus.UriStatus != UriStatus.Valid)
                {
                    errorCount++;
                    continue;
                }
                this.AddPodcast(await Podcast.GetPodcastAsync(url));
            }
            return errorCount;
        }

        /// <summary>
        /// Creates an OPML stream from the current podcast subscriptions using
        /// the default XML writer settings.
        /// </summary>
        /// <param name="Title">Title of the OPML file.</param>
        /// <returns>Stream with the OPML data.</returns>
        public Stream CreateOpmlFromSubscriptions(string Title)
        {
            var opmlWriterSettings = new XmlWriterSettings()
            {
                Encoding = new UTF8Encoding(false, true),
                Indent = true,
                IndentChars = "  ",
                NewLineOnAttributes = false,
                NewLineChars = "\r\n",
                OmitXmlDeclaration = false,
                WriteEndDocumentOnClose = true,
                CheckCharacters = false,
                ConformanceLevel = ConformanceLevel.Auto,
                NamespaceHandling = NamespaceHandling.Default
            };
            return CreateOpmlFromSubscriptions(Title, opmlWriterSettings);
        }

        /// <summary>
        /// Creates an OPML stream from the current podcast subscriptions using
        /// specified XML writer settings.
        /// </summary>
        /// <param name="Title">Title of the OPML file.</param>
        /// <param name="WriterSettings">XML Writer Settings to be used.</param>
        /// <returns>Stream with the OPML data.</returns>
        public Stream CreateOpmlFromSubscriptions(string Title, XmlWriterSettings WriterSettings)
        {
            XElement head = new XElement("head",
                new XElement("title", Title),
                new XElement("dateCreated", DateTime.Now.ToString()));
            XElement body = new XElement("body");
            body.Add(new XElement("outline", new XAttribute("text", Title)));
            foreach (var podcast in Podcasts)
            {
                XElement podcastElement = new XElement("outline",
                    new XAttribute("text", podcast.Title),
                    new XAttribute("title", podcast.Title),
                    new XAttribute("type", "rss"),
                    new XAttribute(xmlUrl, podcast.FeedUri));
                body.Add(podcastElement);
            }
            XDocument doc = new XDocument(
                new XElement("opml",
                    new XAttribute("version", opmlVersion),
                    head,
                    body));
            var docStream = new MemoryStream();
            using (XmlWriter xmlWriter = XmlWriter.Create(docStream, WriterSettings))
            {
                doc.Save(docStream);
                docStream.Seek(0, SeekOrigin.Begin);
            }
            return docStream;
        }
        #endregion

        #region Private Methods
        private void raiseNotifyPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void raisePodcastChanged(object sender, PropertyChangedEventArgs e)
        {
            raiseNotifyPropertyChanged(e.PropertyName);
        }

        private void podcasts_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.OldItems != null)
            {
                foreach (INotifyPropertyChanged item in e.OldItems)
                {
                    item.PropertyChanged -= raisePodcastChanged;
                }
            }
            if (e.NewItems != null)
            {
                foreach (INotifyPropertyChanged item in e.NewItems)
                {
                    item.PropertyChanged += raisePodcastChanged;
                }
            }
        }

        [OnDeserialized()]
        internal void OnDeserialized(StreamingContext context)
        {
            foreach (var podcast in Podcasts)
            {
                podcast.PropertyChanged += raisePodcastChanged;
            }
        }

        private uint getNextSortOrder() => Podcasts.Max(p => p.SortOrder) + 1;
        #endregion

        #region Static Methods
        /// <summary>
        /// From a given enumeration of podcast Uris, create a subscription class.
        /// </summary>
        /// <param name="PodcastUris">Podcast RSS feed Uris to subscribe to.</param>
        /// <returns>An awaitable subscriptions.</returns>
        public static async Task<Subscriptions> GetSubscriptionsAsync(IEnumerable<Uri> PodcastUris)
        {
            Subscriptions subscriptions = new Subscriptions();
            foreach (Uri uri in PodcastUris)
            {
                Podcast podcast = await Podcast.GetPodcastAsync(uri);
                subscriptions.AddPodcast(podcast);
            }
            subscriptions.GenerateEpisodeGuids(false);
            return subscriptions;
        }

        /// <summary>
        /// From a given enumeration of podcast Uris, create a subscription class.
        /// </summary>
        /// <param name="PodcastUris">Podcast RSS feed Uris to subscribe to.</param>
        /// <returns>An awaitable subscriptions.</returns>
        public static async Task<Subscriptions> GetSubscriptionsAsync(IEnumerable<string> PodcastUris)
        {
            Subscriptions subscriptions = new Subscriptions();
            foreach (string uri in PodcastUris)
            {
                Podcast podcast = await Podcast.GetPodcastAsync(uri);
            }
            subscriptions.GenerateEpisodeGuids(false);
            return subscriptions;
        }
        #endregion
    }
}
