using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;

namespace Monosoftware.Podcast
{
    public enum UriStatus
    {
        None,
        Malformed,
        NetworkError,
        HttpError,
        Valid,
        Unauthorized,
        Redirect
    }

    public struct UriStatusInfo
    {
        public HttpStatusCode HttpStatusCode { get; set; }
        public UriStatus UriStatus { get; set; }
        public HttpResponseHeaders Headers { get; set; }

        public UriStatusInfo(UriStatus UriStatus,
                             HttpStatusCode HttpStatusCode = HttpStatusCode.Unused,
                             HttpResponseHeaders Headers = null)
        {
            this.UriStatus = UriStatus;
            this.HttpStatusCode = HttpStatusCode;
            this.Headers = Headers;
        }
    }
}
