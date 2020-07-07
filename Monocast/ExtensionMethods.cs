using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Windows.ApplicationModel.Resources;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.Storage.AccessCache;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;
using Monosoftware.Podcast;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Monocast
{
    public static class ExtensionMethods
    {
        #region Constants
        private const string THEME = "PodcastHtml";
        private const string PLACEHOLDER = @"<!--PLACEHOLDER //-->";
        private const string JPG = "jpg";
        #endregion

        #region Web View
        public static void NavigateToThemedString(this WebView view, string InputHtml)
        {
            var loader = new ResourceLoader();
            string podcastHtmlTheme = THEME + Application.Current.RequestedTheme.ToString();
            string descriptionhtml = loader.GetString(podcastHtmlTheme);
            if (descriptionhtml == string.Empty) descriptionhtml = loader.GetString(THEME);
            descriptionhtml = descriptionhtml.Replace(PLACEHOLDER, InputHtml);
            view.NavigateToString(descriptionhtml);
        }
        #endregion

        #region Subscriptions
        public static async Task RefreshPodcastArtworkAsync(this Subscriptions subscriptions, bool ForceUpdate = false)
        {
            var podcastList = new List<Podcast>();
            if (ForceUpdate)
            {
                podcastList = subscriptions.Podcasts.ToList();
            }
            else
            {
                podcastList = subscriptions.Podcasts.Where(p => Math.Abs((DateTime.Now - p.Artwork.LastCacheTime).Days) > 7).ToList();
            }
            foreach (var art in podcastList.Select(p => p.Artwork))
            {
                string fileName = !string.IsNullOrWhiteSpace(art.LocalArtworkPath) ? new AppData(art.LocalArtworkPath, FolderLocation.Local).FullPath : null;
                if (!art.IsDownloaded)
                {
                    await art.DownloadFileAsync();
                }
                art.SaveToFile(fileName);
            }
        }
        public static async Task GetLocalImagesAsync(this Subscriptions subscriptions)
        {
            var podcastList = subscriptions.Podcasts.Where(p => p.Artwork.IsDownloaded);
            foreach (var podcast in podcastList)
            {
                AppData appData = new AppData(podcast.Artwork.LocalArtworkPath, FolderLocation.Local);
                Stream imgStream;
                if (!appData.CheckFileExists() && podcast.Artwork.MediaSource == null)
                {
                    var assembly = typeof(Monocast.Program).GetTypeInfo().Assembly;
                    const string fileName = "Monocast.Resources.placeholder_image.bmp";
                    imgStream = assembly.GetManifestResourceStream(fileName);
                    //imgStream = new BitmapImage(new Uri("ms-appx:///Resources/placeholder_image.bmp"));
                    //imgStream = new FileStream("ms-appx:///Resources/placeholder_image.bmp", FileMode.Open);
                }
                else if (!appData.CheckFileExists() && podcast.Artwork.MediaSource != null)
                {
                    await podcast.Artwork.DownloadFileAsync();
                    imgStream = podcast.Artwork.GetStream();
                }
                else
                {
                    imgStream = await appData.LoadFromFileAsync();
                }
                imgStream.Seek(0, SeekOrigin.Begin);
                podcast.Artwork.SetStream(imgStream);
                imgStream.Dispose();
            }
        }
        #endregion

        #region String
        public static string ToSafeWindowsNameString(this string str)
        {
            //TODO: I should figure out how this works
            return Path.GetInvalidFileNameChars().Aggregate(str, (current, c) => current.Replace(c.ToString(), string.Empty));
        }

        public static string GetAbsoluteFileName(this Uri uri)
        {
            string uriPath = uri.AbsolutePath;
            if (!Uri.TryCreate(uriPath, UriKind.Absolute, out Uri newUri))
                newUri = new Uri(uri, uriPath);
            if (newUri == null) return string.Empty;

            return Path.GetFileName(newUri.LocalPath);
        }
        #endregion

        #region StorageFile
        public static async Task<StorageFile> GetLocalEpisodeStorageFileAsync(this Episode episode)
        {
            var storageFile = await StorageApplicationPermissions.FutureAccessList.GetFileAsync(episode.LocalFileToken);
            return storageFile;
        }
        #endregion

        #region BitmapImage
        public static async Task<BitmapImage> GetBitmapImageFromBytesAsync(this byte[] array)
        {
            if (array.Length < 1) return null;
            BitmapImage image = new BitmapImage();
            using (InMemoryRandomAccessStream imageStream = new InMemoryRandomAccessStream())
            {
                await imageStream.WriteAsync(array.AsBuffer());
                imageStream.Seek(0);
                await image.SetSourceAsync(imageStream);
            }
            return image;
        }
        #endregion

        #region ArtworkInfo
        public static Uri GetBestArtworkSoruce(this ArtworkInfo artworkInfo)
        {
            Uri uri = artworkInfo.MediaSource;
            if (!string.IsNullOrWhiteSpace(artworkInfo.LocalArtworkPath))
            {
                var appData = new AppData(artworkInfo.LocalArtworkPath, FolderLocation.Local);
                return new Uri(appData.FullPath);
            }
            return uri;
        }

        public async static void SaveToFile(this ArtworkInfo artworkInfo, string FileName = null)
        {
            if (string.IsNullOrWhiteSpace(FileName))
                FileName = Guid.NewGuid().ToString().ToLower();
            FileName = FileName.ToSafeWindowsNameString() + Path.GetExtension(artworkInfo.MediaSource.GetAbsoluteFileName());
            var appData = new AppData(FileName, FolderLocation.Local);
            var filePath = await appData.SaveToFileAsync(artworkInfo.MediaBytes, CreationCollisionOption.ReplaceExisting);
            artworkInfo.LocalArtworkPath = FileName;
        }
        #endregion

        #region Collections
        //public static void Sort<T>(this ObservableCollection<T> collection) where T : IComparable<T>, IEquatable<T>
        #endregion
    }
}
