using System;
using System.Collections.Generic;
using System.Text;

namespace Adf.CacheServer
{
    static class HashHelper
    {
        public static int GetHashCode(string value)
        {
            if (value == null)
            {
                return 0;
            }

            //lower 31 bits
            //取正值

            return value.GetHashCode() & 0x7FFFFFFF;
        }
    }
}
