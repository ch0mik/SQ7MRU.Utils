using System;
using System.Reflection;
using System.Text;

namespace SQ7MRU.Utils
{
    /// <summary>Exports the string-backed ADIF row model and provides legacy band/case normalization helpers.</summary>
    /// <seealso href="https://www.adif.org/100/adif_100.htm">Historical ADIF 1.0 specification describing length-prefixed fields and record markers.</seealso>
    public static class AdifHelper
    {
        /// <summary>Serializes rows with an ADIF header, program identifier, UTC creation timestamp and EOR-delimited records.</summary>
        /// <param name="rows">Non-null array of non-null records; an empty array produces a header-only export.</param>
        /// <returns>ADIF text with the implemented format-version header and only nonempty string fields.</returns>
        /// <remarks>Uses reflection and .NET string lengths, not encoded byte counts. Field order follows runtime property enumeration and is not a stable sorting contract. This method does not upload, validate records or change their values.</remarks>
        /// <remarks>The exporter declares ADIF_VER 3.0.6 and begins with a tagged header. It does not produce the strict ADIF 1.0 header convention, which requires a non-angle-bracket first character when a header is present.</remarks>
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

        /// <summary>Serializes one record's nonempty public string properties and appends an EOR marker.</summary>
        /// <param name="row">Non-null string-backed QSO record to serialize.</param>
        /// <returns>One ADIF record without a file header, including its terminating EOR.</returns>
        /// <remarks>Null/empty values are omitted. Values are not escaped because lengths delimit them; lengths count .NET string characters. No callsign, numeric, date or enum validation is performed.</remarks>
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

        /// <summary>Maps a frequency in MHz to the implemented amateur-radio ADIF band label.</summary>
        /// <param name="freq">Frequency in megahertz, not hertz or kilohertz.</param>
        /// <returns>A band label such as 20m, or null if no implemented interval matches.</returns>
        /// <remarks>Legacy intervals use strict inequalities: exact endpoints are excluded. This is a fixed mapping, not a current jurisdiction-specific frequency-allocation check.</remarks>
        public static string? FreqToBand(double freq)
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

        /// <summary>Normalizes only CALL to uppercase and BAND to lowercase using invariant casing.</summary>
        /// <param name="row">Non-null record mutated in place; null field values remain null.</param>
        /// <returns>The same record instance after case normalization.</returns>
        /// <remarks>Does not trim values, infer bands, normalize modes or validate other fields. Copy a record first if the original casing must be retained.</remarks>
        public static AdifRow FixRecord(AdifRow row)
        {
            //ToDo 
            //ENUM Parse
            //HRD Log - wrong Mode 

                row.BAND = row.BAND?.ToLowerInvariant();
                row.CALL = row.CALL?.ToUpperInvariant();

            return row;
        }
    }
}
