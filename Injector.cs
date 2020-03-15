using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace InteropMgr
{
    public class Injector
    {
        private readonly Target target;
        public Injector(Target target)
        {
            this.target = target;
        }
        public void Inject(string dllName)
        {
            Inject(dllName, false);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="dllName">name of the dll we want to inject</param>
        public void Inject(string dllName, bool printDebugInfo)
        {
            target.Assertions.AssertProcessAttached();
            target.Assertions.AssertInjectionPermissions();

            // searching for the address of LoadLibraryA and storing it in a pointer
            IntPtr kernel32Handle = WinAPI.GetModuleHandle("kernel32.dll");
            if (kernel32Handle == IntPtr.Zero)
            {
                uint errorCode = WinAPI.GetLastError();
                throw new Win32Exception((int)errorCode, "Encountered error " + errorCode.ToString() + " (0x" + errorCode.ToString("x") + ") - FATAL: Could not get handle of kernel32.dll: was NULL.");
            }
            IntPtr loadLibraryAddr = WinAPI.GetProcAddress(kernel32Handle, "LoadLibraryA");
            if (loadLibraryAddr == IntPtr.Zero)
            {
                uint errorCode = WinAPI.GetLastError();
                throw new Win32Exception((int)errorCode, "Encountered error " + errorCode.ToString() + " (0x" + errorCode.ToString("x") + ") - FATAL: Could not get address of LoadLibraryA: was NULL.");
            }
            HelperMethods.Debug("LoadLibraryA is at 0x" + loadLibraryAddr.ToInt64().ToString("x"), printDebugInfo);

            // alocating some memory on the target process - enough to store the name of the dll
            // and storing its address in a pointer
            uint size = (uint)((dllName.Length + 1) * Marshal.SizeOf(typeof(char)));
            IntPtr allocMemAddress = WinAPI.VirtualAllocEx(target.Handle, IntPtr.Zero, size, (uint)Permissions.MemoryPermission.MEM_COMMIT | (uint)Permissions.MemoryPermission.MEM_RESERVE, (uint)Permissions.MemoryPermission.PAGE_READWRITE);
            HelperMethods.Debug("Allocated memory at 0x" + allocMemAddress.ToInt64().ToString("x"), printDebugInfo);

            int bytesWritten = 0;
            // writing the name of the dll there
            byte[] buffer = new byte[size];
            byte[] bytes = Encoding.ASCII.GetBytes(dllName);
            Array.Copy(bytes, 0, buffer, 0, bytes.Length);
            buffer[buffer.Length - 1] = 0;
            bool success = WinAPI.WriteProcessMemory((uint)target.Handle, allocMemAddress.ToInt64(), buffer, size, ref bytesWritten);
            if (success)
            {
                HelperMethods.Debug("Successfully wrote \"" + dllName + "\" to 0x" + allocMemAddress.ToInt64().ToString("x"), printDebugInfo);
            }
            else
            {
                HelperMethods.Debug("FAILED to write dll name!", printDebugInfo);
            }
            // creating a thread that will call LoadLibraryA with allocMemAddress as argument
            HelperMethods.Debug("Injecting dll ...", printDebugInfo);
            IntPtr threadHandle = WinAPI.CreateRemoteThread(target.Handle, IntPtr.Zero, 0, loadLibraryAddr, allocMemAddress, 0, IntPtr.Zero);
            HelperMethods.Debug("CreateRemoteThread returned the following handle: 0x" + threadHandle.ToInt32().ToString("x"), printDebugInfo);
            uint waitExitCode = WinAPI.WaitForSingleObject(threadHandle, 0xFFFF);
            HelperMethods.Debug("Waiting for thread to exit ...", printDebugInfo);
            HelperMethods.Debug("WaitForSingleObject returned 0x" + waitExitCode.ToString("x"), printDebugInfo);
            success = WinAPI.GetExitCodeThread(threadHandle, out uint exitCode);
            if (!success)
            {
                uint errorCode = WinAPI.GetLastError();
                throw new Win32Exception((int)errorCode, "Encountered error " + errorCode.ToString() + " (0x" + errorCode.ToString("x") + ") - FATAL: Non-zero exit code of GetExitCodeThread.");
            }
            HelperMethods.Debug("Remote thread returned 0x" + exitCode.ToString("x"), printDebugInfo);
            success = WinAPI.CloseHandle(threadHandle);
            if (!success)
            {
                uint errorCode = WinAPI.GetLastError();
                throw new Win32Exception((int)errorCode, "Encountered error " + errorCode.ToString() + " (0x" + errorCode.ToString("x") + ") - FATAL: Failed calling CloseHandle on 0x" + threadHandle.ToInt64().ToString("x") + ".");
            }
            HelperMethods.Debug("Called CloseHandle on 0x" + threadHandle.ToInt64().ToString("x") + ".", printDebugInfo);
            success = WinAPI.VirtualFreeEx(target.Handle, allocMemAddress, 0, 0x8000);
            if (!success)
            {
                uint errorCode = WinAPI.GetLastError();
                throw new Win32Exception((int)errorCode, "Encountered error " + errorCode.ToString() + " (0x" + errorCode.ToString("x") + ") - FATAL: Failed calling VirtualFreeEx on 0x" + allocMemAddress.ToInt64().ToString("x") + ".");
            }
            HelperMethods.Debug("Released all previously allocated resources!", printDebugInfo);
        }
    }
}
