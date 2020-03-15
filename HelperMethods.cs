using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InteropMgr
{
    public static class HelperMethods
    {
        public static int MakeLong(int low, int high)
        {
            return (high << 16) | (low & 0xffff);
        }
        public static void Debug(string message, bool display)
        {
            if (display)
            {
                Console.WriteLine(message);
            }
        }
    }
}
