using System.Collections.Concurrent;

namespace SQ7MRU.Utils
{
    public class CallAndQTH
    {
        public string CallSign { get; set; }
        public string QTH { get; set; }
        public string HamID { get; set; }
        public string Adif { get; set; } 
        public ConcurrentDictionary<AdifRow, string> QSOs { get; set; }

        public CallAndQTH()
        {
            QSOs = new ConcurrentDictionary<AdifRow, string>();
        }
    }
}