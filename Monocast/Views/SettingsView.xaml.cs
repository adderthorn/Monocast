using System;
using System.ComponentModel;
using System.Collections.Generic;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using Monosoftware.Podcast;
using System.IO;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Monocast.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class SettingsView : Page
    {
        public bool HasSubscriptions
        {
            get => App.Subscriptions.Podcasts.Count > 0;
        }
        public Settings Settings => App.Settings;
        public SettingsView()
        {
            this.InitializeComponent();
        }

        private async void ExportOpmlButton_Click(object sender, RoutedEventArgs e)
        {
            FileSavePicker picker = new FileSavePicker()
            {
                SuggestedStartLocation = PickerLocationId.Desktop,
                SuggestedFileName = "subscriptions"
            };
            picker.FileTypeChoices.Add("OPML Files", new string[] { ".opml", ".xml" });
            StorageFile opmlFile = await picker.PickSaveFileAsync();
            if (opmlFile != null)
            {
                //FileIO.WriteTextAsync(opmlFile, App.Subscriptions.CreateOpmlFromSubscriptions("Monocast Subscriptions"))
                MemoryStream opmlStream = App.Subscriptions.CreateOpmlFromSubscriptions("Monocast Subscriptions") as MemoryStream;
                using (var stream = await opmlFile.OpenStreamForWriteAsync())
                {
                    opmlStream.Seek(0, SeekOrigin.Begin);
                    opmlStream.WriteTo(stream);
                }
                await CachedFileManager.CompleteUpdatesAsync(opmlFile);
            }
        }
    }
}
