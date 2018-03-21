using System;
using System.Collections.Generic;
using System.Text;

namespace n1mm_leaderboard_shared
{
    public static class Mappers
    {
        public static ContactDbRow Map(N1mmXmlContactBase cb)
        {
            return new ContactDbRow
            {
                Call = cb.Call,
                ContestNumber = cb.Contestnr,
                Operator = cb.Operator,
                StationName = cb.StationName,
                TimestampUtc_dt = DateTime.Parse(cb.Timestamp),
                Band = cb.Band,
                IsMultiplier1 = cb.Ismultiplier1 != 0,
                IsMultiplier2 = cb.Ismultiplier2 != 0,
                IsMultiplier3 = cb.Ismultiplier3 != 0,
            };
        }
    }
}
