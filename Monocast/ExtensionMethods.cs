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
            foreach (var podcast in subscriptions.Podcasts.Where(p => p.Artwork == null))
            {
                podcast.Artwork = new ArtworkInfo();
                podcast.Artwork.SetStream(getDefaultArtworkImageStream());
            }

            var podcastList = new List<Podcast>();
            if (ForceUpdate)
            {
                podcastList = subscriptions.Podcasts.ToList();
            }
            else
            {
                podcastList = subscriptions.Podcasts.Where(p => Math.Abs((DateTime.Now - p.Artwork.LastCacheTime).Days) > 7).ToList();
            }

            foreach (var item in podcastList.Where(p => p.Artwork.MediaSource != null).Select(p => new { Title = p.Title, Artwork = p.Artwork }))
            {
                if (!string.IsNullOrEmpty(item.Artwork.LocalArtworkPath))
                {
                    AppData data = new AppData(item.Artwork.LocalArtworkPath, FolderLocation.Local);
                    if (data.CheckFileExists())
                    {
                        await data.DeleteStorageFileAsync();
                    }
                }
                if (!item.Artwork.IsDownloaded)
                {
                    await item.Artwork.DownloadFileAsync();
                }
                item.Artwork.SaveToFile(item.Title);
            }
        }
        public static async Task GetLocalImagesAsync(this Subscriptions subscriptions)
        {
            var podcastList = subscriptions.Podcasts;
            foreach (var podcast in podcastList.Where(p => p.Artwork != null))
            {
                AppData appData = new AppData(podcast.Artwork.LocalArtworkPath, FolderLocation.Local);
                Stream imgStream;
                if (!appData.CheckFileExists() && podcast.Artwork.MediaSource == null)
                {
                    imgStream = getDefaultArtworkImageStream();
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
                if (imgStream == null)
                {
                    imgStream = getDefaultArtworkImageStream();
                }
                imgStream.Seek(0, SeekOrigin.Begin);
                podcast.Artwork.SetStream(imgStream);
                imgStream.Dispose();
            }
        }

        private static Stream getDefaultArtworkImageStream()
        {
            const string fileName = "Monocast.Resources.placeholder_image.bmp";
            var assembly = typeof(Monocast.Program).GetTypeInfo().Assembly;
            var imgStream = assembly.GetManifestResourceStream(fileName);
            return imgStream;
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
            if (uri == null) return string.Empty;
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
            if (artworkInfo == null) return null;
            Uri uri = artworkInfo.MediaSource;
            if (!string.IsNullOrEmpty(artworkInfo.LocalArtworkPath))
            {
                var appData = new AppData(artworkInfo.LocalArtworkPath, FolderLocation.Local);
                if (appData.CheckFileExists())
                {
                    uri = new Uri(appData.FullPath);
                }
            }
            return uri;
        }

        public async static void SaveToFile(this ArtworkInfo artworkInfo, string FileName)
        {
            if (artworkInfo.MediaBytes == null) return;
            FileName = FileName.ToSafeWindowsNameString() + Path.GetExtension(artworkInfo.MediaSource.GetAbsoluteFileName());
            var appData = new AppData(FileName, FolderLocation.Local);
            _ = await appData.SaveToFileAsync(artworkInfo.MediaBytes, CreationCollisionOption.ReplaceExisting);
            artworkInfo.LocalArtworkPath = FileName;
        }
        #endregion

        #region Collections
        //public static void Sort<T>(this ObservableCollection<T> collection) where T : IComparable<T>, IEquatable<T>
        #endregion
    }
}
