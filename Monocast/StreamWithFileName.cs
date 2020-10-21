using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage.Streams;

namespace Monocast
{
    struct StreamWithFileName
    {
        public IRandomAccessStream Stream { get; set; }
        public string File { get; set; }

        public StreamWithFileName(IRandomAccessStream Stream, string File)
        {
            this.Stream = Stream;
            this.File = File;
        }
    }
}
