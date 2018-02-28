using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace n1mmsender
{
    class Program
    {

        const string xmlSample = @"<?xml version=""1.0"" encoding=""utf-8""?>
<{re}>
	<contestname>DXPEDITION</contestname>
	<contestnr>10</contestnr>
	<timestamp>{ts}</timestamp>
	<mycall>K8UT</mycall>
	<band>21</band>
	<rxfreq>2125500</rxfreq>
	<txfreq>2125500</txfreq>
	<operator>K8UT</operator>
	<mode>USB</mode>
	<call>W2BBB</call>
	<countryprefix>K</countryprefix>
	<wpxprefix>W2</wpxprefix>
	<stationprefix>K8UT</stationprefix>
	<continent>NA</continent>
	<snt>59</snt>
	<sntnr>2</sntnr>
	<rcv>59</rcv>
	<rcvnr>0</rcvnr>
	<gridsquare></gridsquare>
	<exchange1></exchange1>
	<section></section>
	<comment></comment>
	<qth></qth>
	<name></name>
	<power></power>
	<misctext></misctext>
	<zone>5</zone>
	<prec></prec>
	<ck>0</ck>
	<ismultiplier1>0</ismultiplier1>
	<ismultiplier2>0</ismultiplier2>
	<ismultiplier3>0</ismultiplier3>
	<points>1</points>
	<radionr>1</radionr>
	<RoverLocation></RoverLocation>
	<RadioInterfaced>0</RadioInterfaced>
	<NetworkedCompNr>0</NetworkedCompNr>
	<IsOriginal>True</IsOriginal>
	<NetBiosName>DEV-PC</NetBiosName>
	<IsRunQSO>0</IsRunQSO>
	<Run1Run2></Run1Run2>
	<ContactType></ContactType>
	<StationName>PHONE-15M</StationName>
</{re}>";

        const string xmldelete = @"<?xml version=""1.0"" encoding=""utf-8""?>
<contactdelete>
	<timestamp>2018-02-27 21:33:54</timestamp>
	<call>M0AAA</call>
	<contestnr>0</contestnr>
	<StationName>DESKTOP-BLFHI3S</StationName>
</contactdelete>";

        static List<DateTime> dates = new List<DateTime>();

        static void Main(string[] args)
        {
            using (var client = new UdpClient("127.0.0.1", 12060))
            {
                string xmlreplace = xmlSample.Replace("{re}", "contactreplace");

                while (true)
                {
                    Console.Write("Waiting for [i]nsert, [d]elete, [r]eplace");

                    var cki = Console.ReadKey(true);
                    Console.WriteLine();

                    string xml;

                    if (cki.KeyChar == 'i')
                    {
                        DateTime date = DateTime.Now;
                        dates.Add(date);

                        xml = xmlSample.Replace("{re}", "contactinfo")
                                       .Replace("{ts}", date.ToString("yyyy-MM-dd HH:mm:ss"));
                    }
                    else if (cki.KeyChar == 'r')
                    {
                        xml = xmlreplace;
                    }
                    else if (cki.KeyChar == 'd')
                    {
                        xml = xmldelete;
                    }
                    else continue;

                    var buf = Encoding.UTF8.GetBytes(xml);
                    client.Send(buf, buf.Length);
                }
            }
        }
    }
}