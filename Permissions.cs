using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InteropMgr
{
    public static class Permissions
    {
        [Flags]
        public enum StandardPermissions
        {
            STANDARD_RIGHTS_REQUIRED = 0x000F0000,
            STANDARD_RIGHTS_READ = 0x00020000
        }
        [Flags]
        public enum ProcessPermission
        {
            PROCESS_ALL_ACCESS = 0x1F0FFF,
            PROCESS_TERMINATE = 0x0001,
            PROCESS_CREATE_THREAD = 0x0002,
            PROCESS_SET_SESSIONID = 0x0004,
            PROCESS_VM_OPERATION = 0x0008,
            PROCESS_VM_READ = 0x0010,
            PROCESS_VM_WRITE = 0x0020,
            PROCESS_DUP_HANDLE = 0x0040,
            PROCESS_CREATE_PROCESS = 0x0080,
            PROCESS_SET_QUOTA = 0x0100,
            PROCESS_SET_INFORMATION = 0x0200,
            PROCESS_QUERY_INFORMATION = 0x0400,
            PROCESS_SUSPEND_RESUME = 0x0800,
            PROCESS_QUERY_LIMITED_INFORMATION = 0x1000,
            DELETE = 0x00010000,
            READ_CONTROL = 0x00020000,
            WRITE_DAC = 0x00040000,
            WRITE_OWNER = 0x00080000,
            SYNCHRONIZE = 0x00100000,
            END = 0xFFF
        }
        [Flags]
        public enum MemoryPermission
        {
            MEM_COMMIT = 0x00001000,
            MEM_RESERVE = 0x00002000,
            PAGE_READWRITE = 4
        }
        [Flags]
        public enum TokenPermission
        {
            TOKEN_ASSIGN_PRIMARY = 0x0001,
            TOKEN_DUPLICATE = 0x0002,
            TOKEN_IMPERSONATE = 0x0004,
            TOKEN_QUERY = 0x0008,
            TOKEN_QUERY_SOURCE = 0x0010,
            TOKEN_ADJUST_PRIVILEGES = 0x0020,
            TOKEN_ADJUST_GROUPS = 0x0040,
            TOKEN_ADJUST_DEFAULT = 0x0080,
            TOKEN_ADJUST_SESSIONID = 0x0100,
            TOKEN_READ = (StandardPermissions.STANDARD_RIGHTS_READ | TOKEN_QUERY),
            TOKEN_ALL_ACCESS = (StandardPermissions.STANDARD_RIGHTS_REQUIRED | TOKEN_ASSIGN_PRIMARY |
            TOKEN_DUPLICATE | TOKEN_IMPERSONATE | TOKEN_QUERY | TOKEN_QUERY_SOURCE |
            TOKEN_ADJUST_PRIVILEGES | TOKEN_ADJUST_GROUPS | TOKEN_ADJUST_DEFAULT |
            TOKEN_ADJUST_SESSIONID)
        }
    }
}
