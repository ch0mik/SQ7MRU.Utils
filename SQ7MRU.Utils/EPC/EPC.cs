using HtmlAgilityPack;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace SQ7MRU.Utils
{
    public class EPC : HttpClient
    {
        private CookieContainer container;
        private Uri baseAddress = new Uri("https://epc-mc.eu/");
        private string _username, _password, _path;
        private ILoggerFactory _loggerFactory;
        private ILogger logger;
        public Dictionary<string, string> AwardsLinks = new Dictionary<string, string>();

        public EPC(string username, string password, ILoggerFactory loggerFactory = null, string path = null)
        {
            _username = username;
            _password = password;
            _loggerFactory = loggerFactory;

            if (path == null) { _path = AppContext.BaseDirectory; }
            else { _path = path; }

            if (loggerFactory == null)
            { _loggerFactory = new LoggerFactory(); }

            container = new CookieContainer();

            logger = _loggerFactory.CreateLogger<EPC>();
            logger.LogInformation($"Initialize {nameof(EPC)} Class");

            try
            {
                //Logon first
                Logon();

                //Get All Certs
                foreach (string group in AwardsLinks.Keys)
                {
                    GetCerts(group);
                }
            }
            catch(Exception exc)
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

                var form = doc.GetElementbyId("login-form");
                string action = "index.php?lang=en";

                FormUrlEncodedContent content;

                content = new FormUrlEncodedContent(new[]
                {
                        new KeyValuePair<string, string>("username", _username),
                        new KeyValuePair<string, string>("password", _password),
                        new KeyValuePair<string, string>("Submit",""),
                        new KeyValuePair<string, string>("option","com_users"),
                        new KeyValuePair<string, string>("task","user.login"),
                        new KeyValuePair<string, string>("return",form?.SelectNodes("//input[@name=\"return\"]")[0]?.Attributes["Value"]?.Value),
                        new KeyValuePair<string, string>(form?.SelectNodes("//input[@value=\"1\"]")[0]?.Attributes["name"]?.Value,"1"),
                    });

                var result = client.PostAsync(action, content).Result;
                result.EnsureSuccessStatusCode();

                Task.Run(async () =>
                {
                    doc.LoadHtml(await result.Content.ReadAsStringAsync());
                }).GetAwaiter().GetResult();

                bool adding = false;
                string start = "Membership certificate";
                string stop = "Contest certificates";
                foreach (var item in doc.DocumentNode.SelectNodes("//span[@class=\"item-title\"]"))
                {
                    if (item.InnerText == start) { adding = true; }
                    if (item.InnerText == stop) { adding = false; }

                    if (adding)
                    {
                        AwardsLinks.Add(item.InnerText, item.ParentNode.Attributes["href"].Value);
                    }
                }
                if (AwardsLinks.ContainsKey(start)) { AwardsLinks.Remove(start); }
            }
        }


        private void GetCerts(string awardGroup)
        {
            logger.LogInformation($"Begin work for {awardGroup}");

            string[] skips = new[] { "pdf", "jpg", "select award", "select format" };
            List<string> awards = new List<string>();

            using (var handler = new HttpClientHandler() { CookieContainer = container })
            using (var client = new HttpClient(handler) { BaseAddress = baseAddress })
            {
                string action = AwardsLinks[awardGroup];
                HtmlDocument doc = new HtmlDocument();

                Task.Run(async () =>
                {
                    doc.LoadHtml(await client.GetAsync(action).Result.Content.ReadAsStringAsync()); ;
                }).GetAwaiter().GetResult();

                if (!doc.DocumentNode.SelectSingleNode("//div[@class=\"tabbody-content\"]").InnerText.Contains("You have not worked any"))
                {

                    var form = doc.DocumentNode.SelectSingleNode("//div[@itemprop=\"articleBody\"]").SelectNodes("//form[contains(@action,'award')]").First();
                    action = form.Attributes["action"].Value;
                    string currentDirectory = Path.Combine(_path, awardGroup);

                    if (!Directory.Exists(currentDirectory)) { Directory.CreateDirectory(currentDirectory); };

                    Parallel.ForEach(form.SelectNodes("//select/option"), option =>
                    {
                        try
                        {
                            if (!skips.Any(s => option.InnerText.Contains(s)))
                            {
                                string name = option.InnerText;
                                string fileName = Path.Combine(currentDirectory, $"{name}.jpg");

                                if (!File.Exists(fileName))
                                {
                                    logger.LogTrace($"Begin download {awardGroup} => {name}");

                                    string awardSelect = option.ParentNode.Attributes["name"].Value;
                                    string fileFormat = option.ParentNode.ParentNode.SelectSingleNode("//select[contains(@name,'fileformat')]").Attributes["name"].Value;
                                    string submit = option.ParentNode.ParentNode.SelectSingleNode("//input[@type=\"submit\"]").Attributes["name"].Value;

                                    FormUrlEncodedContent body = new FormUrlEncodedContent(new[]
                                    {
                                new KeyValuePair<string, string>(awardSelect,  name),
                                new KeyValuePair<string, string>(fileFormat, "jpg"),
                                new KeyValuePair<string, string>(submit,"Submit")
                                });


                                    Task.Run(async () =>
                                    {
                                        var content = client.PostAsync(action, body).Result.Content;
                                        byte[] img = content.ReadAsByteArrayAsync().Result;
                                        File.WriteAllBytes(fileName, img);
                                        logger.LogInformation($"Downloaded {awardGroup} => {name} to {fileName}");
                                    }).GetAwaiter().GetResult();
                                }
                                else
                                {
                                    logger.LogTrace($"Skiped {awardGroup} => {name} : file exists");
                                }
                            }
                        }
                        catch (Exception exc)
                        {
                            logger.LogCritical(exc.Message);
                        }
                    });
                }
                else
                {
                    logger.LogInformation($"Skiping the {awardGroup} - no awards");
                }

            }
        }
    }
}