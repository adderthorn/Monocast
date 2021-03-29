using System;
using System.IO;
using System.Collections.Generic;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Monosoftware.Podcast;
using Windows.Storage;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Threading;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Core;
using System.Net.NetworkInformation;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Monocast.Controls
{
    public enum DownloadState
    {
        None,
        NotStarted,
        Started,
        Finished
    }

    public sealed partial class DownloadControl : UserControl, IDisposable, INotifyPropertyChanged
    {
        private const string DOWNLOADED_TEXT = "{0:f2} %";
        private CancellationTokenSource cancellationToken;
        private Uri _Artwork;
        private bool _SaveToken = true;

        public Episode Episode { get; set; }
        public Settings Settings { get; set; }
        public DownloadState State { get; set; }
        public bool SaveToken
        {
            get => _SaveToken;
            set => _SaveToken = value;
        }
        public List<DownloadControl> CurrentDownloads { get; private set; }
        public StorageFile DownloadFileLocation { get; private set; }
        public Uri Artwork
        {
            get => _Artwork;
            set
            {
                if (_Artwork != value)
                {
                    _Artwork = value;
                    RaisePropertyChanged(nameof(Artwork));
                }
            }
        }

        public EventHandler DownloadFinished;
        public EventHandler DownloadCancelled;

        public event PropertyChangedEventHandler PropertyChanged;

        public DownloadControl(List<DownloadControl> CurrentDownloads, Episode Episode, StorageFile DownloadFileLocation, Uri ArtworkUri)
        {
            this.DownloadFileLocation = DownloadFileLocation;
            this.Episode = Episode;
            this.CurrentDownloads = CurrentDownloads;
            cancellationToken = new CancellationTokenSource();
            if (!CurrentDownloads.Contains(this)) CurrentDownloads.Add(this);
            State = DownloadState.None;
            this.Artwork = ArtworkUri;
            InitializeComponent();
        }

        public async void StartDownload()
        {
            try
            {
                State = DownloadState.Started;
                var progressCallback = new Progress<HttpProgressInfo>(progress =>
                {
                    if (progress.TotalBytesToReceive == null) return;
                    double current = (double)progress.BytesReceived;
                    double total = (double)progress.TotalBytesToReceive;
                    double percentage = current / total * 100;
                    DownloadPercent.Text = string.Format(DOWNLOADED_TEXT, percentage);
                    DownloadProgressBar.Value = percentage;
                    if (percentage >= 100D)
                    {
                        DownloadProgressBar.IsIndeterminate = true;
                        DownloadPercent.Text = "Writing File...";
                    }
                });

                menuFlyoutItemCancel.IsEnabled = true;
                var episodeStreamTask = Episode.DownloadFileAsync(progressCallback, cancellationToken);
                string fileName = Episode.Title.ToSafeWindowsNameString()
                    + "."
                    + Path.GetExtension(Episode.MediaSource.GetAbsoluteFileName()).Trim('.');
                DownloadFileLocation = DownloadFileLocation;
                using (var outputStream = await DownloadFileLocation.OpenStreamForWriteAsync())
                {
                    await episodeStreamTask;
                    if (cancellationToken.IsCancellationRequested)
                    {
                        throw new TaskCanceledException();
                    }
                    if (SaveToken)
                    {
                        Episode.LocalFileToken = Windows.Storage.AccessCache.StorageApplicationPermissions.FutureAccessList.Add(DownloadFileLocation);
                        Episode.LocalFilePath = DownloadFileLocation.Path ?? string.Empty;
                    }
                    await Episode.GetStream().CopyToAsync(outputStream);
                }
                DownloadPercent.Text = "Done!";
                DownloadFinished?.Invoke(this, new EventArgs());
            }
            catch (TaskCanceledException)
            {
                DownloadPercent.Text = "Cancelled";
                DownloadProgressBar.Value = 0;
                if (DownloadFileLocation != null) await DownloadFileLocation.DeleteAsync();
                DownloadCancelled?.Invoke(this, new EventArgs());
            }
            finally
            {
                DownloadProgressBar.IsIndeterminate = false;
                menuFlyoutItemCancel.IsEnabled = false;
                State = DownloadState.Finished;
            }
        }

        private void menuFlyoutItemCancel_Click(object sender, RoutedEventArgs e)
        {
            cancellationToken.Cancel();
        }

        public void Dispose()
        {
            if (cancellationToken != null)
            {
                cancellationToken.Dispose();
                cancellationToken = null;
            }
            if (CurrentDownloads.Contains(this)) CurrentDownloads.Remove(this);
        }

        private void RaisePropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        private void SymbolIcon_PointerEntered(object sender, Windows.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            Window.Current.CoreWindow.PointerCursor = new CoreCursor(CoreCursorType.Hand, 0);
        }

        private void SymbolIcon_PointerExited(object sender, Windows.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            Window.Current.CoreWindow.PointerCursor = new CoreCursor(CoreCursorType.Arrow, 0);
        }
    }
}
