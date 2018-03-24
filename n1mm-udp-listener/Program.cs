using Microsoft.Extensions.Configuration;
using n1mm_leaderboard_shared;
using System;
using System.IO;
using System.Net;
using System.Net.Sockets;

namespace n1mm_udp_listener
{
    class Program
    {
        static bool OnlyProcessOriginals;

        static string pathToDb;

        static void Main(string[] args)
        {
            if (args.Length != 1 || !Directory.Exists(Path.GetDirectoryName(args[0])))
            {
                Console.WriteLine("Expected a full path to the database file");
                return;
            }

            pathToDb = args[0];

            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json");

            var configuration = builder.Build();

            if (bool.TryParse(configuration["OnlyProcessOriginals"], out bool b))
            {
                OnlyProcessOriginals = b;
            }

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
                    //Console.WriteLine($"Adding a contact: {ci.Call}");
                    ProcessContactAdd(ci);
                }
                else if (N1mmXmlContactReplace.TryParse(msg, out N1mmXmlContactReplace cr))
                {
                    isReplace = true;
                    //Console.WriteLine($"Replacing a contact: {cr.Call}");
                    ProcessContactReplace(cr);
                }
                else if (ContactDelete.TryParse(msg, out ContactDelete cd))
                {
                    isDelete = true;
                    //Console.WriteLine($"Deleting a contact: {cd.Call}");
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

        static void Log(string format, params object[] args)
        {
            Console.WriteLine(format, args);
        }

        static void ProcessContactAdd(N1mmXmlContactInfo ci)
        {
            if (!OnlyProcessOriginals || (OnlyProcessOriginals && ci.IsOriginal == "True"))
            {                            // True = contact was originated on this computer. Without this check, in a multiple n1mm scenario, 
                                         // if this computer has All QSOs selected, we will receive the contact twice. We only want
                                         // the one from the PC on which the contact was logged. This does mean every n1mm instance
                                         // will need to be configured to send datagrams to us. That seems reasonable.

                Console.WriteLine($"Adding a contact: {ci.Call}  / {ci.Operator}");

                var contactRepo = new ContactDbRepo(pathToDb);

                ContactDbRow row = Mappers.Map(ci);

                contactRepo.Add(row);
            }
            else
            {
                Console.WriteLine($"Skipping a non-original contact: {ci.Call} / {ci.Operator}");
            }
        }

        static void ProcessContactDelete(ContactDelete cd)
        {
            Console.WriteLine($"Deleting a contact: {cd.Call}");

            DeleteContact(cd.Timestamp, cd.StationName);
        }

        static void DeleteContact(string n1mmTimestamp, string stationName)
        {
            var contactRepo = new ContactDbRepo(pathToDb);

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

                Console.WriteLine($"Replacing a contact: {cr.Call} / {cr.Operator}");

                var contactRepo = new ContactDbRepo(pathToDb);

                DeleteContact(cr.Timestamp, cr.StationName);

                ContactDbRow row = Mappers.Map(cr);

                contactRepo.Add(row);
            }
            else
            {
                Console.WriteLine($"Skipping a replace (non-original): {cr.Call} / {cr.Operator}");
            }
        }
    }
}
