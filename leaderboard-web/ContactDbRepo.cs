using DapperExtensions;
using Dapper;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace n1mmlistener
{
    public class ContactDbRepo
    {
        static object lockObj = new object();

        public ContactDbRepo()
        {
            lock (lockObj)
            {
                SchemaMgr.Init();
            }
        }

        public void Add(ContactDbRow c)
        {
            lock (lockObj)
            {
                using (var conn = GetConn())
                {
                    conn.Insert(c);
                }
            }
        }

        internal List<LeaderboardRow> GetTotalQsoLeaderboard()
        {
            return GetLeaderboard("select operator, count(1) as count from contacts group by operator order by count desc, operator");
        }

        internal List<LeaderboardRow> GetLeaderboard(string sql)
        {
            lock (lockObj)
            {
                using (var conn = GetConn())
                {
                    var results = conn.Query<LeaderboardRow>(sql);

                    return results.ToList();
                }
            }
        }

        internal List<LeaderboardRow> GetIsMulti1Leaderboard()
        {
            return GetLeaderboard("select operator, count(1) as count from contacts where ismultiplier1 = 1 group by operator order by count desc, operator");
        }

        internal List<LeaderboardRow> GetQsoRateLeaderboard(int mins)
        {
            return GetLeaderboard($@"select operator, count(1) * {60 / mins} as count from contacts 
where timestamputc > datetime('now', '-{mins} minutes')
and timestamputc <= datetime('now')
group by operator
order by count desc, operator");
        }

        const int peakCalculationWindowLengthInSecs = 1;
        const int peakLengthMins = 5;
        internal List<LeaderboardRow> GetPeakRateLeaderboard()
        {
            var data = GetList();

            if (!data.Any())
            {
                return new List<LeaderboardRow>();
            }

            DateTime firstQso = data.Min(row => row.TimestampUtc_dt);
            DateTime lastQso = data.Max(row => row.TimestampUtc_dt);

            int secs = (int)(lastQso - firstQso).TotalSeconds;

            Dictionary<string, int> leaderboard = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            Dictionary<string, int> windowLeaderboard = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            for (int i = 0; i < secs; i += peakCalculationWindowLengthInSecs)
            {
                DateTime windowStart = firstQso.AddSeconds(i);
                DateTime windowEnd = windowStart.AddMinutes(peakLengthMins);

                IEnumerable<ContactDbRow> window = data.Where(row => row.TimestampUtc_dt >= windowStart
                                                                  && row.TimestampUtc_dt < windowEnd);

                windowLeaderboard.Clear();

                foreach (ContactDbRow row in window)
                {
                    if (!windowLeaderboard.ContainsKey(row.Operator))
                    {
                        windowLeaderboard.Add(row.Operator, 0);
                    }

                    windowLeaderboard[row.Operator]++;
                }

                foreach (var kvp in windowLeaderboard)
                {
                    if (!leaderboard.ContainsKey(kvp.Key))
                    {
                        leaderboard.Add(kvp.Key, 0);
                    }

                    if (windowLeaderboard[kvp.Key] > leaderboard[kvp.Key])
                    {
                        leaderboard[kvp.Key] = windowLeaderboard[kvp.Key];
                    }
                }
            }

            var result = leaderboard
                .Select(kvp => new LeaderboardRow { Operator = kvp.Key, Count = kvp.Value })
                .OrderByDescending(lb => lb.Count)
                .ThenBy(lb => lb.Operator)
                .ToList();

            return result;
        }

        public IEnumerable<ContactDbRow> GetList()
        {
            using (var conn = GetConn())
            {
                return conn.Query<ContactDbRow>("select id, operator, timestampUTC, call, stationName, contestNumber, IsMultiplier1, IsMultiplier2, IsMultiplier3, cast(band as real) Band from contacts;").ToArray();
            }
        }

        internal IEnumerable<ContactDbRow> GetList(DateTime contactTime, string stationName)
        {
            lock (lockObj)
            {
                using (var conn = GetConn())
                {
                    var pg = new PredicateGroup { Operator = GroupOperator.And, Predicates = new List<IPredicate>() };

                    pg.Predicates.Add(Predicates.Field<ContactDbRow>(f => f.TimestampUtc, Operator.Eq, contactTime.ToString("yyyy-MM-dd HH:mm:ss")));

                    pg.Predicates.Add(Predicates.Field<ContactDbRow>(f => f.StationName, Operator.Eq, stationName));

                    return conn.GetList<ContactDbRow>(pg).ToArray();
                }
            }
        }

        public void Delete(ContactDbRow c)
        {
            lock (lockObj)
            {
                using (var conn = GetConn())
                {
                    conn.Delete(c);
                }
            }
        }

        IDbConnection GetConn()
        {
            return SchemaMgr.GetConn();
        }
    }
}