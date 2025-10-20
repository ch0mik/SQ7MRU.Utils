using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Threading;
using HtmlAgilityPack;

namespace SQ7MRU.Utils
{
    /// <summary>
    /// eQSL downloader from eqsl.cc
    /// </summary>
    public class Downloader : HttpClient
    {
        private readonly CookieContainer container;
        private string callsign, password, path;
        private int concurentDownloads, sleepTime, maxRetry;
        private readonly string patternAlphaNumeric = "[^a-zA-ZæÆøØåÅéÉöÖäÄüÜ-ñÑõÕéÉáÁóÓôÔzżźćńółęąśŻŹĆĄŚĘŁÓŃ _]";
        private readonly Uri baseAddress = new Uri("https://eqsl.cc/qslcard/");
        public List<CallAndQTH> callAndQTHList;
        private ILoggerFactory _loggerFactory;
        private ILogger logger;
        public List<CallAndQTH> CallSigns { get { return callAndQTHList; } }


        /// <summary>
        /// Initialize class Downloader, logon to eQSL.cc and get CallSign/QTH for this login
        /// </summary>
        /// <param name="callsign"></param>
        /// <param name="password"></param>
        /// <param name="loggerFactory"></param>
        /// <param name="path"></param>
        /// <param name="concurentDownloads"></param>
        public Downloader(string callsign, string password, ILoggerFactory loggerFactory = null, 
        string path = null, int concurentDownloads = 10, int sleepTime = 100, int maxRetry = 100)
        {
            this.concurentDownloads = concurentDownloads;
            this.sleepTime = sleepTime;
            this.maxRetry = maxRetry;
            this.container = new CookieContainer();
            this.callAndQTHList = new List<CallAndQTH>();
            this.callsign = callsign;
            this.password = password;
            this._loggerFactory = loggerFactory;

            try
            {
                if (string.IsNullOrEmpty(path))
                { this.path = Directory.GetCurrentDirectory(); }
                else
                { this.path = path; }

                if (loggerFactory == null)
                { this._loggerFactory = new LoggerFactory(); }

                logger = _loggerFactory.CreateLogger<Downloader>();
                logger.LogInformation("Initialize Downloader Class");
                               
            }
            catch (Exception exc)
            {
                logger.LogCritical(exc.Message);
            }
        }

