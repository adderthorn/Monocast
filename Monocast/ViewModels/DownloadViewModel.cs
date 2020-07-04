using System;
using System.Collections.Generic;
using System.ComponentModel;
using Windows.UI.Core;
using Monocast.Controls;

namespace Monocast.ViewModels
{
    public class DownloadViewModel : INotifyPropertyChanged
    {
        private bool isDisposed;
        private CoreDispatcher dispatcher;
        private List<DownloadControl> controls;

        public event PropertyChangedEventHandler PropertyChanged;
        
        public DownloadViewModel(List<DownloadControl> Controls, CoreDispatcher Dispatcher)
        {
            controls = Controls;
            dispatcher = Dispatcher;
        }

        private void RaisePropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void Dispose()
        {
            if (isDisposed) return;
            isDisposed = true;
        }
    }
}
