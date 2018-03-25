using DapperExtensions;
using Dapper;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using n1mm_leaderboard_shared;

namespace n1mm_leaderboard_shared
{
    public class ContactDbRepo
    {
        static object lockObj = new object();

        public ContactDbRepo(string pathToDb = null)
        {
            lock (lockObj)
            {
                SchemaMgr.Init(pathToDb ?? Environment.GetEnvironmentVariable("dashdb"));
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

        public List<LeaderboardRow> GetTotalQsoLeaderboard()
        {
            return GetLeaderboard(@"select 'Total' as operator, sum(1) as count from contacts
union select operator, count(1) as count from contacts group by operator order by count desc, operator");
        }

        public List<LeaderboardRow> GetPointsLeaderboard()
        {
            return GetLeaderboard(@"select 0 ord, 'Total' operator, sum(points) * (select count(1) from contacts where ismultiplier1=1) count from contacts
union select 1 ord, operator, sum(points) * (select count(1) from contacts c1 where ismultiplier1=1) from contacts group by operator
order by ord, count desc");
        }

        public List<LeaderboardRow> GetBandStats()
        {
            return GetLeaderboard(@"select 'Total' as Operator, sum(1) as Count, 0 ord from contacts union
select case when band=3.5 then '80m'
                      when band=7 then '40m' 
					  when band=14 then '20m' 
					  when band=21 then '15m'
					  when band=28 then '10m'
					  when band=50 then '6m'
					  else band end band,
                 count(1), 1 ord from contacts group by band order by ord asc, band;");
        }

        public List<LeaderboardRow> GetSeatStatus()
        {
            return GetLeaderboard(@"with stations (stationName) as (
  select distinct stationName from contacts),
stationFreqs (operator, stationName, band, timestampUtc) as (
  select (select operator from contacts c where c.stationName=stations.stationName order by timestamputc desc limit 1) band,
        stationName, 
       (select band from contacts c where c.stationName=stations.stationName order by timestamputc desc limit 1) band,
	   (select timestampUtc from contacts c where c.stationName=stations.stationName order by timestamputc desc limit 1) timestampUtc
  from stations),
stationBandsTS (operator, stationName, band, timestampUtc) as (
  select operator, stationName, case
    when band=3.5 then '80m'
    when band=7 then '40m'
    when band=14 then '20m'
    when band=21 then '15m'
    when band=28 then '10m'
    when band=50 then '6m'
  else cast(band as text) end band,
  timestampUtc 
  from stationFreqs)
select operator || ' @ ' || stationName as operator, cast(band as text) || ' @ ' || time(timestampUtc) as count from stationBandsTS");
        }

        public List<LeaderboardRow> GetLeaderboard(string sql)
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

        public List<LeaderboardRow> GetIsMulti1Leaderboard()
        {
            return GetLeaderboard(@"select 'Total' as operator, sum(1) as count from contacts where ismultiplier1 = 1 union
select operator, count(1) as count from contacts where ismultiplier1 = 1 group by operator order by count desc, operator");
        }

        public List<LeaderboardRow> GetQsoRateLeaderboard(int mins)
        {
            return GetLeaderboard($@"select operator, count(1) * {60 / mins} as count from contacts 
where timestamputc > datetime('now', '-{mins} minutes')
and timestamputc <= datetime('now')
group by operator
order by count desc, operator");
        }

        public List<LeaderboardRow> GetSinceLastQsoLeaderboard()
        {
            var lb = GetLeaderboard("select operator, (julianday('now') - julianday(max(timestampUTC)))*24 count from contacts group by operator order by count asc");

            foreach (var row in lb)
            {
                var ts = TimeSpan.FromHours(double.Parse(row.Count));
                row.Count = String.Format("{0:0}:{1:00}:{2:00}", Math.Floor(ts.TotalHours), ts.Minutes, ts.Seconds);
            }

            return lb;
        }

        const int peakCalculationWindowLengthInSecs = 1;
        const int peakLengthMins = 5;
        public List<LeaderboardRow> GetPeakRateLeaderboard()
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
                .Select(kvp => new LeaderboardRow { Operator = kvp.Key, Count = kvp.Value.ToString() })
                .OrderByDescending(lb => int.Parse(lb.Count))
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

        public IEnumerable<ContactDbRow> GetList(DateTime contactTime, string stationName)
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
