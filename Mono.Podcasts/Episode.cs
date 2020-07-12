using System;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.Serialization;
using System.IO;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices.ComTypes;

namespace Monosoftware.Podcast
{
    /// <summary>
    /// Represents a single episode of a podcast
    /// </summary>
    [DataContract(IsReference = true)]
    public class Episode : IDisposable, IDownloadable, INotifyPropertyChanged
    {
        #region Private Variables
        private HttpClientProgress httpClient;
        private Stream _MediaStream;
        private bool isDisposed = false;
        private string _Title;
        private bool _IsArchived;
        private bool _IsPinned;
        private string _Description;
        private ArtworkInfo _Artwork;
        private DateTimeOffset _PublishDate;
        private Uri _MediaSource;
        private string _LocalFilePath;
        private string _LocalFileToken;
        private bool _Downloaded;
        private bool _PendingDownload;
        private TimeSpan _Duration;
        private TimeSpan _PlaybackPosition;
        private string _GUID;
        #endregion

        #region Event Handlers
        public event PropertyChangedEventHandler PropertyChanged;

        private void raiseNotifyPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        private void raiseNotifyPropertyChanged(params string[] propertyNames)
        {
            for (int i = 0; i < propertyNames.Length; i++)
            {
                raiseNotifyPropertyChanged(propertyNames[i]);
            }
        }
        #endregion

