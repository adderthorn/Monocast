﻿using Microsoft.SyndicationFeed;
using Microsoft.SyndicationFeed.Rss;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Monosoftware.Podcast
{
    /// <summary>
    /// Extension methods and static constants to assist in the library.
    /// </summary>
    public static class CastHelpers
    {
        #region Public Constants
        /// <summary>
        /// The text of "User-agent" to be used in an HTTP request.
        /// </summary>
        public const string USER_AGENT_TEXT = "User-agent";
        public const string DEFAULT_MP3 = "http://www.monocast.co/disconnected.mp3";
        #endregion Public Constants

        #region Private Constants
        private const string USER_AGENT = "Monocast/1.1 (Windows; U; Windows NT 5.1)";
        private const string URL_TEXT = "url";
        private const string HREF_TEXT = "href";
        private static string _UserAgent = USER_AGENT;
        #endregion Private Constants

        #region Public Properties
        /// <summary>
        /// The user agent to be sent in the HTTP request.
        /// </summary>
        public static string UserAgent { get => _UserAgent; set => _UserAgent = value; }
        #endregion Public Properties

        #region Helper Functions
        /// <summary>
        /// Checks the syndication content for an attribute of url or href.
        /// </summary>
        /// <param name="enclosureContent"></param>
        /// <returns>New Uri if found, null if no Uri is found.</returns>
        public static Uri CheckForUri(ISyndicationContent enclosureContent)
        {
            if (enclosureContent == null) return null;
            ISyndicationAttribute tempAttribute = null;
            tempAttribute = enclosureContent.Attributes.FirstOrDefault(a => a.Name == URL_TEXT);
            if (tempAttribute == null) tempAttribute = enclosureContent.Attributes.FirstOrDefault(a => a.Name == HREF_TEXT);
            if (tempAttribute != null) return new Uri(tempAttribute.Value);
            if (enclosureContent.Value?.ToUpper().StartsWith("HTTP") == true) return new Uri(enclosureContent.Value);
            return null;
        }

        /// <summary>
        /// Returns a byte array value from the specified Uri.
        /// </summary>
        /// <param name="uri">Uri (http) to download contents from.</param>
        /// <returns>Byte array of value of contents.</returns>
        public static async Task<byte[]> GetUriReturnByteArrayAsync(Uri uri)
        {
            HttpClient httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add(USER_AGENT_TEXT, _UserAgent);
            var httpRequest = new HttpRequestMessage(HttpMethod.Get, uri);
            var responseMessage = await httpClient.SendAsync(httpRequest, HttpCompletionOption.ResponseContentRead);
            byte[] responseContent = await responseMessage.Content.ReadAsByteArrayAsync();
            return responseContent;
        }

        public static async Task<Uri> ResolveUriRedirectsAsync(Uri uri)
        {
            HttpClient httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add(USER_AGENT_TEXT, _UserAgent);
            var response = await resolveAsync(httpClient, uri);
            int code = (int)response.StatusCode;
            while (code >= 300 && code < 400)
            {
                uri = response.Headers.Location;
                response = await resolveAsync(httpClient, uri);
                code = (int)response.StatusCode;
            }
            return uri;
        }

        private static async Task<HttpResponseMessage> resolveAsync(HttpClient client, Uri uri)
        {
            var response = await client.GetAsync(uri);
            return response;
        }

        /// <summary>
        /// Checks that the Uri string value is a valid HTTP URI that does not return an HTTP error code;
        /// i.e. the status code must be less greater than 199 or less than 300.
        /// </summary>
        /// <param name="UriToTest">Uri to check.</param>
        /// <returns>True/False</returns>
        public static async Task<UriStatusInfo> CheckUriValidAsync(string UriToTest)
        {
            if (!Uri.IsWellFormedUriString(UriToTest, UriKind.RelativeOrAbsolute)) return new UriStatusInfo(UriStatus.Malformed);
            return await CheckUriValidAsync(new Uri(UriToTest));
        }

        public static async Task<UriStatusInfo> CheckUriValidAsync(Uri UriToTest)
        {
            if (!UriToTest.IsWellFormedOriginalString()) return new UriStatusInfo(UriStatus.Malformed);
            try
            {
                HttpClient request = new HttpClient();
                request.DefaultRequestHeaders.Add(USER_AGENT_TEXT, _UserAgent);
                HttpResponseMessage response = await request.GetAsync(UriToTest);
                if ((int)response.StatusCode == 403)
                {
                    return new UriStatusInfo(UriStatus.Unauthorized, response.StatusCode);
                }
                if ((int)response.StatusCode >= 300 && (int)response.StatusCode < 400)
                {
                    return new UriStatusInfo(UriStatus.Redirect, response.StatusCode);
                }
                if ((int)response.StatusCode < 200 || (int)response.StatusCode >= 400)
                {
                    return new UriStatusInfo(UriStatus.HttpError, response.StatusCode);
                }
                return new UriStatusInfo(UriStatus.Valid, response.StatusCode);

            }
            catch (UriFormatException)
            {
                return new UriStatusInfo(UriStatus.Malformed);
            }
            catch (HttpRequestException)
            {
                return new UriStatusInfo(UriStatus.NetworkError);
            }
            catch
            {
                return new UriStatusInfo(UriStatus.None);
            }
        }
        #endregion Helper Functions
    }
}
