using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace Monosoftware.Podcast
{
    public enum UriStatus
    {
        None,
        Malformed,
        NetworkError,
        HttpError,
        Valid
    }

    public struct UriStatusInfo
    {
        public HttpStatusCode HttpStatusCode { get; set; }
        public UriStatus UriStatus { get; set; }

        public UriStatusInfo(UriStatus UriStatus, HttpStatusCode HttpStatusCode = HttpStatusCode.Unused)
        {
            this.UriStatus = UriStatus;
            this.HttpStatusCode = HttpStatusCode;
        }
    }
}