        #region Public Properties
        /// <summary>
        /// The title of the podcast the episode belongs to; used to
        /// relate the episode to the parent podcast.
        /// </summary>
        //[DataMember]
        //public string PodcastTitle { get; set; }
        [DataMember]
        public Podcast Podcast { get; private set; }
        /// <summary>
        /// The title of the episode.
        /// </summary>
        [DataMember]
        public string Title
        { 
            get => _Title;
            set
            {
                string newTitle = value.Trim();
                if (newTitle != _Title)
                {
                    _Title = newTitle;
                    raiseNotifyPropertyChanged(nameof(Title));
                }
            }
        }
        /// <summary>
        /// The podcast description; this is usually HTML formatted text.
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
                    raiseNotifyPropertyChanged(nameof(Description));
                }
            }
        }
        /// <summary>
        /// A boolean that represents whether the episode has its own artwork.
        /// </summary>
        public bool HasUniqueArtwork => Artwork != null;
        /// <summary>
        /// The epsiode's artwork; if this is not unique, it should return the
        /// podcast's artwork.
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
                    raiseNotifyPropertyChanged(nameof(Artwork));
                }
            }
        }
        /// <summary>
        /// The published date of the episode.
        /// </summary>
        [DataMember]
        public DateTimeOffset PublishDate
        {
            get => _PublishDate;
            set
            {
                if (value != _PublishDate)
                {
                    _PublishDate = value;
                    raiseNotifyPropertyChanged(nameof(PublishDate));
                }
            }
        }
        /// <summary>
        /// The URI that represents the MP3 or audio file for the episode.
        /// </summary>
        [DataMember]
        public Uri MediaSource
        {
            get => _MediaSource;
            set
            {
                if (value != _MediaSource)
                {
                    _MediaSource = value;
                    raiseNotifyPropertyChanged(nameof(MediaSource));
                }
            }
        }
        /// <summary>
        /// Represents the system path to the locally downloaded episode file.
        /// </summary>
        [DataMember]
        public string LocalFilePath
        {
            get => _LocalFilePath;
            set
            {
                if (value != _LocalFilePath)
                {
                    _LocalFilePath = value;
                    raiseNotifyPropertyChanged(nameof(LocalFilePath));
                }
            }
        }
        /// <summary>
        /// Represents a unique token to the local file; this can be used in platforms
        /// like Microsoft's UWP where access control comes from the API rather than
        /// direct filesystem access.
        /// </summary>
        [DataMember]
        public string LocalFileToken
        {
            get => _LocalFileToken;
            set
            {
                if (value != _LocalFileToken)
                {
                    _LocalFileToken = value;
                    raiseNotifyPropertyChanged(nameof(LocalFileToken));
                }
            }
        }
        /// <summary>
        /// Represents if the episode has been downloaded locally.
        /// </summary>
        [DataMember]
        public bool Downloaded
        {
            get => _Downloaded;
            set
            {
                if (value != _Downloaded)
                {
                    _Downloaded = value;
                    raiseNotifyPropertyChanged(nameof(Downloaded));
                }
            }
        }
        /// <summary>
        /// Flag that can be used to queue up a group of episodes to be downloaded.
        /// </summary>
        public bool PendingDownload
        {
            get => _PendingDownload;
            set
            {
                if (value != _PendingDownload)
                {
                    _PendingDownload = value;
                    raiseNotifyPropertyChanged(nameof(PendingDownload));
                }
            }
        }
        /// <summary>
        /// The total duration of the episode; this is set from the feed, not the MP3.
        /// </summary>
        public TimeSpan Duration
        {
            get => _Duration;
            set
            {
                if (value != _Duration)
                {
                    _Duration = value;
                    raiseNotifyPropertyChanged(nameof(Duration), nameof(DurationLong));
                }
            }
        }
        /// <summary>
        /// The current playback position of the episode, so users can resume from a
        /// certain point in the episode.
        /// </summary>
        public TimeSpan PlaybackPosition
        {
            get => _PlaybackPosition;
            set
            {
                if (value != _PlaybackPosition)
                {
                    _PlaybackPosition = value;
                    raiseNotifyPropertyChanged(nameof(PlaybackPosition),
                        nameof(PlaybackPositionLong),
                        nameof(IsPlayed));
                }
            }
        }
        /// <summary>
        /// long integer of playback position to be stored in the XML.
        /// </summary>
        [DataMember]
        public long PlaybackPositionLong
        {
            get => PlaybackPosition.Ticks;
            set
            {
                if (value != _PlaybackPosition.Ticks)
                {
                    _PlaybackPosition = new TimeSpan(value);
                    raiseNotifyPropertyChanged(nameof(PlaybackPosition), nameof(PlaybackPositionLong));
                }
            }
        }
        /// <summary>
        /// Long integer of duration to be stored in the XML.
        /// </summary>
        [DataMember]
        public long DurationLong
        {
            get => Duration.Ticks;
            set
            {
                if (value != _Duration.Ticks)
                {
                    _Duration = new TimeSpan(value);
                    raiseNotifyPropertyChanged(nameof(Duration), nameof(DurationLong));
                }
            }
        }
        /// <summary>
        /// A unique identifier that represents the episode, can be used to find an exact episode where
        /// a title may be used twice (not globally unique).
        /// </summary>
        [DataMember]
        public string GUID
        {
            get => _GUID;
            set
            {
                if (value != _GUID)
                {
                    _GUID = value;
                    raiseNotifyPropertyChanged(nameof(GUID));
                }
            }
        }
        /// <summary>
        /// An identifier used to "pin" or "star" an episode perahps to save it for future use.
        /// Pinned and Archived are mutually exclusive.
        /// </summary>
        [DataMember]
        public bool IsPinned
        {
            get => _IsPinned;
            set
            {
                if (value != _IsPinned)
                {
                    _IsPinned = value;
                    raiseNotifyPropertyChanged(nameof(IsPinned));
                    if (value)
                    {
                        _IsArchived = false;
                        raiseNotifyPropertyChanged(nameof(IsArchived));
                    }
                }
            }
        }
        /// <summary>
        /// Flag used to show if the episode is archived so it can be hidden from the UI.
        /// Pinned and Archived are mutually exclusive.
        /// </summary>
        [DataMember]
        public bool IsArchived
        {
            get => _IsArchived;
            set
            {
                if (value != _IsArchived)
                {
                    _IsArchived = value;
                    raiseNotifyPropertyChanged(nameof(IsArchived));
                    if (value)
                    {
                        _IsPinned = false;
                        raiseNotifyPropertyChanged(nameof(IsPinned));
                    }
                }
            }
        }
        /// <summary>
        /// Gets or sets if the episode has been played to completion; setting this will cause the playpack
        /// position to be equal to the total duration of the episode.
        /// </summary>
        public bool IsPlayed
        {
            get
            {
                // Needed to check to episodes where we do not have a valid duration.
                //if (Duration == PlaybackPosition && PlaybackPosition > TimeSpan.MinValue) return true;
                //return false;
                return (PlaybackPosition > TimeSpan.Zero && Duration > TimeSpan.MinValue);
            }
            set
            {
                if (value != IsPlayed)
                {
                    if (value)
                    {
                        PlaybackPosition = Duration;
                    }
                    else
                    {
                        if (PlaybackPosition == Duration)
                        {
                            PlaybackPosition = TimeSpan.Zero;
                        }
                    }
                    raiseNotifyPropertyChanged(nameof(IsPlayed));
                }
            }
        }
        #endregion

        #region Constructors
        public Episode(Podcast PodcastInstance)
        {
            Podcast = PodcastInstance;
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Downloads the episode file to the object's internal media stream, the downloaded file can be retreived using the GetStream() method.
        /// </summary>
        /// <param name="progressCallback">Optional callback function to be used for progress.</param>
        /// <param name="cancellationToken">Optional cancellation token to cancel a download-in-progress.</param>
        /// <returns>An awaitable task.</returns>
        public async Task DownloadFileAsync(IProgress<HttpProgressInfo> progressCallback = null, CancellationTokenSource cancellationToken = null)
        {
            if (_MediaStream == null)
            {
                if (httpClient == null)
                    httpClient = new HttpClientProgress(MediaSource, cancellationToken);
                httpClient.ProgressChanged += (totalFileSize, bytesDownloaded, percentage) =>
                {
                    var info = new HttpProgressInfo()
                    {
                        TotalBytesToReceive = totalFileSize,
                        BytesReceived = bytesDownloaded,
                        ProgressPercentage = percentage
                    };
                    progressCallback?.Report(info);
                };
                _MediaStream = await httpClient.StartDownloadAsync();
            }
        }

        /// <summary>
        /// Gets a new stream object from the downloaded file; note that the file must be downloaded first,
        /// you can check IsDownloaded before using this method to prevent an ObjectDisposedException.
        /// </summary>
        /// <returns>Stream object of the episode.</returns>
        public Stream GetStream()
        {
            if (_MediaStream == null || isDisposed)
                throw new ObjectDisposedException("MediaStream", "The object is currently NULL or disposed.Check IsDownloaded before accessing the stream.");
            _MediaStream.Seek(0, SeekOrigin.Begin);
            return _MediaStream;
        }

        /// <summary>
        /// Sets the episode stream to a stream object.
        /// </summary>
        /// <param name="stream">Stream object of the episode.</param>
        public void SetStream(Stream stream)
        {
            if (!stream.CanRead || !stream.CanSeek || isDisposed)
                throw new ObjectDisposedException(nameof(stream), "Object is disposed and cannot be read!");
            stream.Seek(0, SeekOrigin.Begin);
            _MediaStream = stream;
        }

        /// <summary>
        /// Disposes of the episode and its stream.
        /// </summary>
        public void Dispose()
        {
            Artwork.Dispose();
            _MediaStream?.Dispose();
            httpClient?.Dispose();
            isDisposed = true;
        }

        /// <summary>
        /// Generates a new GUID if none exists and sets the GUID property of the episode.
        /// </summary>
        /// <returns>True if a new GUID was generated or false if no GUID is needed</returns>
        public bool GenerateGUID()
        {
            if (string.IsNullOrWhiteSpace(GUID))
            {
                GUID = Guid.NewGuid().ToString();
                return true;
            }
            return false;
        }

        /// <summary>
        /// Returns a value to show if the episode has been downloaded.
        /// </summary>
        /// <returns>True/False status of downloaded.</returns>
        public bool IsDownloaded { get => Downloaded; }
        #endregion
    }
}
