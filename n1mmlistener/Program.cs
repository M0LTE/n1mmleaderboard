using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using System;
using System.Collections.Generic;
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

        public static void Main(string[] args)
        {
            udpThread.Start();

            BuildWebHost(args).Run();
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

                    try
                    {
                        ProcessDatagram(msg);
                    }
                    catch (Exception ex)
                    {
                        Log("Uncaught exception: {0}", ex);
                    }
                }
            }
        }

        static void ProcessDatagram(byte[] msg)
        {
            try
            {
                string rawFolder = Path.Combine(Environment.CurrentDirectory, "datagrams");
                if (!Directory.Exists(rawFolder))
                {
                    Directory.CreateDirectory(rawFolder);
                }
                string rawFile = Path.Combine(rawFolder, string.Format("{0:yyyyMMdd-HHmmss.fff}.xml", DateTime.Now));
                File.WriteAllBytes(rawFile, msg);
            }
            catch (Exception ex)
            {
                Log("Could not write datagram: {0}", ex);
            }

            if (N1mmXmlContactInfo.TryParse(msg, out N1mmXmlContactInfo ci))
            {
                ProcessContactAdd(ci);
            }
            else if (N1mmXmlContactReplace.TryParse(msg, out N1mmXmlContactReplace cr))
            {
                ProcessContactReplace(cr);
            }
            else if (ContactDelete.TryParse(msg, out ContactDelete cd))
            {
                ProcessContactDelete(cd);
            }
            /*else
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
            }*/
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

        static ContactDbRow Map(N1mmXmlContactBase cb)
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

        static void ProcessContactAdd(N1mmXmlContactInfo ci)
        {
            var contactRepo = new ContactDbRepo();

            ContactDbRow row = Map(ci);

            contactRepo.Add(row);
        }

        static void ProcessContactDelete(ContactDelete cd)
        {
            var contactRepo = new ContactDbRepo();

            if (!DateTime.TryParse(cd.Timestamp, out DateTime dt))
            {
                Log($"Invalid DateTime {dt}, failed to delete (Call={cd.Call}, Contestnr={cd.Contestnr}, StationName={cd.StationName}");
                return;
            }

            IEnumerable<ContactDbRow> search;
            if (dt != new DateTime(1900, 1, 1, 0, 0, 0))
            {
                search = contactRepo.GetList(cd.Call, cd.Contestnr, cd.StationName, dt);
            }
            else
            {
                search = contactRepo.GetList(cd.Call, cd.Contestnr, cd.StationName, null);
            }

            foreach (ContactDbRow c in search.ToArray())
            {
                string op = c.Operator;
                contactRepo.Delete(c);
            }
        }

        static void ProcessContactReplace(N1mmXmlContactReplace cr)
        {
            var contactRepo = new ContactDbRepo();

            throw new NotImplementedException();

            ContactDbRow row = Map(cr);

            contactRepo.Add(row);
        }

        internal static void Log(string format, params object[] args)
        {
            lock (logLockObj)
            {
                Console.WriteLine(format, args);
                File.AppendAllText("n1mmlistener.log", String.Concat(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff "), String.Format(format, args), Environment.NewLine));
            }
        }

        static object logLockObj = new object();

        public static IWebHost BuildWebHost(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .UseStartup<Startup>()
                .Build();
    }
}