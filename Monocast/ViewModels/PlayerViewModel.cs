using System;
using System.ComponentModel;
using Windows.Media.Playback;
using Windows.UI.Core;

namespace Monocast.ViewModels
{
    public class PlayerViewModel : INotifyPropertyChanged, IDisposable
    {
        private bool isDisposed;
        private MediaPlayer mediaPlayer;
        private CoreDispatcher dispatcher;
        private EpisodePlayerInfo playerInfo;
        
        public PlaybackSessionViewModel PlaybackSession { get; set; }
        public event PropertyChangedEventHandler PropertyChanged;
        public EpisodePlayerInfo EpisodePlayerInfo
        {
            get => playerInfo;
            set
            {
                if (playerInfo != value)
                {
                    playerInfo = value;
                    RaisePropertyChanged("EpisodePlayerInfo");
                }
            }
        }
        public TimeSpan TotalDuration
        {
            get => mediaPlayer.PlaybackSession.NaturalDuration;
        }
        
        public PlayerViewModel(MediaPlayer MediaPlayer, CoreDispatcher Dispatcher)
        {
            mediaPlayer = MediaPlayer;
            dispatcher = Dispatcher;
            PlaybackSession = new PlaybackSessionViewModel(mediaPlayer.PlaybackSession, dispatcher);
        }

        public void SetNewEpisodePlayerInfo(EpisodePlayerInfo info)
        {
            EpisodePlayerInfo = info;
            mediaPlayer.Source = info.PlaybackSource;
            mediaPlayer.PlaybackSession.Position = info.PlaybackPosition;
        }

        public void TogglePlayPause()
        {
            switch (mediaPlayer.PlaybackSession.PlaybackState)
            {
                case MediaPlaybackState.Playing:
                    mediaPlayer.Pause();
                    break;
                case MediaPlaybackState.Paused:
                    mediaPlayer.Play();
                    break;
            }
        }

        public void JumpPlaybackBySeconds(double Seconds)
        {
            TimeSpan newPosition = TimeSpan.FromSeconds(Seconds) + mediaPlayer.PlaybackSession.Position;
            if (newPosition < TimeSpan.Zero)
            {
                newPosition = TimeSpan.Zero;
            }
            else if (newPosition > mediaPlayer.PlaybackSession.NaturalDuration)
            {
                newPosition = mediaPlayer.PlaybackSession.NaturalDuration;
            }
            mediaPlayer.PlaybackSession.Position = newPosition;
        }

        private void RaisePropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void Dispose()
        {
            if (isDisposed) return;
            PlaybackSession.Dispose();
            isDisposed = true;
        }
    }
}
