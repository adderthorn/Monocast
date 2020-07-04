using System;
using System.Threading.Tasks;
using Windows.Media.Playback;
using Windows.Media.Core;
using Windows.Storage;
using Windows.UI.Xaml.Media.Imaging;
using Monosoftware.Podcast;
using System.Net.NetworkInformation;

namespace Monocast
{
    public class EpisodePlayerInfo
    {
        public Episode Episode { get; set; }

        public IMediaPlaybackSource PlaybackSource { get; set; }
        public BitmapImage ArtworkPoster { get; private set; }
        public TimeSpan PlaybackPosition => Episode.PlaybackPosition;

        public EpisodePlayerInfo(Episode Episode) => this.Episode = Episode;

        public static async Task<EpisodePlayerInfo> CreateFromEpisodeAsync(Episode Episode)
        {
            var episodePlayerInfo = new EpisodePlayerInfo(Episode);
            //await App.Subscriptions.GetLocalImagesAsync();
            if (episodePlayerInfo.Episode.Downloaded)
            {
                try
                {
                    StorageFile storageFile = await episodePlayerInfo.Episode.GetLocalEpisodeStorageFileAsync();
                    episodePlayerInfo.PlaybackSource = MediaSource.CreateFromStorageFile(storageFile);
                }
                catch
                {
                    SetEpisodeFromUri(episodePlayerInfo);
                    return episodePlayerInfo;
                }
                if (Episode.Artwork?.IsDownloaded == true)
                    episodePlayerInfo.ArtworkPoster = await Episode.Artwork.MediaBytes.GetBitmapImageFromBytesAsync();
                else
                {
                    if (Episode.Podcast.Artwork?.IsDownloaded == true)
                    {
                        episodePlayerInfo.ArtworkPoster = await Episode.Podcast.Artwork.MediaBytes.GetBitmapImageFromBytesAsync();
                    }
                }
            }
            else
            {
                SetEpisodeFromUri(episodePlayerInfo);
            }
            return episodePlayerInfo;
        }

        private static void SetEpisodeFromUri(EpisodePlayerInfo episodePlayerInfo)
        {
            episodePlayerInfo.PlaybackSource = MediaSource.CreateFromUri(episodePlayerInfo.Episode.MediaSource);
            BitmapImage img = new BitmapImage();
            img.UriSource = episodePlayerInfo.Episode.Artwork?.MediaSource;
            episodePlayerInfo.ArtworkPoster = img;
        }
    }
}
