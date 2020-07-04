using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Monosoftware.Podcast;
using System.Diagnostics;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Monocast.Controls
{
    public sealed partial class EditPodcastControl : UserControl
    {
        public Podcast Podcast { get; set; }

        public EditPodcastControl(Podcast podcast)
        {
            this.InitializeComponent();
            this.Podcast = podcast;
        }

        private void RemoveIcon_Tapped(object sender, TappedRoutedEventArgs e)
        {
            Debug.WriteLine("sup?");
        }
    }
}
