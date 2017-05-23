using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SQ7MRU.Utils
{
    /// <summary>
    /// iQSL downloader from hrdlog.net
    /// </summary>
    public class iQSL
    {
        private readonly Uri baseAddress = new Uri("http://www.hrdlog.net/");
        private readonly CookieContainer container;
        private string path, callsign, subfolder;
        private int concurentDownloads;
        private ILoggerFactory _loggerFactory;
        private ILogger logger;
        public CookieContainer Cookies { get { return container; } }

        /// <summary>
        /// Initialize class iQSL
        /// </summary>
        /// <param name="callsign"></param>
        /// <param name="loggerFactory"></param>
        /// <param name="path"></param>
        /// <param name="subfolder"></param>
        /// <param name="concurentDownloads"></param>
        public iQSL(string callsign, ILoggerFactory loggerFactory = null, string path = null, string subfolder = "iQSL_HRDLOG", int concurentDownloads = 10)
        {
            this.concurentDownloads = concurentDownloads;
            this.container = new CookieContainer();
            this.callsign = callsign;
            this.subfolder = subfolder;
            this._loggerFactory = loggerFactory;

            if (string.IsNullOrEmpty(path))
            { this.path = Directory.GetCurrentDirectory(); }
            else
            { this.path = path; }

            if (loggerFactory == null)
            { this._loggerFactory = new LoggerFactory(); }

            logger = _loggerFactory.CreateLogger<iQSL>();
            logger.LogInformation("Initialize iQSL Class");

        }

        /// <summary>
        /// Downloads the iQSL from hrdlog.net
        /// </summary>
        public void Download()
        {
            Task.Run(async () =>
            {
                var Urls = await GetUrlsAsync();
                logger.LogInformation($"{Urls.Count} iQSLs");
                Parallel.ForEach(Urls, async url =>
                 {
                     await GetJPGfromURLAsync(url);
                 });
            }).GetAwaiter().GetResult();
        }

        private List<string> HTMLtoList(string HTML)
        {
            List<string> UrlList = Regex.Split(HTML, @"((PrintQsl)+[\w\d:#@%/;$()~_?\+-=\\\.&]*)").Where(S => S.Contains("PrintQsl.aspx?id=")).ToList();
            return UrlList;
        }

        private async Task<List<string>> GetUrlsAsync()
        {
            string _old_string = "PrintQsl.aspx?id=";
            string _new_string = "qsl.aspx?id=";
            List<string> _list = new List<string>();

            try
            {
                string action = "searchqso.aspx?log=";
                using (var handler = new HttpClientHandler() { CookieContainer = this.container })
                using (var client = new HttpClient(handler) { BaseAddress = baseAddress })
                {
                    var content = new FormUrlEncodedContent(new[]
                    {
                    new KeyValuePair<string, string>("ctl00$ContentPlaceHolder1$TbCallsign", this.callsign),
                    new KeyValuePair<string, string>("ctl00$ContentPlaceHolder1$CbAllQsl", "on"),
                });

                    this.container.Add(baseAddress, new Cookie("callsign", callsign.ToUpper()));
                    var result = client.PostAsync(action, content).Result;
                    result.EnsureSuccessStatusCode();
                    string response = await result.Content.ReadAsStringAsync();
                    foreach (string str in HTMLtoList(response))
                    {
                        _list.Add(str.Replace(_old_string, _new_string));
                    }

                    return _list;
                }
            }
            catch (Exception exc)
            {
                logger.LogCritical(exc.Message);
                return null;
            }
        }

        private async Task GetJPGfromURLAsync(string url)
        {
            if(!Directory.Exists(Path.Combine(path, subfolder))) { Directory.CreateDirectory(Path.Combine(path, subfolder)); }

            string filename = url.ToLower().Replace("qsl.aspx?id=", "") + ".JPG";
            string pathfile = Path.Combine(path, subfolder, filename);

            if (!File.Exists(pathfile) || new FileInfo(pathfile).Length == 0)
            {
                using (var handler = new HttpClientHandler() { CookieContainer = this.container })
                using (var client = new HttpClient(handler) { BaseAddress = baseAddress })
                {
                    var result = client.GetAsync(url).Result;
                    result.EnsureSuccessStatusCode();
                    var jpg = await result.Content.ReadAsByteArrayAsync();
                    File.WriteAllBytes(pathfile, jpg);
                    logger.LogInformation($"Save the {filename}");
                };
            }
            else
            {
                logger.LogTrace($"Skip the {filename}");
            }
        }

    }
}