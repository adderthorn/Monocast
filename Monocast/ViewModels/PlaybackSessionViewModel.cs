using System;
using System.ComponentModel;
using Windows.Media.Playback;
using Windows.UI.Core;

namespace Monocast.ViewModels
{
    public class PlaybackSessionViewModel : INotifyPropertyChanged, IDisposable
    {
        private bool isDisposed;
        private MediaPlayer _MediaPlayer;
        private MediaPlaybackSession _PlaybackSession;
        private CoreDispatcher _dispatcher;

        public MediaPlaybackState PlaybackState => _PlaybackSession.PlaybackState;
        public TimeSpan Position
        {
            get => _PlaybackSession.Position;
            set => _PlaybackSession.Position = value;
        }
        public event PropertyChangedEventHandler PropertyChanged;

        public PlaybackSessionViewModel(MediaPlaybackSession PlaybackSession, CoreDispatcher Dispatcher)
        {
            _MediaPlayer = PlaybackSession.MediaPlayer;
            _PlaybackSession = PlaybackSession;
            _dispatcher = Dispatcher;

            _PlaybackSession.PlaybackStateChanged += PlaybackSession_PlaybackStateChanged;
            _PlaybackSession.SeekCompleted += PlaybackSession_PlaybackStateChanged;
            _MediaPlayer.MediaEnded += MediaPlayer_MediaEnded;
        }

        private async void MediaPlayer_MediaEnded(MediaPlayer sender, object args)
        {
            if (isDisposed) return;
            await _dispatcher.RunAsync(CoreDispatcherPriority.Normal,
                agileCallback: () => { if (!isDisposed) RaisePropertyChanged("MediaEnded"); });
        }

        private async void PlaybackSession_PlaybackStateChanged(MediaPlaybackSession sender, object args)
        {
            if (isDisposed) return;
            await _dispatcher.RunAsync(CoreDispatcherPriority.Normal,
                agileCallback: () => { if (!isDisposed) RaisePropertyChanged("PlaybackState"); });
        }

        public void Dispose()
        {
            if (isDisposed) return;
            _PlaybackSession.PlaybackStateChanged -= PlaybackSession_PlaybackStateChanged;
            _PlaybackSession.SeekCompleted -= PlaybackSession_PlaybackStateChanged;
            _MediaPlayer.MediaEnded -= MediaPlayer_MediaEnded;
            isDisposed = true;
        }

        private void RaisePropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
