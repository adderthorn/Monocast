using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using Monosoftware.Podcast;

namespace Monocast
{
    public class VersionConverter
    {
        public const string OLD_NAMESPACE = "http://schemas.datacontract.org/2004/07/Mono.Podcasts";
        public Subscriptions Subscriptions { get; private set; }

        public VersionConverter()
        {
            Subscriptions = new Subscriptions();
        }

        public async Task LoadAsync(MemoryStream stream)
        {
            XmlReader reader = XmlReader.Create(stream);
            try
            {
                bool foundPodcastNode = false;
                while (reader.Read())
                {
                    if (reader.NodeType == XmlNodeType.Element && reader.Name.Equals("Podcast", StringComparison.CurrentCultureIgnoreCase))
                    {
                        foundPodcastNode = true;
                    }
                    else if (foundPodcastNode && reader.NodeType == XmlNodeType.Element && reader.Name.Equals("FeedUri", StringComparison.CurrentCultureIgnoreCase))
                    {
                        if (reader.Read() && reader.NodeType == XmlNodeType.Text)
                        {
                            string uri = reader.Value;
                            var podcast = await Podcast.GetPodcastAsync(uri);
                            Subscriptions.AddPodcast(podcast);
                        }
                    }
                    else if (foundPodcastNode && reader.NodeType == XmlNodeType.EndElement && reader.Name.Equals("Podcast", StringComparison.CurrentCultureIgnoreCase))
                    {
                        foundPodcastNode = false;
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                reader.Dispose();
            }
        }
    }
}
