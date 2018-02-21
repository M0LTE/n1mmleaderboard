using System;
using System.Linq;

namespace n1mmlistener
{
    static class ExtensionMethods
    {
        public static string ToHexBytes(this byte[] arr)
        {
            return String.Join(" ", arr.Select(b => String.Format("{0:X2}", b)));
        }

        public static string Truncate(this string str)
        {
            if (str.Length < 100)
            {
                return str;
            }

            return str.Substring(0, 97) + "...";
        }
    }
}
