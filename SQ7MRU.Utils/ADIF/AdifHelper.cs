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

        /// <summary>
        /// http://www.adif.org/308/ADIF_308_annotated.htm#Band_Enumeration
        /// </summary>
        /// <param name="freq"></param>
        /// <returns></returns>
        public static string FreqToBand(double freq)
        {
            if (freq > 0.1357 && freq < 0.1378)
            {
                return "2190m";
            }
            else if (freq > 0.472 && freq < 0.479)
            {
                return "630m";
            }
            else if (freq > 0.501 && freq < 0.504)
            {
                return "560m";
            }
            else if (freq > 1.8 && freq < 2.0)
            {
                return "160m";
            }
            else if (freq > 3.5 && freq < 4.0)
            {
                return "80m";
            }
            else if (freq > 5.06 && freq < 5.45)
            {
                return "60m";
            }
            else if (freq > 7.0 && freq < 7.3)
            {
                return "40m";
            }
            else if (freq > 10.1 && freq < 10.15)
            {
                return "30m";
            }
            else if (freq > 14.0 && freq < 14.35)
            {
                return "20m";
            }
            else if (freq > 18.068 && freq < 18.168)
            {
                return "17m";
            }
            else if (freq > 21.0 && freq < 21.45)
            {
                return "15m";
            }
            else if (freq > 24.890 && freq < 24.99)
            {
                return "12m";
            }
            else if (freq > 28.0 && freq < 29.7)
            {
                return "10m";
            }
            else if (freq > 50 && freq < 54)
            {
                return "6m";
            }
            else if (freq > 70 && freq < 71)
            {
                return "4m";
            }
            else if (freq > 144 && freq < 148)
            {
                return "2m";
            }
            else if (freq > 222 && freq < 225)
            {
                return "1.25m";
            }
            else if (freq > 420 && freq < 450)
            {
                return "70cm";
            }
            else if (freq > 902 && freq < 928)
            {
                return "33cm";
            }
            else if (freq > 1240 && freq < 1300)
            {
                return "23cm";
            }
            else if (freq > 2300 && freq < 2450)
            {
                return "13cm";
            }
            else if (freq > 3300 && freq < 3500)
            {
                return "9cm";
            }
            else if (freq > 5650 && freq < 5925)
            {
                return "6cm";
            }
            else if (freq > 10000 && freq < 10500)
            {
                return "3cm";
            }
            else if (freq > 24000 && freq < 24250)
            {
                return "1.25cm";
            }
            else if (freq > 47000 && freq < 47200)
            {
                return "6mm";
            }
            else if (freq > 75500 && freq < 81000)
            {
                return "4mm";
            }
            else if (freq > 119980 && freq < 120020)
            {
                return "2.5mm";
            }
            else if (freq > 142000 && freq < 149000)
            {
                return "2mm";
            }
            else if (freq > 241000 && freq < 250000)
            {
                return "1mm";
            }

            else
            {
                return null;
            }
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