using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
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

            BuildWebHost(args).Run();
        }

        static void LoadState()
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
                var listener = new UdpClient(new IPEndPoint(IPAddress.Any, 12060));

                while (true)
                {
                    IPEndPoint receivedFrom = new IPEndPoint(IPAddress.Any, 0);
                    byte[] msg = listener.Receive(ref receivedFrom);

                    Task.Factory.StartNew(() =>
                    {
                        try
                        {
                            ProcessDatagram(msg);
                        }
                        catch (Exception ex)
                        {
                            if (Debugger.IsAttached)
                            {
                                Debugger.Break();
                            }

                            Log("Uncaught exception: {0}", ex);
                        }
                    });
                }
            }
        }

        static void ProcessDatagram(byte[] msg)
        {
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
                    return;
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
                        Log("Received garbage: {0}", str.Truncate());
                    }
                }
                else
                {
                    Log("Received whitespace");
                }
            }
        }

        static string GetRootElementName(string possibleXml)
        {
            try
            {
                using (var stringReader = new StringReader(possibleXml))
                using (XmlReader reader = XmlReader.Create(possibleXml))
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

        static ContactRow Map(ContactBase cb)
        {
            return new ContactRow
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

        static void ProcessContactDelete(ContactDelete cd)
        {
            var contactRepo = new ContactRepo();

            if (!DateTime.TryParse(cd.Timestamp, out DateTime dt))
            {
                Log($"Invalid DateTime {dt}, failed to delete (Call={cd.Call}, Contestnr={cd.Contestnr}, StationName={cd.StationName}");
                return;
            }

            foreach (ContactRow c in contactRepo.GetList(dt, cd.Call, cd.Contestnr, cd.StationName).ToArray())
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

    

    
}