using System;
using System.IO;
using System.Text;
using System.Xml.Serialization;

namespace n1mmlistener
{
    [XmlRoot(ElementName = "contactdelete")]
    public class ContactDelete
    {
        public static bool TryParse(byte[] datagram, out ContactDelete contactDelete)
        {
            string str;
            try
            {
                str = Encoding.UTF8.GetString(datagram);
            }
            catch (Exception ex)
            {
                Program.Log("Exception: {0}", ex);
                contactDelete = null;
                return false;
            }

            try
            {
                var serialiser = new XmlSerializer(typeof(ContactDelete));
                using (var reader = new StringReader(str))
                {
                    contactDelete = (ContactDelete)serialiser.Deserialize(reader);
                }
            }
            catch (Exception ex)
            {
                Program.Log("Exception: {0}", ex);
                contactDelete = null;
                return false;
            }

            return true;
        }

        [XmlElement(ElementName = "timestamp")]
        public string Timestamp { get; set; }
        [XmlElement(ElementName = "call")]
        public string Call { get; set; }
        [XmlElement(ElementName = "contestnr")]
        public int Contestnr { get; set; }
        [XmlElement(ElementName = "StationName")]
        public string StationName { get; set; }
    }
}