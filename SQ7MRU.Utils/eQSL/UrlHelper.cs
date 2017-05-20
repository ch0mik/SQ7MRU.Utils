using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace SQ7MRU.Utils
{
    public static class UrlHelper
    {
        public static string Encode(string str)
        {
            var charClass = String.Format("0-9a-zA-Z{0}", Regex.Escape("-_.!~*'()"));
            return Regex.Replace(str,
                String.Format("[^{0}]", charClass),
                new MatchEvaluator(EncodeEvaluator));
        }

        public static string EncodeEvaluator(Match match)
        {
            return (match.Value == " ") ? "+" : String.Format("%{0:X2}", Convert.ToInt32(match.Value[0]));
        }

        public static string DecodeEvaluator(Match match)
        {
            return Convert.ToChar(int.Parse(match.Value.Substring(1), System.Globalization.NumberStyles.HexNumber)).ToString();
        }

        public static string Decode(string str)
        {
            return Regex.Replace(str.Replace('+', ' '), "%[0-9a-zA-Z][0-9a-zA-Z]", new MatchEvaluator(DecodeEvaluator));
        }

        public static List<Uri> ConvertListStringToListUri(List<string> UrlList)
        {
            List<Uri> Urls = new List<Uri>();

            foreach (string Url in UrlList)
            {
                Uri _url = new Uri(Url);
                Urls.Add(_url);
                _url = null;
            }

            return Urls;
        }
    }
}