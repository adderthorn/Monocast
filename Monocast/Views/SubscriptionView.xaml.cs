using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using Monosoftware.Podcast;
using Monocast.Controls;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Monocast.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class SubscriptionView : Page, INotifyPropertyChanged
    {
        public Subscriptions Subscriptions => App.Subscriptions;

        public SubscriptionView()
        {
            this.InitializeComponent();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            //var taskList = new List<Task>();
            foreach (Podcast podcast in Subscriptions.Podcasts)
            {
                PodcastControl podcastCtrl = new PodcastControl(podcast);
                podcastCtrl.UnsubscribePodcast += UnsubscribePodcast_Click;
                PodcastsGrid.Items.Add(podcastCtrl);
                //taskList.Add(podcastCtrl.SetArtworkAsync());
            }
            //Task.WhenAll(taskList).ContinueWith((a) =>
            //{
            //    if (a.IsCompleted) UpdateLayout();
            //});
            //Task.WaitAll(taskList.ToArray());
            base.OnNavigatedTo(e);
        }

        public async void GetPodcasts(Task<Podcast> podcastAwaitable)
        {
            Podcast podcast = await podcastAwaitable;
            var pControl = new PodcastControl(podcast)
            {
                Margin = new Thickness(15)
            };
            Subscriptions.AddPodcast(podcast);
            PodcastsGrid.Items.Add(pControl);
        }

        private void PodcastsGrid_ItemClick(object sender, ItemClickEventArgs e)
        {
            if (e.ClickedItem is PodcastControl clickedPodcast)
            {
                this.Frame.Navigate(typeof(PodcastView), clickedPodcast.Podcast,
                    new Windows.UI.Xaml.Media.Animation.DrillInNavigationTransitionInfo());
                RaisePropertyChanged("ActivePage");
            }
        }

        private async void UnsubscribePodcast_Click(object sender, RoutedEventArgs e)
        {
            if (sender is PodcastControl clickedControl)
            {
                PodcastsGrid.Items.Remove(clickedControl);
                Subscriptions.RemovePodcast(clickedControl.Podcast);
                await Utilities.SaveSubscriptions(Subscriptions);
            }
        }

        private void RaisePropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
