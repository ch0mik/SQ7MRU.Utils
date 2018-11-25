using HtmlAgilityPack;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SQ7MRU.Utils
{
    public class DMC
    {
        private CookieContainer container;
        private Uri baseAddress = new Uri("https://www.digital-modes-club.org/");
        private string _username, _password, _path;
        private ILoggerFactory _loggerFactory;
        private ILogger logger;

        public DMC(string username, string password, ILoggerFactory loggerFactory = null, string path = null)
        {
            _username = username;
            _password = password;
            _loggerFactory = loggerFactory;

            if (path == null) { _path = AppContext.BaseDirectory; }
            else { _path = path; }

            if (loggerFactory == null)
            { _loggerFactory = new LoggerFactory(); }

            container = new CookieContainer();

            logger = _loggerFactory.CreateLogger<DMC>();
            logger.LogInformation($"Initialize {nameof(DMC)} Class");

            try
            {
                if (!Directory.Exists(_path)) { Directory.CreateDirectory(_path); };
                Logon();
            }
            catch (Exception exc)
            {
                logger.LogCritical(exc.Message);
            }
        }

        private void Logon()
        {
            using (var handler = new HttpClientHandler() { CookieContainer = container })
            using (var client = new HttpClient(handler) { BaseAddress = baseAddress })
            {
                HtmlDocument doc = new HtmlDocument();

                Task.Run(async () =>
                {
                    doc.LoadHtml(await client.GetAsync("/").Result.Content.ReadAsStringAsync()); ;
                }).GetAwaiter().GetResult();

                string action = "/index.php/en/";
                var form = doc.DocumentNode.SelectSingleNode($"//form[@action=\"{action}\"]");

                FormUrlEncodedContent content = new FormUrlEncodedContent(new[]
                {
                        new KeyValuePair<string, string>("username", _username),
                        new KeyValuePair<string, string>("password", _password),
                        new KeyValuePair<string, string>("Submit",""),
                        new KeyValuePair<string, string>("option","com_users"),
                        new KeyValuePair<string, string>("task","user.login"),
                        new KeyValuePair<string, string>("return",form?.SelectNodes("//input[@name=\"return\"]")[0]?.Attributes["Value"]?.Value),
                        new KeyValuePair<string, string>("mod_id","120"),
                        new KeyValuePair<string, string>(form?.SelectNodes("//input[@value=\"1\"]")[0]?.Attributes["name"]?.Value,"1"),
                });

                var result = client.PostAsync(action, content).Result;
                result.EnsureSuccessStatusCode();

                Task.Run(async () =>
                {
                    doc.LoadHtml(await result.Content.ReadAsStringAsync());
                }).GetAwaiter().GetResult();
            }
        }

        public void Download()
        {
            try
            {
                Logon();
                GetCertsNew();
                GetCertsOld();
            }
            catch (Exception exc)
            {
                logger.LogCritical(exc.Message);
            }
        }

        public void GetCertsNew()
        {
            string pattern = @"location.href = 'https:\/\/www\.digital-modes-club\.org\/awards_pdf2\/(.*)';";
            string action = "/awards_pdf2/download1-user-V3.php";

            logger.LogInformation($"Begin work for new awards");
            using (var handler = new HttpClientHandler() { CookieContainer = container })
            using (var client = new HttpClient(handler) { BaseAddress = baseAddress })
            {
                HtmlDocument doc = new HtmlDocument();

                Task.Run(async () =>
                {
                    doc.LoadHtml(await client.GetAsync(action).Result.Content.ReadAsStringAsync()); ;
                }).GetAwaiter().GetResult();

                var form = doc.DocumentNode.SelectSingleNode("//form");
                action = $"/awards_pdf2/{form.Attributes["action"].Value}";
                string datan_ser = form.SelectSingleNode("//input[@name=\"datan_ser\"]").Attributes["value"].Value;
                string setcall = form.SelectSingleNode("//input[@name=\"setcall\"]").Attributes["value"].Value;

                foreach (var option in doc.DocumentNode.SelectNodes("//select[@name=\"ID_aw\"]/option"))
                {
                    string name = option.InnerText?.Trim();

                    if (!name.Contains("please select"))
                    {
                        string fileName = Path.Combine(_path, $"{name.Replace(" ", "_")}.jpg");

                        if (!File.Exists(fileName))
                        {
                            FormUrlEncodedContent body = new FormUrlEncodedContent(new[]  {
                                new KeyValuePair<string, string>("ID_aw", option.Attributes["value"].Value),
                                new KeyValuePair<string, string>("pdfjpg", "jpg"),
                                new KeyValuePair<string, string>("setcall",setcall),
                                new KeyValuePair<string, string>("datan_ser",datan_ser),
                                new KeyValuePair<string, string>("downl","Download")});

                            Task.Run(async () =>
                            {
                                var match = Regex.Match(await client.PostAsync(action, body).Result.Content.ReadAsStringAsync(), pattern);
                                if (match.Success && match.Groups.Count == 2)
                                {
                                    var content = client.GetAsync($"/awards_pdf2/{match.Groups[1].Value}").Result.Content;
                                    byte[] img = content.ReadAsByteArrayAsync().Result;
                                    File.WriteAllBytes(fileName, img);
                                    logger.LogInformation($"Downloaded {name} to {fileName}");
                                }
                            }).GetAwaiter().GetResult();
                        }
                    }
                }
            }
        }

        public void GetCertsOld()
        {
            string action = "/awards_pdf2/download1-user-old-V3.php";

            logger.LogInformation($"Begin work for old awards");
            using (var handler = new HttpClientHandler() { CookieContainer = container })
            using (var client = new HttpClient(handler) { BaseAddress = baseAddress })
            {
                HtmlDocument doc = new HtmlDocument();

                Task.Run(async () =>
                {
                    doc.LoadHtml(await client.GetAsync(action).Result.Content.ReadAsStringAsync()); ;
                }).GetAwaiter().GetResult();

                var form = doc.DocumentNode.SelectSingleNode("//form");
                action = $"/awards_pdf2/{form.Attributes["action"].Value}";
                string setcall = form.SelectSingleNode("//input[@name=\"setcall\"]").Attributes["value"].Value;

                Parallel.ForEach(doc.DocumentNode.SelectNodes("//select[@name=\"ID_aw\"]/option"), option =>
                 {
                     string name = option.InnerText?.Trim();

                     if (!name.Contains("please select"))
                     {
                         string fileName = Path.Combine(_path, $"{name.Replace(" ", "_")}.jpg");

                         if (!File.Exists(fileName))
                         {
                             string[] parameters = option.Attributes["value"]?.Value?.Split(new char[] { '-' });
                             var content = client.GetAsync($"/awards_pdf2/V3_jpg_{parameters[1]?.Trim()}.php?awid={parameters[0]?.Trim()}&ad=user").Result.Content;
                             byte[] img = content.ReadAsByteArrayAsync().Result;
                             File.WriteAllBytes(fileName, img);
                             logger.LogInformation($"Downloaded {name} to {fileName}");
                         }
                         else
                         {
                             logger.LogTrace($"Skiped {name} : file exists");
                         }
                     }
                 });
            }
        }
    }
}