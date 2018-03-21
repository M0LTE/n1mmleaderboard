using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Xml.Serialization;

namespace n1mm_leaderboard_shared
{
    [XmlRoot(ElementName = "contactreplace")]
    public class N1mmXmlContactReplace : N1mmXmlContactBase
    {
        public static bool TryParse(byte[] datagram, out N1mmXmlContactReplace contactReplace)
        {
            string str;
            try
            {
                str = Encoding.UTF8.GetString(datagram);
            }
            catch (Exception ex)
            {
                Log("Exception: {0}", ex);
                contactReplace = null;
                return false;
            }

            try
            {
                var serialiser = new XmlSerializer(typeof(N1mmXmlContactReplace));
                using (var reader = new StringReader(str))
                {
                    contactReplace = (N1mmXmlContactReplace)serialiser.Deserialize(reader);
                }
            }
            catch (Exception ex)
            {
                Log("Exception: {0}", ex);
                contactReplace = null;
                return false;
            }

            return true;
        }

        static void Log(string format, params object[] args)
        {
            Trace.WriteLine(string.Format(format, args));
        }
    }
}
