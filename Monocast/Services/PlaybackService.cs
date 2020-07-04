using Windows.Media.Playback;
using Windows.Media.Core;

namespace Monocast.Services
{
    /// <summary>
    /// The central authority on playback in the application
    /// providing access to the player and active playlist.
    /// </summary>
    public class PlaybackService
    {
        private static PlaybackService _Instance;
        
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
        /// Source for the media player element to playback from.
        /// </summary>
        public EpisodePlayerInfo EpisodePlayerInfo { get; set; }

        public PlaybackService()
        {
            // Create the player instance
            MediaPlayer = new MediaPlayer();
            MediaPlayer.AutoPlay = true;
        }
    }
}
