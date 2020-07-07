using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using Monosoftware.Podcast;
using Monocast.Controls;
using System.Linq;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Monocast.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class DownloadView : Page, INotifyPropertyChanged
    {
        //private bool _CameFromControlList;

        public event PropertyChangedEventHandler PropertyChanged;

        //public IEnumerable<DownloadControl> DownloadableEpisodes { get; set; }
        public Subscriptions Subscriptions => App.Subscriptions;
        public Settings Settings => App.Settings;

        public DownloadView()
        {
            this.InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            //if (e.Parameter is IEnumerable<DownloadControl>)
            //{
            //    DownloadableEpisodes = (IEnumerable<DownloadControl>)e.Parameter;
            //    _CameFromControlList = true;
            //}
            base.OnNavigatedTo(e);
            DownloadEpisodes();
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);
            MainStackPanel.Children.Clear();
        }

        private async void DownloadComplete(object sender, EventArgs e)
        {
            DownloadControl control = null;
            if (sender is DownloadControl)
            {
                control = sender as DownloadControl;
            }
            if (control == null) return;
            control.Episode.Downloaded = true;
            if (/*_CameFromControlList
                &&*/ !string.IsNullOrEmpty(control.Episode.GUID))
            {
                var episode = Subscriptions.GetEpisodeFromGuid(control.Episode.GUID);
                if (episode != null)
                {
                    episode.Downloaded = true;
                    episode.LocalFilePath = control.Episode.LocalFilePath;
                    episode.LocalFileToken = control.Episode.LocalFileToken;
                }
            }
            await Task.Delay(1000);
            await RemoveDownloadControlAsync(control);
            control.Dispose();
        }

        private async void DownloadCancelled(object sender, EventArgs e)
        {
            DownloadControl control = null;
            if (sender is DownloadControl)
            {
                control = sender as DownloadControl;
            }
            if (control == null) return;
            await RemoveDownloadControlAsync(control);
            control.Dispose();
        }

        private async Task RemoveDownloadControlAsync(DownloadControl control)
        {
            await Task.Delay(1000);
            MainStackPanel.Children.Remove(control);
            control.Dispose();
            if (MainStackPanel.Children.Count == 0)
            {
                FinishAllDownloads();
            }
        }

        private async void FinishAllDownloads()
        {
            if (App.CurrentDownloads.Count > 0) return;
            await Utilities.SaveSubscriptionsAsync(Subscriptions);
            //Frame.Navigate(typeof(SubscriptionView));
            Frame.GoBack();
            RaisePropertyChanged("ActivePage");
        }

        private void DownloadEpisodes()
        {
            // TODO: See if this is still needed...
            //List<DownloadControl> episodeControlsToDownload;
            //if (DownloadableEpisodes != null)
            //{
            //    episodeControlsToDownload = DownloadableEpisodes.ToList();
            //}
            //else
            //{
            //    var episodesToDownload = Subscriptions.GetPendingDownloadEpisodes().ToList();
            //    if (episodesToDownload.Count() < 1) return;
            //    //var control = new DownloadControl(episode);
            //    episodeControlsToDownload = new List<DownloadControl>();
            //    episodesToDownload.ForEach(e => episodeControlsToDownload.Add(new DownloadControl(e)));
            //}
            foreach (DownloadControl episodeControl in App.CurrentDownloads)
            {
                if (episodeControl.State == DownloadState.None)
                {
                    episodeControl.DownloadFinished += DownloadComplete;
                    episodeControl.DownloadCancelled += DownloadCancelled;
                    episodeControl.Episode.PendingDownload = false;
                    episodeControl.StartDownload();
                }
                MainStackPanel.Children.Add(episodeControl);
            }
        }

        private void RaisePropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
