using Dapper;
using DapperExtensions;
using DapperExtensions.Mapper;
using DapperExtensions.Sql;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Xml;

namespace n1mmlistener
{
    public class Program
    {
        static Thread udpThread = new Thread(new ThreadStart(UdpThread));

        public static List<LeaderboardRow> State = new List<LeaderboardRow>();
        static LeaderboardRow GetRow(string op)
        {
            if (op == null)
                throw new ArgumentNullException();

            op = op.ToUpper().Trim();

            var row = State.SingleOrDefault(r => String.Equals(r.Op, op));

            if (row == null)
            {
                row = new LeaderboardRow { Op = op };
                State.Add(row);
            }

            return row;
        }

        public static void Main(string[] args)
        {
            LoadState();
            udpThread.Start();
            //UdpThread();

            BuildWebHost(args).Run();
        }



        private static void LoadState()
        {
            ContactRepo repo = new ContactRepo();

            var contacts = repo.GetList();

            foreach (var contact in contacts)
            {
                LeaderboardRow row = GetRow(contact.Operator);

                row.TotalQsos++;
            }
        }

        static void UdpThread()
        {
            while (true)
            {
                try
                {
                    var listener = new UdpClient(new IPEndPoint(IPAddress.Any, 12060));

                    while (true)
                    {
                        IPEndPoint receivedFrom = new IPEndPoint(IPAddress.Any, 0);
                        byte[] msg = listener.Receive(ref receivedFrom);

                        if (ContactInfo.TryParse(msg, out ContactInfo ci))
                        {
                            ProcessContactInfo(ci);
                        }
                        else if (ContactReplace.TryParse(msg, out ContactReplace cr))
                        {
                            ProcessContactReplace(cr);
                        }
                        else if (ContactDelete.TryParse(msg, out ContactDelete cd))
                        {
                            ProcessContactDelete(cd);
                        }
                        else
                        {
                            string str;
                            try
                            {
                                str = Encoding.UTF8.GetString(msg);
                            }
                            catch (Exception)
                            {
                                Log("Bad datagram, not UTF8: {0}", msg.ToHexBytes());
                                continue;
                            }

                            if (!string.IsNullOrWhiteSpace(str))
                            {
                                string rename = GetRootElementName(str);
                                if (!string.IsNullOrWhiteSpace(rename))
                                {
                                    Log("Not a known datagram: {0}", rename);
                                }
                                else
                                {
                                    Log("Received garbage: {0}", Truncate(str));
                                }
                            }
                            else
                            {
                                Log("Received whitespace");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    if (Debugger.IsAttached)
                    {
                        Debugger.Break();
                    }

                    Log("Uncaught exception: {0}", ex);
                    Thread.Sleep(1000);
                }
            }
        }

        static string Truncate(string str)
        {
            if (str.Length < 100)
            {
                return str;
            }

            return str.Substring(0, 97) + "...";
        }

        private static string GetRootElementName(string str)
        {
            /*
<?xml version="1.0" encoding="utf-8"?>
<contactreplace> 
            */

            try
            {
                using (var stringReader = new StringReader(str))
                using (XmlReader reader = XmlReader.Create(str))
                {
                    while (reader.Read())
                    {
                        // first element is the root element
                        if (reader.NodeType == XmlNodeType.Element)
                        {
                            return reader.Name;
                        }
                    }
                }
            }
            catch (Exception)
            {
                return null;
            }

            return null;
        }

        static void ProcessContactInfo(ContactInfo ci)
        {
            var contactRepo = new ContactRepo();

            contactRepo.Add(Map(ci));

            var opRow = State.SingleOrDefault(row => String.Equals(row.Op, ci.Operator.Trim(), StringComparison.OrdinalIgnoreCase));
            if (opRow == null)
            {
                opRow = new LeaderboardRow { Op = ci.Operator.Trim().ToUpper() };
                State.Add(opRow);
            }

            opRow.TotalQsos++;
        }

        static Contact Map(ContactBase cb)
        {
            return new Contact
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

        private static void ProcessContactDelete(ContactDelete cd)
        {
            var contactRepo = new ContactRepo();

            if (!DateTime.TryParse(cd.Timestamp, out DateTime dt))
            {
                Log($"Invalid DateTime {dt}, failed to delete (Call={cd.Call}, Contestnr={cd.Contestnr}, StationName={cd.StationName}");
                return;
            }

            foreach (Contact c in contactRepo.GetList(dt, cd.Call, cd.Contestnr, cd.StationName).ToArray())
            {
                string op = c.Operator;
                contactRepo.Delete(c);
                GetRow(op).TotalQsos--;
            }
        }

        private static void ProcessContactReplace(ContactReplace cr)
        {
            var contactRepo = new ContactRepo();

            if (DateTime.TryParse(cr.Timestamp, out DateTime contactTime))
            {
                foreach (var contact in contactRepo.GetList(contactTime, null, cr.Contestnr, cr.StationName))
                {
                    contactRepo.Delete(contact);
                }
            }

            contactRepo.Add(Map(cr));

            GetRow(cr.Operator).TotalQsos++;
        }

        internal static void Log(string format, params object[] args)
        {
            Console.WriteLine(format, args);
        }

        public static IWebHost BuildWebHost(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .UseStartup<Startup>()
                .Build();
    }

    public class ContactRepo
    {
        const string meta = "meta";

        public ContactRepo()
        {
            DapperExtensions.DapperExtensions.SqlDialect = new SqliteDialect();

            string dbFile = Path.Combine(Environment.CurrentDirectory, "n1mmlistener.db");
            var csb = new SqliteConnectionStringBuilder();
            csb.DataSource = dbFile;
            connectionString = csb.ToString();

            if (!DoesTableExist(meta))
            {
                InitDb();
            }

            UpgradeDb();
        }

        public void Add(Contact c)
        {
            using (var conn = GetConn())
            {
                conn.Insert(c);
            }
        }

        public IEnumerable<Contact> GetList()
        {
            using (var conn = GetConn())
            {
                return conn.GetList<Contact>().ToArray();
            }
        }

        public IEnumerable<Contact> GetList(DateTime timestampUtc, string call, int contestNumber, string stationName)
        {
            using (var conn = GetConn())
            {
                var pg = new PredicateGroup { Operator = GroupOperator.And, Predicates = new List<IPredicate>() };
                pg.Predicates.Add(Predicates.Field<Contact>(f => f.TimestampUtc, Operator.Eq, timestampUtc.ToString("yyyy-MM-dd HH:mm:ss")));

                if (call != null)
                {
                    pg.Predicates.Add(Predicates.Field<Contact>(f => f.Call, Operator.Eq, call));
                }

                pg.Predicates.Add(Predicates.Field<Contact>(f => f.ContestNumber, Operator.Eq, contestNumber));
                pg.Predicates.Add(Predicates.Field<Contact>(f => f.StationName, Operator.Eq, stationName));
                return conn.GetList<Contact>(pg);
            }
        }

        public void Delete(Contact c)
        {
            using (var conn = GetConn())
            {
                conn.Delete(c);
            }
        }

        Dictionary<int, string> UpgradeStatements = new Dictionary<int, string> {
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

        int GetSchemaVer()
        {
            using (var conn = GetConn())
            {
                return int.Parse(conn.ExecuteScalar<string>("select value from meta where infokey='schemaver';"));
            }
        }

        void InitDb()
        {
            using (var conn = GetConn())
            {
                conn.Execute("create table meta (infokey text primary key, value text);");
                conn.Execute("insert into meta (infokey,value) values ('schemaver', '0')");
            }
        }

        private void UpgradeDb()
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

        string connectionString;

        bool DoesTableExist(string tname)
        {
            using (var conn = GetConn())
            {
                return conn.ExecuteScalar<int?>("SELECT count(*) FROM sqlite_master WHERE type = 'table' AND name = @n", new { n = tname }) == 1;
            }
        }

        IDbConnection GetConn()
        {
            var conn = new SqliteConnection(connectionString);
            conn.Open();
            return conn;
        }
    }

    public class Contact
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

    public class ContactMapper : ClassMapper<Contact>
    {
        public ContactMapper()
        {
            Table("Contacts");
            Map(m => m.TimestampUtc_dt).Ignore();
            AutoMap();
        }
    }

    static class Extensions
    {
        public static string ToHexBytes(this byte[] arr)
        {
            return String.Join(" ", arr.Select(b => String.Format("{0:X2}", b)));
        }
    }
}