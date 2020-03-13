using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Text;
using System.Threading.Tasks;

namespace InteropMgr
{
    public static partial class Extensions
    {
        public unsafe static byte[] GetBytes(this float value)
        {
            byte[] bytes = new byte[4];
            fixed (byte* b = bytes)
            {
                *((int*)b) = *(int*)&value;
            }
            return bytes;
        }

        public static bool IsNULL(this byte b) => b == (byte)0;

        public static int GetParentPid(this Process process)
        {
            string query = string.Format("SELECT ParentProcessId FROM Win32_Process WHERE ProcessId = {0}", process.Id);
            ManagementObjectSearcher search = new ManagementObjectSearcher("root\\CIMV2", query);
            ManagementObjectCollection.ManagementObjectEnumerator results = search.Get().GetEnumerator();
            results.MoveNext();
            ManagementBaseObject queryObj = results.Current;
            uint parentId = (uint)queryObj["ParentProcessId"];
            return (int)parentId;
        }

        public static Process GetParentProcess(this Process process)
        {
            return Process.GetProcessById(process.GetParentPid());
        }
    }
}
