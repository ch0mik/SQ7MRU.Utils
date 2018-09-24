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
                sb.AppendLine(ConvertToString(qso));
            }

            return sb.ToString();
        }

        private static string MakeTagValue(string tag, string value)
        {
            return $"<{tag?.ToUpper()}:{value?.Length}>{value} ";
        }

        public static string ConvertToString(AdifRow row)
        {
            StringBuilder sbRow = new StringBuilder();
            foreach (var pi in row.GetType().GetRuntimeProperties())
            {
                var v = pi.GetValue(row, null) as string;
                if (!string.IsNullOrEmpty(v))
                {
                    sbRow.Append(MakeTagValue(pi.Name, v));
                }
            }

            return $"{sbRow.ToString()}<EOR>";
        }

        public static AdifRow FixRecord(AdifRow row)
        {
            //ToDo 
            //ENUM Parse
            //HRD Log - wrong Mode 

                row.BAND = row.BAND.ToLower();
                row.CALL = row.CALL.ToUpper();

            return row;
        }
    }
}