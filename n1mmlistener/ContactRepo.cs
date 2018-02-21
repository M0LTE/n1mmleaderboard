using DapperExtensions;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace n1mmlistener
{
    public class ContactRepo
    {
        public ContactRepo()
        {
            SchemaMgr.Init();
        }

        public void Add(ContactRow c)
        {
            using (var conn = GetConn())
            {
                conn.Insert(c);
            }
        }

        public IEnumerable<ContactRow> GetList()
        {
            using (var conn = GetConn())
            {
                return conn.GetList<ContactRow>().ToArray();
            }
        }

        public IEnumerable<ContactRow> GetList(DateTime timestampUtc, string call, int contestNumber, string stationName)
        {
            using (var conn = GetConn())
            {
                var pg = new PredicateGroup { Operator = GroupOperator.And, Predicates = new List<IPredicate>() };
                pg.Predicates.Add(Predicates.Field<ContactRow>(f => f.TimestampUtc, Operator.Eq, timestampUtc.ToString("yyyy-MM-dd HH:mm:ss")));

                if (call != null)
                {
                    pg.Predicates.Add(Predicates.Field<ContactRow>(f => f.Call, Operator.Eq, call));
                }

                pg.Predicates.Add(Predicates.Field<ContactRow>(f => f.ContestNumber, Operator.Eq, contestNumber));
                pg.Predicates.Add(Predicates.Field<ContactRow>(f => f.StationName, Operator.Eq, stationName));
                return conn.GetList<ContactRow>(pg);
            }
        }

        public void Delete(ContactRow c)
        {
            using (var conn = GetConn())
            {
                conn.Delete(c);
            }
        }

        IDbConnection GetConn()
        {
            return SchemaMgr.GetConn();
        }
    }
}
