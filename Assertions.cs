using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InteropMgr
{
    public class Assertions
    {
        private readonly Target target;
        public Assertions(Target target)
        {
            this.target = target;
        }
        public void AssertProcessAttached()
        {
            if (target.Handle == IntPtr.Zero)
            {
                throw new ProcessNotAttachedException("Not attached to any process.");
            }
        }
        public void AssertWritePermission()
        {
            if (!target.HasProcessPermission(Permissions.ProcessPermission.PROCESS_VM_WRITE))
            {
                throw new UnauthorizedAccessException("Cannot write process memory: missing permission 'PROCESS_VM_WRITE'");
            }
        }
        public void AssertReadPermission()
        {
            if (!target.HasProcessPermission(Permissions.ProcessPermission.PROCESS_VM_READ))
            {
                throw new UnauthorizedAccessException("Cannot read process memory: missing permission 'PROCESS_VM_READ'");
            }
        }
        public void AssertInjectionPermissions()
        {
            if (!target.HasProcessPermission(Permissions.ProcessPermission.PROCESS_CREATE_THREAD))
            {
                throw new UnauthorizedAccessException("Cannot inject dll: missing permission 'PROCESS_CREATE_THREAD'");
            }
            if (!target.HasProcessPermission(Permissions.ProcessPermission.PROCESS_QUERY_INFORMATION))
            {
                throw new UnauthorizedAccessException("Cannot inject dll: missing permission 'PROCESS_QUERY_INFORMATION'");
            }
            if (!target.HasProcessPermission(Permissions.ProcessPermission.PROCESS_VM_OPERATION))
            {
                throw new UnauthorizedAccessException("Cannot inject dll: missing permission 'PROCESS_VM_OPERATION'");
            }
            if (!target.HasProcessPermission(Permissions.ProcessPermission.PROCESS_VM_WRITE))
            {
                throw new UnauthorizedAccessException("Cannot inject dll: missing permission 'PROCESS_VM_WRITE'");
            }
            if (!target.HasProcessPermission(Permissions.ProcessPermission.PROCESS_VM_READ))
            {
                throw new UnauthorizedAccessException("Cannot inject dll: missing permission 'PROCESS_VM_READ'");
            }
        }
    }
}
