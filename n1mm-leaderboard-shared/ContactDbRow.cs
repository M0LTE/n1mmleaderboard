using DapperExtensions.Mapper;
using System;

namespace n1mm_leaderboard_shared
{
    public class ContactMapper : ClassMapper<ContactDbRow>
    {
        public ContactMapper()
        {
            Table("Contacts");
            Map(m => m.TimestampUtc_dt).Ignore();
            AutoMap();
        }
    }

    public class ContactDbRow
    {
        public int ID { get; set; }
        public string Operator { get; set; }

        /// <summary>
        /// This is for SQLite, get and set using TimestampUtc_dt instead
        /// </summary>
        public string TimestampUtc
        {
            get
            {
                return TimestampUtc_dt.ToString("yyyy-MM-dd HH:mm:ss");
            }
            set
            {
                if (DateTime.TryParse(value, out DateTime dt))
                {
                    TimestampUtc_dt = dt;
                }
                else
                {
                    TimestampUtc_dt = default(DateTime);
                }
            }
        }

        public DateTime TimestampUtc_dt { get; set; }
        public string Call { get; set; }
        public string StationName { get; set; }
        public int ContestNumber { get; set; }
        public bool IsMultiplier1 { get; set; }
        public bool IsMultiplier2 { get; set; }
        public bool IsMultiplier3 { get; set; }

        /// <summary>
        /// MHz
        /// </summary>
        public double Band { get; set; }
    }
}
