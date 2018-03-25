using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using n1mm_leaderboard_shared;
using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Xml;

namespace n1mmlistener
{
    public class Program
    {
        public static void Main(string[] args)
        {
            if (false)
            {
                MakeUpRandomData();
                Debugger.Break();
            }

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