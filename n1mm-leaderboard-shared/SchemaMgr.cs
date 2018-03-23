using Dapper;
using DapperExtensions.Sql;
using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;

namespace n1mm_leaderboard_shared
{
    public static class SchemaMgr
    {
        static string connectionString;
        const string meta = "meta";
        public static bool DoesTableExist(string tname)
        {
            using (var conn = GetConn())
            {
                return conn.ExecuteScalar<int?>("SELECT count(*) FROM sqlite_master WHERE type = 'table' AND name = @n", new { n = tname }) == 1;
            }
        }

        public static IDbConnection GetConn()
        {
            var conn = new SqliteConnection(connectionString);
            conn.Open();
            return conn;
        }
        internal static void Init(string dbFile)
        {
            DapperExtensions.DapperExtensions.SqlDialect = new SqliteDialect();

            //string dbFile = Path.Combine(Environment.CurrentDirectory, "n1mmlistener.db");
            var csb = new SqliteConnectionStringBuilder();
            csb.DataSource = dbFile;
            connectionString = csb.ToString();

            if (!DoesTableExist(meta))
            {
                InitDb();
            }

            UpgradeDb();
        }

        static void InitDb()
        {
            using (var conn = GetConn())
            {
                conn.Execute("create table meta (infokey text primary key, value text);");
                conn.Execute("insert into meta (infokey,value) values ('schemaver', '0')");
            }
        }

        static void UpgradeDb()
        {
            int ver = GetSchemaVer();
            if (ver < UpgradeStatements.Max(kvp => kvp.Key))
            {
                using (var conn = GetConn())
                {
                    foreach (var kvp in UpgradeStatements.OrderBy(kvp => kvp.Key))
                    {
                        int statementVer = kvp.Key;
                        string statement = kvp.Value;

                        if (ver < statementVer)
                        {
                            conn.Execute(statement);
                            conn.Execute("update meta set value=@v where infokey='schemaver';", new { v = statementVer });
                            ver = statementVer;
                        }
                    }
                }
            }
        }

        static Dictionary<int, string> UpgradeStatements = new Dictionary<int, string> {
            { 1, @"create table contacts (
                     id integer primary key autoincrement, 
                     operator text,
                     timestampUTC text,
                     call text,
                     stationName text,
                     contestNumber int
                   );" },
            { 2, @"alter table contacts add column IsMultiplier1 integer;
                   alter table contacts add column IsMultiplier2 integer;
                   alter table contacts add column IsMultiplier3 integer;
                   alter table contacts add column Band integer;" }
        };

        static int GetSchemaVer()
        {
            using (var conn = GetConn())
            {
                return int.Parse(conn.ExecuteScalar<string>("select value from meta where infokey='schemaver';"));
            }
        }
    }
}
