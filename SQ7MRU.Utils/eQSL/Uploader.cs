using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;

namespace SQ7MRU.Utils
{
    public class Uploader
    {

        private readonly CookieContainer container;
        private int concurentDownloads, sleepTime, maxRetry;
        private readonly Uri baseAddress = new Uri("https://eqsl.cc/qslcard/");
        private ILoggerFactory _loggerFactory;
        private ILogger logger;


        public Uploader(ILoggerFactory loggerFactory = null)
        {
            container = new CookieContainer();
            _loggerFactory = loggerFactory;

            if (loggerFactory == null)
            { this._loggerFactory = new LoggerFactory(); }

            logger = _loggerFactory.CreateLogger<Downloader>();
            logger.LogInformation("Initialize Uploader Class");
        }

            /// <summary>
            /// Logon to eQSL.cc
            /// </summary>
            /// <param name="callSign"></param>
            /// <param name="hamID"></param>
            private void Logon(string callSign, string password, string hamID = null)
        {
            string action = "LoginFinish.cfm";
            using (var handler = new HttpClientHandler() { CookieContainer = container })
            using (var client = new HttpClient(handler) { BaseAddress = baseAddress })
            {
                FormUrlEncodedContent content;

                if (!string.IsNullOrEmpty(hamID))
                {
                    content = new FormUrlEncodedContent(new[]
                    {
                        new KeyValuePair<string, string>("HamID", hamID),
                        new KeyValuePair<string, string>("Callsign", callSign),
                        new KeyValuePair<string, string>("EnteredPassword", password),
                        new KeyValuePair<string, string>("SelectCallsign","Log+In")
                    });
                }
                else
                {
                    content = new FormUrlEncodedContent(new[]
                    {
                        new KeyValuePair<string, string>("Callsign", callSign),
                        new KeyValuePair<string, string>("EnteredPassword", password),
                        new KeyValuePair<string, string>("Login", "Go")
                    });
                }
                var result = client.PostAsync(action, content).Result;
                result.EnsureSuccessStatusCode();
            }
        }


        public bool UploadAdif(string adif, string callSign, string password, string hamID = null)
        {

            bool result = false;
            string action = "UploadFile.cfm";

            Logon(callSign, password, hamID);

            using (var handler = new HttpClientHandler() { CookieContainer = container })
            using (var client = new HttpClient(handler) { BaseAddress = baseAddress })
            {
                using (MultipartFormDataContent content = new MultipartFormDataContent())
                {
                    var values = new[]
                       {
                            new KeyValuePair<string, string>("AsyncMode", "TRUE")
                       };

                    foreach (var keyValuePair in values)
                    {
                        content.Add(new StringContent(keyValuePair.Value), keyValuePair.Key);
                    }

                    var fileStream = new MemoryStream(Encoding.UTF8.GetBytes(adif));
                    var streamContent = new StreamContent(fileStream);
                    streamContent.Headers.ContentDisposition = new ContentDispositionHeaderValue("form-data");
                    streamContent.Headers.ContentDisposition.Name = "\"Filename\"";
                    streamContent.Headers.ContentDisposition.FileName = "\"" + "UPLOAD.ADIF" + "\"";
                    streamContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
                    string boundary = new Guid().ToString();
                    var fContent = new MultipartFormDataContent(boundary);
                    fContent.Headers.Remove("Content-Type");
                    fContent.Headers.TryAddWithoutValidation("Content-Type", "multipart/form-data; boundary=" + boundary);
                    fContent.Add(streamContent);
                    var response = client.PostAsync(action, fContent).Result;
                    if (response.StatusCode == HttpStatusCode.OK)
                    {
                        result = true;
                        logger.LogInformation("Upload success");
                    }
                    else
                    {
                        logger.LogError($"Upload failure : {response.StatusCode}");
                    }
                    return result;
                }
            }
        }

    }
}