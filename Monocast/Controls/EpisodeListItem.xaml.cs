using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Monosoftware.Podcast;
using System.ComponentModel;
using Windows.UI.Xaml.Media.Animation;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Monocast.Controls
{
    public sealed partial class EpisodeListItem : UserControl, INotifyPropertyChanged
    {
        private const int MAX_TITLE_LENGTH = 50;

        private bool _CanSelect = false;
        private bool _Selected = false;
        private Visibility _CheckboxVisibility = Visibility.Collapsed;

        public Episode Episode { get; set; }
        public bool CanSelect
        {
            get => _CanSelect;
            set
            {
                if (value != _CanSelect)
                {
                    _CanSelect = value;
                    CheckboxVisibility = value ? Visibility.Visible : Visibility.Collapsed;
                    RaisePropertyChanged(
                        nameof(CanSelect),
                        nameof(PinnedVisiblity),
                        nameof(UnreadVisibility),
                        nameof(ProgressBarVisibility));
                    int from = value ? -40 : 0;
                    int to = value ? 0 : -40;
                    EaseIn.Children[0].SetValue(DoubleAnimation.FromProperty, from);
                    EaseIn.Children[0].SetValue(DoubleAnimation.ToProperty, to);
                    EaseIn.Begin();
                }
            }
        }
        public bool Selected
        {
            get => _Selected;
            set
            {
                if (value != _Selected)
                {
                    _Selected = value;
                    RaisePropertyChanged(nameof(Selected));
                    EpisodeChecked?.Invoke(this, new RoutedEventArgs());
                }
            }
        }

        private Visibility CheckboxVisibility
        {
            get => _CheckboxVisibility;
            set
            {
                if (value != _CheckboxVisibility)
                {
                    _CheckboxVisibility = Visibility;
                    RaisePropertyChanged(nameof(CheckboxVisibility));
                }
            }
        }
        private Visibility PinnedVisiblity =>
            (Episode.IsPinned && !CanSelect) ? Visibility.Visible : Visibility.Collapsed;
        private Visibility ProgressBarVisibility =>
            (Episode.PlaybackPositionLong > 0
            && Episode.PlaybackPositionLong != Episode.DurationLong
            && !Episode.IsPlayed
            && !CanSelect) ? Visibility.Visible : Visibility.Collapsed;

        private Visibility UnreadVisibility =>
            (Episode.PlaybackPositionLong == 0
            && !Episode.IsPlayed
            && !Episode.IsPinned
            && !Episode.IsArchived
            && !CanSelect) ? Visibility.Visible : Visibility.Collapsed;

        private SolidColorBrush TextColor =>
            (Episode.IsArchived ? Application.Current.Resources["ListBoxItemDisabledForegroundThemeBrush"]
            : Application.Current.Resources["DefaultTextForegroundThemeBrush"]) as SolidColorBrush;

        private bool IsNotCompletelyPlayed => this.CompletedPct < 100;

        public double CompletedPct
        {
            get
            {
                if (Episode.PlaybackPositionLong <= 0 || Episode.DurationLong <= 0) return 0;
                return (double)Episode.PlaybackPositionLong / (double)Episode.DurationLong * 100;
            }
        }

        public string TruncatedEpisodeTitle =>
            Episode.Title.Substring(0, Episode.Title.Length < MAX_TITLE_LENGTH ? Episode.Title.Length : MAX_TITLE_LENGTH);

        private string MarkArchivedText => Episode.IsArchived ? "Unarchive" : "Mark Archived";

        public RoutedEventHandler DownloadEpisode;
        public RoutedEventHandler MarkEpisodeAsPlayed;
        public RoutedEventHandler GoToEpisodeDetails;
        public RoutedEventHandler EpisodeChecked;
        public RoutedEventHandler ToggleMarkArchived;
       
        public RoutedEventHandler PinEpisode;

        public event PropertyChangedEventHandler PropertyChanged;

        public EpisodeListItem(Episode episode)
        {
            InitializeComponent();
            Episode = episode;
        }

        public void EpisodeUpdated()
        {
            RaisePropertyChanged(
                nameof(CheckboxVisibility),
                nameof(PinnedVisiblity),
                nameof(ProgressBarVisibility),
                nameof(IsNotCompletelyPlayed),
                nameof(CompletedPct),
                nameof(TruncatedEpisodeTitle),
                nameof(UnreadVisibility),
                nameof(TextColor));
        }

        private void RaisePropertyChanged(string name) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        private void RaisePropertyChanged(params string[] names)
        {
            for (int i = 0; i < names.Length; i++)
                RaisePropertyChanged(names[i]);
        }

        private void DownloadMenuFlyoutItem_Click(object sender, RoutedEventArgs e) =>
            this.DownloadEpisode?.Invoke(this, e);

        private void MarkPlayedMenuFlyoutItem_Click(object sender, RoutedEventArgs e) =>
            this.MarkEpisodeAsPlayed?.Invoke(this, e);

        private void GoToMenuFlyoutItem_Click(object sender, RoutedEventArgs e) =>
            this.GoToEpisodeDetails?.Invoke(this, e);

        private void MarkArchivedMenuFlyoutItem_Click(object sender, RoutedEventArgs e) =>
            this.ToggleMarkArchived?.Invoke(this, e);

        private void PinEpisodeMenuFlyoutItem_Click(object sender, RoutedEventArgs e) =>
            this.PinEpisode?.Invoke(this, e);

        private void EaseIn_Completed(object sender, object e) =>
            CheckboxVisibility = CanSelect ? Visibility.Visible : Visibility.Collapsed;
    }
}
