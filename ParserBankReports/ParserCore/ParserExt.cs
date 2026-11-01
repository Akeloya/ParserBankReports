using System;
using System.Linq;

namespace ParserCore
{
    public static class ParserExt
    {
        private static readonly char[] _digitChars = { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', '.', ',' };
        public static decimal AsDecimal(this string sumStr)
        {
            if (string.IsNullOrEmpty(sumStr))
                return 0;
            var str = sumStr.ToList();
            if (!string.IsNullOrEmpty(sumStr))
                for (var i = 0; i < str.Count;)
                {
                    if (!_digitChars.Contains(str[i]))
                        str.RemoveAt(i);
                    else
                        i++;
                }
            var result = new string(str.ToArray());
            return decimal.Parse(result);
        }

        public static DateTime AsDate(this string dateStr)
        {
            if (string.IsNullOrEmpty(dateStr))
                return DateTime.MinValue;
            return DateTime.Parse(dateStr);
        }
    }
}
