using System;

namespace n1mmlistener
{
    public class ContactRow
    {
        public int ID { get; set; }
        public string Operator { get; set; }

        /// <summary>
        /// This is for Dapper, get and set using TimestampUtc_dt instead
        /// </summary>
        public string TimestampUtc { get; set; }

        public DateTime TimestampUtc_dt
        {
            get
            {
                if (DateTime.TryParse(TimestampUtc, out DateTime result))
                {
                    return result;
                }
                return default(DateTime);
            }
            set
            {
                TimestampUtc = value.ToString("yyyy-MM-dd HH:mm:ss");
            }
        }
        public string Call { get; set; }
        public string StationName { get; set; }
        public int ContestNumber { get; set; }
        public bool IsMultiplier1 { get; set; }
        public bool IsMultiplier2 { get; set; }
        public bool IsMultiplier3 { get; set; }
        public int Band { get; set; }
    }
}
