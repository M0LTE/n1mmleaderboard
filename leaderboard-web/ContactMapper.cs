using DapperExtensions.Mapper;
using n1mm_leaderboard_shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace n1mmlistener
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
}
