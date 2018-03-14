using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using System;
using System.Diagnostics;
using System.IO;
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
            if (false)
            {
                MakeUpRandomData();
                Debugger.Break();
            }

            udpThread.Start();

            BuildWebHost(args).Run();
        }

        static void MakeUpRandomData()
        {
            var repo = new ContactDbRepo();
            foreach (var contact in repo.GetList())
            {
                repo.Delete(contact);
            }

            var calls = new[] { "M0LTE", "2E0XLX", "G4RDC", "2E0JPM", "2E1EPQ" };
            var bands = new[] { 21, 14, 7, 3.5, 1.8 };

            for (int i = 0; i < 10000; i++)
            {
                Debug.WriteLine(i);
                DateTime dt = DateTime.Now.AddDays(-1).AddSeconds(i * 10 + rnd.Next(0, 6));
                repo.Add(new ContactDbRow
                {
                    Band = bands[rnd.Next(0, bands.Length)],
                    Operator = calls[rnd.Next(0, calls.Length)],
                    Call = MakeUpCallsign(),
                    ContestNumber = 10,
                    IsMultiplier1 = rnd.Next(5) == 0,
                    StationName = "stn-" + (rnd.Next(2) + 1),
                    TimestampUtc_dt = dt
                });
            }
        }

        static Random rnd = new Random();

        static char GetRandomLetter()
        {
            char prefix1 = 'A';
            prefix1 += (char)rnd.Next(0, 26);
            return prefix1;
        }
        static string MakeUpCallsign()
        {
            var sb = new StringBuilder();

            sb.Append(GetRandomLetter());

            if (rnd.Next(3) == 0)
            {
                sb.Append(GetRandomLetter());
            }

            sb.Append(rnd.Next(0, 10));

            sb.Append(GetRandomLetter());
            sb.Append(GetRandomLetter());

            if (rnd.Next(10) != 0)
            {
                sb.Append(GetRandomLetter());
            }

            return sb.ToString();
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
            bool isAdd, isReplace, isDelete;
            isAdd = isReplace = isDelete = false;

            try
            {
                if (N1mmXmlContactInfo.TryParse(msg, out N1mmXmlContactInfo ci))
                {
                    isAdd = true;
                    ProcessContactAdd(ci);
                }
                else if (N1mmXmlContactReplace.TryParse(msg, out N1mmXmlContactReplace cr))
                {
                    isReplace = true;
                    ProcessContactReplace(cr);
                }
                else if (ContactDelete.TryParse(msg, out ContactDelete cd))
                {
                    isDelete = true;
                    ProcessContactDelete(cd);
                }
            }
            finally
            {
                try
                {
                    string rawFolder = Path.Combine(Environment.CurrentDirectory, "datagrams");
                    if (!Directory.Exists(rawFolder))
                    {
                        Directory.CreateDirectory(rawFolder);
                    }

                    string targetFolder;

                    if (isAdd)
                    {
                        targetFolder = Path.Combine(rawFolder, "ContactAdd");
                    }
                    else if (isReplace)
                    {
                        targetFolder = Path.Combine(rawFolder, "ContactReplace");
                    }
                    else if (isDelete)
                    {
                        targetFolder = Path.Combine(rawFolder, "ContactDelete");
                    }
                    else
                    {
                        targetFolder = rawFolder;
                    }

                    if (!Directory.Exists(targetFolder))
                    {
                        Directory.CreateDirectory(targetFolder);
                    }

                    string rawFile = Path.Combine(targetFolder, string.Format("{0:yyyyMMdd-HHmmss.fff}.xml", DateTime.Now));

                    File.WriteAllBytes(rawFile, msg);
                }
                catch (Exception ex)
                {
                    Log("Could not write datagram: {0}", ex);
                }
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
            if (ci.IsOriginal == "True") // True = contact was originated on this computer. Without this check, in a multiple n1mm scenario, 
            {                            // if this computer has All QSOs selected, we will receive the contact twice. We only want
                                         // the one from the PC on which the contact was logged. This does mean every n1mm instance
                                         // will need to be configured to send datagrams to us. That seems reasonable.

                var contactRepo = new ContactDbRepo();

                ContactDbRow row = Map(ci);

                contactRepo.Add(row);
            }
        }

        static void ProcessContactDelete(ContactDelete cd)
        {
            DeleteContact(cd.Timestamp, cd.StationName);
        }

        static void DeleteContact(string n1mmTimestamp, string stationName)
        {
            var contactRepo = new ContactDbRepo();

            // search for which contact to delete by station name and timestamp
            if (DateTime.TryParse(n1mmTimestamp, out DateTime ts))
            {
                if (ts != new DateTime(1900, 1, 1, 0, 0, 0)) // for some reason n1mm passes us this in some circumstances, no idea what we're supposed to do with it
                {
                    if (!string.IsNullOrWhiteSpace(stationName))
                    {
                        var contacts = contactRepo.GetList(contactTime: ts, stationName: stationName);

                        foreach (var contact in contacts)
                        {
                            contactRepo.Delete(contact);
                        }
                    }
                }
            }
        }

        static void ProcessContactReplace(N1mmXmlContactReplace cr)
        {
            if (cr.IsOriginal == "True") // True = contact was originated on this computer. Without this check, in a multiple n1mm scenario, 
            {                            // if this computer has All QSOs selected, we will receive the contact twice. We only want
                                         // the one from the PC on which the contact was logged. This does mean every n1mm instance
                                         // will need to be configured to send datagrams to us. That seems reasonable.

                var contactRepo = new ContactDbRepo();

                DeleteContact(cr.Timestamp, cr.StationName);

                ContactDbRow row = Map(cr);

                contactRepo.Add(row);
            }
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