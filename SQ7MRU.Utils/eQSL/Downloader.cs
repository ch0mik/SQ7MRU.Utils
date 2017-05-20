﻿using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SQ7MRU.Utils
{
    public class Downloader : HttpClient
    {
        private readonly CookieContainer container = new CookieContainer();

        private string callsign, password, path;
        private int concurentDownloads;
        private ConcurrentDictionary<string, string> adifFiles = new ConcurrentDictionary<string, string>();
        private ILoggerFactory loggerFactory = new LoggerFactory();
        private readonly string patternAlphaNumeric = "[^a-zA-ZæÆøØåÅéÉöÖäÄüÜ-ñÑõÕéÉáÁóÓôÔzżźćńółęąśŻŹĆĄŚĘŁÓŃ _]";
        private readonly Uri baseAddress = new Uri("http://eqsl.cc/qslcard/");
        private List<CallAndQTH> callAndQTHList;
        private ConcurrentDictionary<CallAndQTH, List<string>> urlsFromAdifs;
        public ILogger logger;
        public List<CallAndQTH> CallSigns { get { return callAndQTHList; } }
        public CookieContainer Cookies { get { return container; } }

        public Downloader(string callsign, string password, LogLevel logLevel = LogLevel.Error, string path = null, int concurentDownloads = 10)
        {
            this.concurentDownloads = concurentDownloads;
            this.container = new CookieContainer();
            this.callAndQTHList = new List<CallAndQTH>();
            this.urlsFromAdifs = new ConcurrentDictionary<CallAndQTH, List<string>>();
            this.callsign = callsign;
            this.password = password;

            if (string.IsNullOrEmpty(path))
            {
                this.path = Directory.GetCurrentDirectory();
            }
            else
            {
                this.path = path;
            }

            try
            {
                logger = loggerFactory.CreateLogger<Downloader>();
#if DEBUG
                logger.IsEnabled(LogLevel.Trace);
#endif

                logger.LogInformation("Initialize Downloader Class");

                Task.Run(async () =>
                {
                    await LogonAsync();
                    await GetCallAndQTHAsync();
                }).GetAwaiter().GetResult();
            }
            catch (Exception exc)
            {
                logger.LogCritical(exc.Message);
            }
        }

        private async System.Threading.Tasks.Task LogonAsync()
        {
            string action = "LoginFinish.cfm";
            using (var handler = new HttpClientHandler() { CookieContainer = this.container })
            using (var client = new HttpClient(handler) { BaseAddress = baseAddress })
            {
                var content = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("Callsign", this.callsign),
                    new KeyValuePair<string, string>("EnteredPassword", this.password),
                });
                var result = client.PostAsync(action, content).Result;
                result.EnsureSuccessStatusCode();
                string response = await result.Content.ReadAsStringAsync();

                if (response.Contains(@">Select one<"))
                {
                    string[] QTHNicknamesArray = Regex.Split(response, @"NAME=""HamID"" VALUE=""(.*)""").Where(S => S.Length < 50).ToArray();

                    foreach (var hamId in QTHNicknamesArray)
                    {
                        content = new FormUrlEncodedContent(new[]
                        {
                        new KeyValuePair<string, string>("HamID", hamId),
                        new KeyValuePair<string, string>("EnteredPassword", this.password),
                        new KeyValuePair<string, string>("SelectCallsign","Log+In")
                        });

                        result = client.PostAsync(action, content).Result;
                        result.EnsureSuccessStatusCode();
                        response = await result.Content.ReadAsStringAsync();
                    }
                }
            }
        }

        public void Logon(string callSign, string hamID = null)
        {
            string action = "LoginFinish.cfm";
            using (var handler = new HttpClientHandler() { CookieContainer = this.container })
            using (var client = new HttpClient(handler) { BaseAddress = baseAddress })
            {
                FormUrlEncodedContent content;

                if (!string.IsNullOrEmpty(hamID))
                {
                    content = new FormUrlEncodedContent(new[]
                    {
                        new KeyValuePair<string, string>("HamID", hamID),
                        new KeyValuePair<string, string>("EnteredPassword", this.password),
                        new KeyValuePair<string, string>("SelectCallsign","Log+In")
                    });
                }
                else
                {
                    content = new FormUrlEncodedContent(new[]
                    {
                        new KeyValuePair<string, string>("Callsign", this.callsign),
                        new KeyValuePair<string, string>("EnteredPassword", this.password),
                        new KeyValuePair<string, string>("Login", "Go")
                    });
                }
                var result = client.PostAsync(action, content).Result;
                result.EnsureSuccessStatusCode();
            }
        }

        private async Task GetCallAndQTHAsync()
        {
            string action = "MyAccounts.cfm";
            using (var handler = new HttpClientHandler() { CookieContainer = this.container })
            using (var client = new HttpClient(handler) { BaseAddress = baseAddress })
            {
                var result = client.GetAsync(action).Result;
                result.EnsureSuccessStatusCode();
                var response = await result.Content.ReadAsStringAsync();

                response = response.Replace("<BR><STRONG>Primary</STRONG>", "");

                if (response.Contains("You currently have no other accounts attached."))
                {
                    CallAndQTH callqth = new CallAndQTH();
                    callqth.CallSign = this.callsign;
                    callqth.QTH = "";
                    callAndQTHList.Add(callqth);
                    callqth = null;
                }
                else
                {
                    string[] CallAndQTHArray = Regex.Split(response, @"<TD\b[^>]*>(.*?)\<BR\>\((.*?)\)</TD>\r\n").Where(S => S.Length < 50).ToArray();
                    string[] HamIDArray = Regex.Split(response, @"NAME=""HamID"" VALUE=""(.*)""").Where(S => S.Length < 50).ToArray();

                    if (CallAndQTHArray.Length % 2 == 0)
                    {
                        for (int i = 0; i < CallAndQTHArray.Length; i++)
                        {
                            CallAndQTH callqth = new CallAndQTH();
                            callqth.CallSign = CallAndQTHArray[i];
                            callqth.QTH = CallAndQTHArray[i + 1];
                            callqth.HamID = HamIDArray[i + 1];
                            callAndQTHList.Add(callqth);
                            callqth = null;
                            i++;
                        }
                    }
                }
            }
        }

        public void GetAdifs()
        {
            //info : Paraller.ForEach is not got, beacouse eqsl.cc works with cookies
            foreach (var callQth in callAndQTHList)
            {
                Task.Run(async () =>
                {
                    logger.LogTrace($"Geting ADIF for CallSign {callQth.CallSign}");
                    Logon(callQth.CallSign, callQth.HamID);
                    await GetSingleAdifAsync(callQth);
                    logger.LogTrace($"ADIF for CallSign {callQth.CallSign} was saved in '{adifFiles[callQth.CallSign]}' file");
                }).GetAwaiter().GetResult();
            }
        }

        public void GetSingleAdif(CallAndQTH callQth)
        {
            Task.Run(async () =>
            {
                logger.LogInformation($"Geting ADIF for CallSign {callQth.CallSign}");
                Logon(callQth.CallSign, callQth.HamID);
                await GetSingleAdifAsync(callQth);
                logger.LogInformation($"ADIF for CallSign {callQth.CallSign} was saved in '{adifFiles[callQth.CallSign]}' file");
            }).GetAwaiter().GetResult();
        }

        private async Task GetSingleAdifAsync(CallAndQTH callQth)
        {
            Logon(callQth.CallSign, callQth.HamID);
            string action = $"DownloadInBox.cfm?UserName={callQth.CallSign}&Password={this.password}";
            using (var handler = new HttpClientHandler() { CookieContainer = this.container })
            using (var client = new HttpClient(handler) { BaseAddress = baseAddress })
            {
                if (!string.IsNullOrEmpty(callQth.QTH))
                {
                    action += $"&QTHNickname={callQth.QTH}";
                }

                var result = client.GetAsync(action).Result;
                result.EnsureSuccessStatusCode();
                var response = await result.Content.ReadAsStringAsync();
                var adifUrl = Regex.Match(response, @"((downloadedfiles\/)+[\w\d:#@%/;$()~_?\+-=\\\.&]*)").ToString();
                var adifFile = Path.Combine(path, callQth.CallSign.Replace("/", "_") + "-" + Regex.Replace(callQth.QTH, patternAlphaNumeric, "") + ".ADIF");
                result = client.GetAsync(adifUrl).Result;
                result.EnsureSuccessStatusCode();
                var adif = await result.Content.ReadAsStringAsync();
                File.WriteAllText(adifFile, adif);
                adifFiles.TryAdd(callQth.CallSign, adifFile);
            }
        }

        public void DownloadJPGs()
        {
            Parallel.ForEach(callAndQTHList, callQth =>
            {
                logger.LogInformation($"Generating Urls from ADIF for CallSign {callQth.CallSign}");
                urlsFromAdifs.TryAdd(callQth, GetUrlsFromAdif(callQth));
                logger.LogInformation($"Generated {urlsFromAdifs[callQth].Count} Urls for CallSign {callQth.CallSign}");
            });

            foreach (CallAndQTH callQth in callAndQTHList)
            {
                string callsignSubDir = Path.Combine(this.path, callQth.CallSign.Replace("/", "_"));
                if (!Directory.Exists(callsignSubDir)) { Directory.CreateDirectory(callsignSubDir); }

                logger.LogInformation($"Start downloads {urlsFromAdifs[callQth].Count} e-QSLs for CallSign {callQth.CallSign}");

                Logon(callQth.CallSign, callQth.HamID);

                Console.WriteLine($"Start downloads {urlsFromAdifs[callQth].Count} e-QSLs for CallSign {callQth.CallSign}");

                using (var handler = new HttpClientHandler() { CookieContainer = this.container })
                using (var client = new HttpClient(handler) { BaseAddress = baseAddress })
                {
                    Parallel.ForEach(urlsFromAdifs[callQth], new ParallelOptions() { MaxDegreeOfParallelism = 100 }, async action =>
                    {
                        var file = FilenameFromURL(action);
                        if (!File.Exists(Path.Combine(callsignSubDir, file)))
                        {
                            try
                            {
                                var result = client.GetAsync(action).Result;
                                result.EnsureSuccessStatusCode();
                                var response = await result.Content.ReadAsStringAsync();
                                action = $"http://eqsl.cc{Regex.Match(response, @"((\/CFFileServlet\/_cf_image\/)+[\w\d:#@%/;$()~_?\+-=\\\.&]*)").ToString()}";
                                result = client.GetAsync(action).Result;
                                result.EnsureSuccessStatusCode();
                                var jpg = await result.Content.ReadAsByteArrayAsync();
                                File.WriteAllBytes(Path.Combine(callsignSubDir, file), jpg);
                                logger.LogTrace($"Save eQSL {file}  for CallSign {callQth.CallSign}");
#if DEBUG
                                Console.WriteLine($"Save eQSL {file}  for CallSign {callQth.CallSign}");
#endif
                            }
                            catch (WebException exc)
                            {
                                logger.LogError($"Error during download {file} for CallSign {callQth.CallSign}; WebException : {exc.Message}");
#if DEBUG
                                Console.WriteLine($"Save eQSL {file}  for CallSign {callQth.CallSign}");
#endif 
                            }
                            catch (Exception exc)
                            {
                                logger.LogCritical($"Error during download {file} for CallSign {callQth.CallSign}; Exception : {exc.Message}");
                            }
                        }
                        else
                        {
                            logger.LogTrace($"Skipped eQSL {file}  for CallSign {callQth.CallSign}");
#if DEBUG
                            Console.WriteLine($"Skipped eQSL {file}  for CallSign {callQth.CallSign}");
#endif
                        }
                    });
                }

                logger.LogInformation($"Finished download e-QSLs for CallSign {callQth.CallSign}");
            }
        }


        public string FilenameFromURL(string URL)
        {
            Dictionary<string, string> dic = UrlHelper.Decode(URL).Replace($"DisplayeQSL.cfm?", "").Split(
                                           new char[] { '&' }, StringSplitOptions.RemoveEmptyEntries).ToDictionary(s => s.Split('=')[0].ToLower(), s => s.Split('=')[1].ToUpper());
            return  $"{dic["qsodate"].Replace(":00.0", "").Replace(":", "").Replace(" ", "").Replace("-", "")}_{dic["callsign"].Replace("/", "-")}_{dic["band"]}_{dic["mode"]}.JPG";
        }

        private List<string> GetUrlsFromAdif(CallAndQTH c)
        {
            using (AdifReader ar = new AdifReader(adifFiles[c.CallSign]))
            {
                List<string> Urls = new List<string>();
                List<AdifRow> rows = ar.GetAdifRows();
                foreach (AdifRow r in rows)
                {
                    try
                    {
                        Urls.Add($"DisplayeQSL.cfm?Callsign={r.call}&VisitorCallsign={c.CallSign}" +
                                 $"&QSODate={ConvertStringQSODateTimeOnToFormattedDateTime(r.qso_date + r.time_on).Replace(" ", "%20")}:00.0" +
                                 $"&Band={r.band}&Mode={r.mode}");
                    }
                    catch (Exception exc)
                    {
                        logger.LogCritical(exc.Message);
                    }
                }

                return Urls;
            }
        }

        private string ConvertStringQSODateTimeOnToFormattedDateTime(string QSODateTimeOn)
        {
            string _datetimeconverted = QSODateTimeOn;

            if (!(string.IsNullOrEmpty(QSODateTimeOn)))
            {
                try
                {
                    _datetimeconverted = DateTime.ParseExact(QSODateTimeOn.Replace(" ", ""), "yyyyMMddHHmm", null).ToString("yyyy-MM-dd HH:mm");
                }
                catch (Exception exc)
                {
                    logger.LogWarning($"Cant convert date {QSODateTimeOn}");
                    logger.LogWarning(exc.Message);
                }
            }

            return _datetimeconverted;
        }
    }
}