        public async Task LogonAsync()
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
                        new KeyValuePair<string, string>("Callsign", this.callsign),
                        new KeyValuePair<string, string>("SelectCallsign","Log+In")
                        });

                        result = client.PostAsync(action, content).Result;
                        result.EnsureSuccessStatusCode();
                        response = await result.Content.ReadAsStringAsync();
                    }
                }
            }
        }

        /// <summary>
        /// Logon to eQSL.cc
        /// </summary>
        /// <param name="callSign"></param>
        /// <param name="hamID"></param>
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
                        new KeyValuePair<string, string>("Callsign", callSign),
                        new KeyValuePair<string, string>("EnteredPassword", this.password),
                        new KeyValuePair<string, string>("SelectCallsign","Log+In")
                    });
                }
                else
                {
                    content = new FormUrlEncodedContent(new[]
                    {
                        new KeyValuePair<string, string>("Callsign", callSign),
                        new KeyValuePair<string, string>("EnteredPassword", this.password),
                        new KeyValuePair<string, string>("Login", "Go")
                    });
                }
                var result = client.PostAsync(action, content).Result;
                result.EnsureSuccessStatusCode();
            }
        }

        public async Task GetCallAndQTHAsync()
        {
            await LogonAsync();

            string action = "MyAccounts.cfm";
            using (var handler = new HttpClientHandler() { CookieContainer = this.container })
            using (var client = new HttpClient(handler) { BaseAddress = baseAddress })
            {
                var result = client.GetAsync(action).Result;
                result.EnsureSuccessStatusCode();
                var response = await result.Content.ReadAsStringAsync();

                response = response?.Replace("<BR><STRONG>Primary</STRONG>", "");

                if (response.Contains("You currently have no other accounts attached."))
                {
                    HtmlDocument doc = new HtmlDocument();
                    
                    doc.LoadHtml(response);
                    var forms = doc.DocumentNode.SelectNodes("//form");

                    foreach (var form in forms)
                    {
                        if (form.Attributes["action"].Value == "AttachAccount.cfm")
                        {
                            CallAndQTH callqth = new CallAndQTH();
                            callqth.QTH = "";
                            callqth.CallSign = form.ChildNodes[5]?.ChildNodes[1]?.InnerText;
                            callqth.UserID = form.Elements("input")?.ToArray()[0]?.Attributes["value"]?.Value;
                            callqth.HamID = form.Elements("input")?.ToArray()[1]?.Attributes["value"]?.Value;
                            callAndQTHList.Add(callqth);
                        }
                    }
                    
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

        /// <summary>
        /// Download ADIF for CallSign
        /// </summary>
        /// <param name="callQth"></param>
        public void GetSingleAdif(CallAndQTH callQth)
        {
            Task.Run(async () =>
            {
                Logon(callQth.CallSign, callQth.HamID);
                await GetSingleAdifAsync(callQth);
            }).GetAwaiter().GetResult();
        }

        private async Task GetSingleAdifAsync(CallAndQTH callQth)
        {
            logger.LogTrace($"Geting ADIF for CallSign {callQth.CallSign}");
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
                var adifFile = Path.Combine(path, $"{callQth.CallSign.Replace("/", "_")}{Regex.Replace(callQth.QTH, patternAlphaNumeric, "")}.ADIF");
                result = client.GetAsync(adifUrl).Result;
                result.EnsureSuccessStatusCode();
                var adif = await result.Content.ReadAsStringAsync();
                File.WriteAllText(adifFile, adif);
                callQth.Adif = adifFile;
                logger.LogInformation($"ADIF for CallSign {callQth.CallSign} was saved in '{callQth.Adif}' file");
            }
        }

        /// <summary>
        /// Get Single ADIF
        /// </summary>
        /// <param name="callsign"></param>
        /// <param name="passwd"></param>
        /// <param name="hamId"></param>
        /// <param name="qth"></param>
        /// <returns></returns>
        public string GetSingleAdif(string callsign, string passwd, string hamId = null)
        {
            logger.LogTrace($"Geting ADIF for CallSign {callsign}");
            Logon(callsign, hamId);
            string action = $"DownloadInBox.cfm";
            using (var handler = new HttpClientHandler() { CookieContainer = container })
            using (var client = new HttpClient(handler) { BaseAddress = baseAddress })
            {
                var result = client.GetAsync(action).Result;
                result.EnsureSuccessStatusCode();
                var response = result.Content.ReadAsStringAsync().Result;
                var adifUrl = Regex.Match(response, @"((downloadedfiles\/)+[\w\d:#@%/;$()~_?\+-=\\\.&]*)").ToString();
                result = client.GetAsync(adifUrl).Result;
                result.EnsureSuccessStatusCode();
                return result.Content.ReadAsStringAsync().Result;
            }
        }

        /// <summary>
        /// Downloads the ADIF file and eQSL for CallSign
        /// </summary>
        public void Download()
        {
            Task.Run(async () =>
            {
                await LogonAsync();
                await GetCallAndQTHAsync();

            }).GetAwaiter().GetResult();

            foreach (CallAndQTH callQth in callAndQTHList)
            {
                GetSingleAdif(callQth);

                using (AdifReader ar = new AdifReader(File.ReadAllText(callQth.Adif)))
                {
                    Parallel.ForEach(ar.GetAdifRows().ToArray(), qso =>
                    {
                        callQth.QSOs.TryAdd(qso, null);
                    });
                }

                string callsignSubDir = Path.Combine(this.path, callQth.CallSign.Replace("/", "_"));
                if (!Directory.Exists(callsignSubDir)) { Directory.CreateDirectory(callsignSubDir); }

                logger.LogInformation($"Start downloads {callQth.QSOs.Count} e-QSLs for CallSign {callQth.CallSign}");

                Logon(callQth.CallSign, callQth.HamID);

                using (var handler = new HttpClientHandler() { CookieContainer = this.container })
                using (var client = new HttpClient(handler) { BaseAddress = baseAddress })
                {
                    Parallel.ForEach(callQth.QSOs, new ParallelOptions() { MaxDegreeOfParallelism = concurentDownloads }, async qso =>
                    {
                        string action = GetUrlFromQSO(qso.Key, callQth);
                        var file = FilenameFromURL(action);
                        callQth.QSOs[qso.Key] = file;

                        //downloading eQSL if not exist on disk
                        if (!File.Exists(Path.Combine(callsignSubDir, file)) || new FileInfo(Path.Combine(callsignSubDir, file)).Length == 0)
                        {
                            try
                            {
                                bool slowDown= true;
                                int _sleepTime = 0;
                                int retry = 0;
                                while(retry < maxRetry || slowDown)
                                {
                                retry +=1; 
                                if(retry > 1) 
                                    {
                                        logger.LogTrace($"Retry {retry} for CallSign {callQth.CallSign} : Call {qso.Key.CALL} Mode {qso.Key.SUBMODE ?? qso.Key.MODE} on {qso.Key.QSO_DATE}");   
                                    }
                                
                                if(retry == maxRetry)
                                    {
                                        logger.LogWarning($"Abort downloading eQSL for CallSign {callQth.CallSign} : Call {qso.Key.CALL} Mode {qso.Key.SUBMODE ?? qso.Key.MODE} on {qso.Key.QSO_DATE}, maxRetry {retry}");   
                                        slowDown = false;
                                        break;
                                    }

                                var result = client.GetAsync(action).Result;
                                result.EnsureSuccessStatusCode();
                                var response = await result.Content.ReadAsStringAsync();
                               
                                if(response.Contains("There is no entry for a QSO"))
                                {
                                     logger.LogWarning($"No confirm eQSL for CallSign {callQth.CallSign} : Call {qso.Key.CALL} Mode {qso.Key.SUBMODE ?? qso.Key.MODE} on {qso.Key.QSO_DATE}");
                                     slowDown = false; 
                                     break;
                                }

                                if(File.Exists(Path.Combine(callsignSubDir, file)))
                                {
                                   logger.LogTrace($"Skipped eQSL {file}  for CallSign {callQth.CallSign}");
                                   slowDown = false; 
                                   break;
                                }
                                else 
                                    {
                                    if((!response.Contains("ERROR - Too many queries overloading the system. Slow down!")) && (result.StatusCode.ToString() == "OK"))
                                        {
                                        
                                        action = $"http://eqsl.cc{Regex.Match(response, @"((\/CFFileServlet\/_cf_image\/)+[\w\d:#@%/;$()~_?\+-=\\\.&]*)").ToString()}";
                                        result = client.GetAsync(action).Result;
                                        result.EnsureSuccessStatusCode();
                                        var jpg = await result.Content.ReadAsByteArrayAsync();
                                            
                                            if(!Encoding.UTF8.GetString(jpg).Contains("HTML"))
                                                {
                                                File.WriteAllBytes(Path.Combine(callsignSubDir, file), jpg);
                                                logger.LogInformation($"Save eQSL {file}  for CallSign {callQth.CallSign}");
                                                slowDown = false;
                                                break;   
                                                }
                                        }
                                        else
                                        {
                                            Thread.Sleep(_sleepTime +=sleepTime);  //prevent for  "ERROR - Too many queries overloading the system. Slow down!"
                                            if(_sleepTime > 60000 ) { _sleepTime = 0;}     
                                        }
                                    }
                                }
                            }
                            catch (Exception exc)
                            {
                                logger.LogCritical($"Error during download {file} for CallSign {callQth.CallSign}; Exception : {exc.Message}");
                            }
                        }
                        else
                        {
                            logger.LogTrace($"Skipped eQSL {file}  for CallSign {callQth.CallSign}");
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

            string qsoDateRaw = dic.ContainsKey("qsodate") ? dic["qsodate"].Replace(":00.0", "") : string.Empty; // e.g. 2012-05-29 22:14
            string datePart = qsoDateRaw;
            string timePart = string.Empty;

            // Try parse known patterns: yyyy-MM-dd HH:mm or yyyyMMddHHmm or yyyy-MM-ddHH:mm etc.
            if (!string.IsNullOrEmpty(qsoDateRaw))
            {
                // Normalize separators
                var normalized = qsoDateRaw.Replace(" ", "").Replace(":", "").Replace("-", ""); // e.g. 201205292214 or 20120529 2214 -> 201205292214
                if (normalized.Length >= 12)
                {
                    // yyyyMMddHHmm -> split
                    datePart = normalized.Substring(0, 8); // yyyyMMdd
                    timePart = normalized.Substring(8, 4); // HHmm
                }
                else
                {
                    // fallback: remove non-digit and try to parse digits
                    var digits = new string(qsoDateRaw.Where(char.IsDigit).ToArray());
                    if (digits.Length >= 12)
                    {
                        datePart = digits.Substring(0, 8);
                        timePart = digits.Substring(8, 4);
                    }
                    else
                    {
                        // give up and use raw cleaned-up string
                        datePart = qsoDateRaw.Replace(" ", "").Replace(":", "").Replace("-", "");
                    }
                }
            }

            string callsign = dic.ContainsKey("callsign") ? dic["callsign"].Replace("/", "-") : "UNKNOWN";
            string band = dic.ContainsKey("band") ? dic["band"] : "";
            string mode = dic.ContainsKey("mode") ? dic["mode"] : "";

            if (!string.IsNullOrEmpty(timePart))
            {
                return $"{callsign}_{datePart}_{timePart}_{band}_{mode}.JPG";
            }

            // fallback to previous behaviour if we couldn't split date/time
            return $"{datePart}_{callsign}_{band}_{mode}.JPG";
        }

        public List<string> GetUrlsFromAdif(CallAndQTH c)
        {
            using (AdifReader ar = new AdifReader(File.ReadAllText(c.Adif)))
            {
                List<string> Urls = new List<string>();
                List<AdifRow> rows = ar.GetAdifRows();
                foreach (AdifRow r in rows)
                {
                    try
                    {
                        Urls.Add(GetUrlFromQSO(r, c));
                    }
                    catch (Exception exc)
                    {
                        logger.LogCritical(exc.Message);
                    }
                }

                return Urls;
            }
        }

        public string GetUrlFromQSO(AdifRow r, CallAndQTH c)
        {
            try
            {
               return $"DisplayeQSL.cfm?Callsign={r.CALL}&VisitorCallsign={c.CallSign}" +
                                 $"&QSODate={ConvertStringQSODateTimeOnToFormattedDateTime(r.QSO_DATE + r.TIME_ON).Replace(" ", "%20")}:00.0" +
                                 $"&Band={r.BAND}&Mode={r.SUBMODE ?? r.MODE}";
               
               
            }
            catch (Exception exc)
            {
                logger.LogCritical(exc.Message);
                return null;
            }
        }

        public string ConvertStringQSODateTimeOnToFormattedDateTime(string QSODateTimeOn)
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