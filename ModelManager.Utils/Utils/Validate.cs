using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModelManager.Utils
{
    public class Validate
    {
        public static bool IsInt(string input)
        {
            int output;
            return int.TryParse(input, out output);
        }

        public static bool IsFloat(string input)
        {
            float output;
            return float.TryParse(input, out output);
        }

        public static bool IsDouble(string input)
        {
            double output;
            return double.TryParse(input, out output);
        }

        public static bool IsLong(string input)
        {
            long output;
            return long.TryParse(input, out output);
        }

        public static bool IsShort(string input)
        {
            short output;
            return short.TryParse(input, out output);
        }

        public static bool IsByte(string input)
        {
            byte output;
            return byte.TryParse(input, out output);
        }

        public static bool IsSbyte(string input)
        {
            sbyte output;
            return sbyte.TryParse(input, out output);
        }

        public static bool IsDecimal(string input)
        {
            decimal output;
            return decimal.TryParse(input, out output);
        }
    }
}
