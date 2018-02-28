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

        public IEnumerable<ContactDbRow> GetList()
        {
            using (var conn = GetConn())
            {
                return conn.GetList<ContactDbRow>().ToArray();
            }
        }

        public IEnumerable<ContactDbRow> GetList(string call, int contestNumber, string stationName, DateTime? timestampUtc = null)
        {
            lock (lockObj)
            {
                using (var conn = GetConn())
                {
                    var pg = new PredicateGroup { Operator = GroupOperator.And, Predicates = new List<IPredicate>() };

                    if (timestampUtc != null)
                    {
                        pg.Predicates.Add(Predicates.Field<ContactDbRow>(f => f.TimestampUtc, Operator.Eq, timestampUtc.Value.ToString("yyyy-MM-dd HH:mm:ss")));
                    }

                    if (call != null)
                    {
                        pg.Predicates.Add(Predicates.Field<ContactDbRow>(f => f.Call, Operator.Eq, call));
                    }

                    pg.Predicates.Add(Predicates.Field<ContactDbRow>(f => f.ContestNumber, Operator.Eq, contestNumber));

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