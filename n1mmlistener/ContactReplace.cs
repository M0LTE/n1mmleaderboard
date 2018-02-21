using System;
using System.IO;
using System.Text;
using System.Xml.Serialization;

namespace n1mmlistener
{
    [XmlRoot(ElementName = "contactreplace")]
    public class ContactReplace1
    {
        public static bool TryParse(byte[] datagram, out ContactReplace contactReplace)
        {
            string str;
            try
            {
                str = Encoding.UTF8.GetString(datagram);
            }
            catch (Exception ex)
            {
                Program.Log("Exception: {0}", ex);
                contactReplace = null;
                return false;
            }

            try
            {
                var serialiser = new XmlSerializer(typeof(ContactReplace));
                using (var reader = new StringReader(str))
                {
                    contactReplace = (ContactReplace)serialiser.Deserialize(reader);
                }
            }
            catch (Exception ex)
            {
                Program.Log("Exception: {0}", ex);
                contactReplace = null;
                return false;
            }

            return true;
        }

        [XmlElement(ElementName = "contestname")]
        public string Contestname { get; set; }
        [XmlElement(ElementName = "contestnr")]
        public int Contestnr { get; set; }
        [XmlElement(ElementName = "timestamp")]
        public string Timestamp { get; set; }
        [XmlElement(ElementName = "mycall")]
        public string Mycall { get; set; }
        [XmlElement(ElementName = "band")]
        public int Band { get; set; }
        [XmlElement(ElementName = "rxfreq")]
        public string Rxfreq { get; set; }
        [XmlElement(ElementName = "txfreq")]
        public string Txfreq { get; set; }
        [XmlElement(ElementName = "operator")]
        public string Operator { get; set; }
        [XmlElement(ElementName = "mode")]
        public string Mode { get; set; }
        [XmlElement(ElementName = "call")]
        public string Call { get; set; }
        [XmlElement(ElementName = "countryprefix")]
        public string Countryprefix { get; set; }
        [XmlElement(ElementName = "wpxprefix")]
        public string Wpxprefix { get; set; }
        [XmlElement(ElementName = "stationprefix")]
        public string Stationprefix { get; set; }
        [XmlElement(ElementName = "continent")]
        public string Continent { get; set; }
        [XmlElement(ElementName = "snt")]
        public string Snt { get; set; }
        [XmlElement(ElementName = "sntnr")]
        public string Sntnr { get; set; }
        [XmlElement(ElementName = "rcv")]
        public string Rcv { get; set; }
        [XmlElement(ElementName = "rcvnr")]
        public string Rcvnr { get; set; }
        [XmlElement(ElementName = "gridsquare")]
        public string Gridsquare { get; set; }
        [XmlElement(ElementName = "exchange1")]
        public string Exchange1 { get; set; }
        [XmlElement(ElementName = "section")]
        public string Section { get; set; }
        [XmlElement(ElementName = "comment")]
        public string Comment { get; set; }
        [XmlElement(ElementName = "qth")]
        public string Qth { get; set; }
        [XmlElement(ElementName = "name")]
        public string Name { get; set; }
        [XmlElement(ElementName = "power")]
        public string Power { get; set; }
        [XmlElement(ElementName = "misctext")]
        public string Misctext { get; set; }
        [XmlElement(ElementName = "zone")]
        public string Zone { get; set; }
        [XmlElement(ElementName = "prec")]
        public string Prec { get; set; }
        [XmlElement(ElementName = "ck")]
        public string Ck { get; set; }
        [XmlElement(ElementName = "ismultiplier1")]
        public int Ismultiplier1 { get; set; }
        [XmlElement(ElementName = "ismultiplier2")]
        public int Ismultiplier2 { get; set; }
        [XmlElement(ElementName = "ismultiplier3")]
        public int Ismultiplier3 { get; set; }
        [XmlElement(ElementName = "points")]
        public string Points { get; set; }
        [XmlElement(ElementName = "radionr")]
        public string Radionr { get; set; }
        [XmlElement(ElementName = "RoverLocation")]
        public string RoverLocation { get; set; }
        [XmlElement(ElementName = "RadioInterfaced")]
        public string RadioInterfaced { get; set; }
        [XmlElement(ElementName = "NetworkedCompNr")]
        public string NetworkedCompNr { get; set; }
        [XmlElement(ElementName = "IsOriginal")]
        public string IsOriginal { get; set; }
        [XmlElement(ElementName = "NetBiosName")]
        public string NetBiosName { get; set; }
        [XmlElement(ElementName = "IsRunQSO")]
        public string IsRunQSO { get; set; }
        [XmlElement(ElementName = "Run1Run2")]
        public string Run1Run2 { get; set; }
        [XmlElement(ElementName = "ContactType")]
        public string ContactType { get; set; }
        [XmlElement(ElementName = "StationName")]
        public string StationName { get; set; }
    }

}
