using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;

namespace Monocast.Controls
{
    public sealed class PodcastMediaTransportControls : MediaTransportControls
    {
        public PodcastMediaTransportControls()
        {
            this.DefaultStyleKey = typeof(PodcastMediaTransportControls);
        }

        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
        }
    }
}
