using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Runtime.Serialization;
using System.Net;
using Windows.Storage;
using Windows.Storage.AccessCache;
using Monosoftware.Podcast;
using System.Net.NetworkInformation;
using System.Linq;

namespace Monocast
{
    public static class Utilities
    {
        public const string UNKNOWN = "Unknown.";
        public const string SUBSCRIPTION_FILE = "MySubscriptions.xml";
        public const string DURATION_FORMAT = @"hh\:mm\:ss";

        public static async Task SaveSubscriptionsAsync(Subscriptions subscriptions)
        {
            subscriptions.GenerateEpisodeGuids(false);
            subscriptions.LastModifiedDate = DateTime.Now;
            AppData appData = new AppData(Utilities.SUBSCRIPTION_FILE, FolderLocation.Roaming);
            await appData.SerializeToFileAsync<Subscriptions>(subscriptions, CreationCollisionOption.ReplaceExisting);
        }

        public static async Task<Subscriptions> LoadSubscriptions()
        {
            AppData appData = new AppData(Utilities.SUBSCRIPTION_FILE, FolderLocation.Roaming);
            var subscriptions = await appData.DeserializeFromFileAsync<Subscriptions>();
            if (subscriptions.Podcasts.FirstOrDefault()?.Episodes.FirstOrDefault().Podcast == null)
            {
                await Utilities.SaveSubscriptionsAsync(subscriptions);
                subscriptions = await appData.DeserializeFromFileAsync<Subscriptions>();
            }
            return subscriptions;
        }

        public static async Task<bool> RemoveFileFromTokenAsync(string token)
        {
            StorageFile file = await StorageApplicationPermissions.FutureAccessList.GetFileAsync(token);
            if (file == null) return false;
            bool successful = false;
            try
            {
                await file.DeleteAsync(StorageDeleteOption.Default);
                successful = true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
            return successful;
        }

        public static Uri GetBestArtworkUriForEpisode(Episode episode)
        {
            if (episode.HasUniqueArtwork && episode.Artwork != null && NetworkInterface.GetIsNetworkAvailable())
            {
                return episode.Artwork.GetBestArtworkSoruce();
            }
            return episode.Podcast.Artwork.GetBestArtworkSoruce();
        }
    }
}
