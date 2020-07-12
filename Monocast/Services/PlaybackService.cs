using System;
using Windows.Media.Playback;
using Monosoftware.Podcast;
using Windows.UI.Xaml;

namespace Monocast.Services
{
    /// <summary>
    /// The central authority on playback in the application
    /// providing access to the player and active playlist.
    /// </summary>
    public class PlaybackService
    {
        private const float POSITION_UPDATE_TIMEOUT = 10f;

        private static PlaybackService _Instance;
        private DispatcherTimer PositionUpdateTimer;
        
        public static PlaybackService Instance
        {
            get
            {
                if (_Instance == null) _Instance = new PlaybackService();
                return _Instance;
            }
        }

        /// <summary>
        /// This application only requires a single shared MediaPlayer
        /// that all pages have access to. The instance could have 
        /// also been stored in Application.Resources or in an 
        /// application defined data model.
        /// </summary>
        public MediaPlayer MediaPlayer { get; private set; }

        /// <summary>
        /// Active Episode used by the player control.
        /// </summary>
        public Episode NowPlayingEpisode { get; set; }

        public PlaybackService()
        {
            // Create the player instance
            MediaPlayer = new MediaPlayer();
            MediaPlayer.AutoPlay = true;
            MediaPlayer.PlaybackSession.PlaybackStateChanged += async (s, e) =>
            {
                await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                {
                    if (MediaPlayer.PlaybackSession.PlaybackState == MediaPlaybackState.Playing)
                        PositionUpdateTimer.Start();
                    else if (PositionUpdateTimer.IsEnabled)
                        PositionUpdateTimer.Stop();
                });
            };

            PositionUpdateTimer = new DispatcherTimer()
            {
                Interval = TimeSpan.FromSeconds(POSITION_UPDATE_TIMEOUT)
            };
            PositionUpdateTimer.Tick += (s, e) =>
            {
                if (NowPlayingEpisode != null)
                    NowPlayingEpisode.PlaybackPosition = MediaPlayer.PlaybackSession.Position;
            };
        }
    }
}
