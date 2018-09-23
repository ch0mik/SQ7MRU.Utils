using System;
using System.Reflection;
using System.Text;

namespace SQ7MRU.Utils
{
    public static class AdifHelper
    {
        public static string ExportAsADIF(AdifRow[] rows)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine(MakeTagValue("PROGRAMID", "SQ7MRU.Utils"));
            sb.AppendLine(MakeTagValue("ADIF_VER", "3.0.6"));
            sb.AppendLine(MakeTagValue("CREATED_TIMESTAMP", DateTime.UtcNow.ToString("yyyyMMdd HHmmss")));
            sb.AppendLine("<EOH>");

            foreach (AdifRow qso in rows)
            {
                StringBuilder sbRow = new StringBuilder();
                foreach (var pi in qso.GetType().GetRuntimeProperties())
                {
                    var v = pi.GetValue(qso, null) as string;
                    if (!string.IsNullOrEmpty(v))
                    {
                        sbRow.Append(MakeTagValue(pi.Name, v));
                    }
                }
                sb.AppendLine(sbRow.ToString());
            }

            return sb.ToString();
        }

        private static string MakeTagValue(string tag, string value)
        {
            return $"<{tag?.ToUpper()}:{value?.Length}>{value}";
        }
    }
}