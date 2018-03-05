using NDesk.Options;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Reflection;

namespace n1mmsender
{
    class Program
    {
        static int Main(string[] args)
        {
            string ip = null;
            int port = 12060;
            bool help = false;
            bool listDatasets = false;
            Contest? contest = null;

            var p = new OptionSet() {
                { "i|ip=",      v => ip = v },
                { "p|port=",    v => {if (int.TryParse(v, out int t)) { port = t; } } },
                { "h|?|help",   v => help = v != null },
                { "l|list-datasets",   v => listDatasets = v != null },
                { "d|dataset=",   v => { if (Enum.TryParse<Contest>(v, out Contest c)) { contest = c; } } },
            };
            List<string> extra = p.Parse(args);

            if (extra.Any())
            {
                Console.WriteLine("Unrecognised parameter(s) {0}", String.Join(", ", extra));
                Console.WriteLine("Use --help for full syntax");
                return -1;
            }

            if (listDatasets)
            {
                foreach (var item in Enum.GetNames(typeof(Contest)))
                {
                    Console.WriteLine(item);
                }
                return 0;
            }

            if (help)
            {
                Console.WriteLine(@"

-i= | --ip=            IP to send N1MM+ datagrams to, or 'broadcast'
-p= | --port=          Port to send datagrams to, default 12060
-l  | --list-datasets  List the available embedded datasets
-d= | --dataset=       The dataset to send
-h  | --help           Show this text
");
                return 0;
            }

            if (contest == null)
            {
                Console.WriteLine("Missing or invalid dataset. Use --list-datasets to find a dataset or --help for full syntax.");
                Console.WriteLine("NB dataset names are case sensitive.");
                return -1;
            }

            if (port <= 0 || port > 65535)
            {
                Console.WriteLine("Invalid port");
                return -1;
            }

            IPAddress ipaddr;
            if (ip == "broadcast")
            {
                UnicastIPAddressInformation nip = GetNicIP();
                ipaddr = GetBroadcastAddress(nip);
            }
            else if (!IPAddress.TryParse(ip, out ipaddr))
            {
                Console.WriteLine("Invalid IP, specify with --ip=[ip address]");
                return -1;
            }

            var assembly = Assembly.GetExecutingAssembly();

            // n1mmsender.SampleData.GB2GP.ARRL_DX_SSB_2018.ContactAdd.20180303-101910.321.xml

            string prefix = datasets[contest.Value];

            IEnumerable<string> filteredResourceNames = from rn in assembly.GetManifestResourceNames()
                                                        where rn.StartsWith(prefix)
                                                        select rn.Substring(prefix.Length + 1);

            var parsedResourceNames = from rn in filteredResourceNames
                                      select new
                                      {
                                          FullResourceName = rn,
                                          Timestamp = DateTime.ParseExact(rn.Split('.')[1] + "." + rn.Split('.')[2], "yyyyMMdd-HHmmss.fff", CultureInfo.InvariantCulture)
                                      };

            var sorted = from r in parsedResourceNames
                         orderby r.Timestamp
                         select r;

            var ipep = new IPEndPoint(ipaddr, port);

            using (var client = new UdpClient(AddressFamily.InterNetwork))
            {
                foreach (var item in sorted)
                {
                    string frn = prefix + "." + item.FullResourceName;
                    using (Stream stream = assembly.GetManifestResourceStream(frn))
                    using (var ms = new MemoryStream())
                    {
                        stream.CopyTo(ms);
                        byte[] buf = ms.ToArray();
                        client.Send(buf, buf.Length, ipep);
                    }
                }
            }

            return 0;
        }

        public static UnicastIPAddressInformation GetNicIP()
        {
            foreach (NetworkInterface ni in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (ni.NetworkInterfaceType == NetworkInterfaceType.Wireless80211 || ni.NetworkInterfaceType == NetworkInterfaceType.Ethernet)
                {
                    foreach (UnicastIPAddressInformation uipi in ni.GetIPProperties().UnicastAddresses)
                    {
                        //IPInterfaceProperties adapterProperties = ni.GetIPProperties();
                        //IPv4InterfaceProperties p = adapterProperties.GetIPv4Properties();
                        if (uipi.Address.AddressFamily == AddressFamily.InterNetwork && !uipi.Address.ToString().StartsWith("169.254."))
                        {
                            return uipi;
                        }
                    }
                }
            }

            return default(UnicastIPAddressInformation);
        }

        public static IPAddress GetBroadcastAddress(UnicastIPAddressInformation unicastAddress)
        {
            return GetBroadcastAddress(unicastAddress.Address, unicastAddress.IPv4Mask);
        }

        public static IPAddress GetBroadcastAddress(IPAddress address, IPAddress mask)
        {
            uint ipAddress = BitConverter.ToUInt32(address.GetAddressBytes(), 0);
            uint ipMaskV4 = BitConverter.ToUInt32(mask.GetAddressBytes(), 0);
            uint broadCastIpAddress = ipAddress | ~ipMaskV4;

            return new IPAddress(BitConverter.GetBytes(broadCastIpAddress));
        }

        enum Contest
        {
            ARRL_DX_SSB_2018
        }

        static Dictionary<Contest, string> datasets = new Dictionary<Contest, string>
        {
            { Contest.ARRL_DX_SSB_2018, "n1mmsender.SampleData.GB2GP.ARRL_DX_SSB_2018" },
        };
    }
}