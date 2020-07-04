﻿namespace Monosoftware.Podcast
{
    /// <summary>
    /// Common namespaces used in RSS feeds for podcasts.
    /// </summary>
    public class XmlNamespaces
    {
        public const string Content = @"http://purl.org/rss/1.0/modules/content/";
        public const string iTunes = @"http://www.itunes.com/dtds/podcast-1.0.dtd";
        public const string CreativeCommons = @"http://backend.userland.com/creativeCommonsRssModule";
        public const string Sy = @"http://purl.org/rss/1.0/modules/syndication/";
        public const string Media = @"http://search.yahoo.com/mrss/";
        public const string Atom = @"http://www.w3.org/2005/Atom";
        public const string RDF = @"http://www.w3.org/1999/02/22-rdf-syntax-ns#";
        public const string DC = @"http://purl.org/dc/elements/1.1/";
    }

    /// <summary>
    /// Common RSS elements used in RSS feeds for podcasts.
    /// </summary>
    public class RssElementNamesExt
    {
        public const string Encoded = "encoded";
        public const string MP3 = "audio/mpeg";
        public const string Artwork = "artwork";
        public const string Subtitle = "subtitle";
        public const string Duration = "duration";
        public const string Type = "type";
        public const string RssXml = @"application/rss+xml";
        public const string TextHtml = @"text/html";
        public const string ImageJpeg = @"image/jpeg";
    }

    /// <summary>
    /// Common OPML elements used to check for valid OPML files
    /// </summary>
    public class OpmlElementNames
    {
        public const string Opml = "opml";
        public const string Version = "version";
    }

    /// <summary>
    /// Constants found in OPML to evaluate against
    /// </summary>
    public class OpmlConstants
    {
        public const string Version = "1.1";
    }
}
