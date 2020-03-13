using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Management;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace InteropMgr
{

#nullable enable
    /// <summary>
    /// TODO: REFACTORING IN PROGRESS
    /// </summary>
    public static class InteropManager
    {
        public static class RawMethods
        {
            public static void SendKeys(Target target, string keys, bool preserveCurrentWindow)
            {
                IntPtr currentWindow = WinAPI.GetForegroundWindow();
                if (WinAPI.GetForegroundWindow() != target.Process.MainWindowHandle)
                {
                    InputManager.SwitchWindow(target.Process.MainWindowHandle);
                }
                System.Windows.Forms.SendKeys.SendWait(keys);
                if (WinAPI.GetForegroundWindow() != currentWindow && preserveCurrentWindow)
                {
                    InputManager.SwitchWindow(currentWindow);
                }
            }
            public static Task SendKeyPressAsync(Target target, ConsoleKey key, int millisecondsHoldtime, bool preserveCurrentWindow) => Task.Run(() => SendKeyPress(target, key, millisecondsHoldtime, preserveCurrentWindow));

            public static void SendKeyPress(Target target, ConsoleKey key, int millisecondsHoldtime, bool preserveCurrentWindow)
            {
                IntPtr currentWindow = WinAPI.GetForegroundWindow();
                SendKeyDown(target, key);
                Thread.Sleep(millisecondsHoldtime);
                SendKeyUp(target, key);
                if (WinAPI.GetForegroundWindow() != currentWindow && preserveCurrentWindow)
                {
                    InputManager.SwitchWindow(currentWindow);
                }
            }
            public static void SendKeyDown(Target target, ConsoleKey key)
            {
                if (WinAPI.GetForegroundWindow() != target.Process.MainWindowHandle)
                {
                    InputManager.SwitchWindow(target.Process.MainWindowHandle);
                }
                InputManager.KeyDown((ushort)key);
            }

            public static void SendKeyUp(Target target, ConsoleKey key)
            {
                if (WinAPI.GetForegroundWindow() != target.Process.MainWindowHandle)
                {
                    InputManager.SwitchWindow(target.Process.MainWindowHandle);
                }
                InputManager.KeyUp((ushort)key);
            }
        }
    }   
#if false

        public const byte NULL = 0;
        

        private IntPtr _handle;
        private string _name;
        private int _pid;
        private Process _proc = null;
        private Permission _perm;
        private int _opCode = 0x0;
        private IntPtr _baseAddress;
        private bool debuggingEnabled = false;

        public InteropManager() { }

        public void GetMainBaseAddress()
        {
            Debug("Determining base address of main module ...");
            _baseAddress = _proc.MainModule.BaseAddress;
            getBaseAddess();
        }

        public ProcessModuleCollection GetLoadedModules()
        {
            return _proc.Modules;
        }

        public void GetBaseAddressOfModule(ProcessModule processModule)
        {
            Debug("Determining base address of " + processModule.ModuleName + " ...");
            _baseAddress = processModule.BaseAddress;
            getBaseAddess();
        }

        private void getBaseAddess()
        {
            Debug("Base address is: 0x" + _baseAddress.ToInt64().ToString("x"));
            Debug("");
            Debug("Attaching to process ...");
            Debug("Requesting the following permission: PROCESS_ALL_ACCESS (0x1F0FFF)");
            Attach(Permission.PROCESS_ALL_ACCESS);
            Debug("Successfully attached to '" + _name + ".exe'");
            Debug("");
            Debug("Trying to read memory from attached process ...");
            int bytesRead = 0;
            byte[] buffer = new byte[8]; //64 bit address takes 8 bytes

            bool success = WinAPI.ReadProcessMemory((int)_handle, _baseAddress.ToInt64(), buffer, buffer.Length, ref bytesRead);
            if (!success)
            {
                throw new MemoryReadException("CRITICAL ERROR: Could not read memory at 0x" + _baseAddress.ToInt64().ToString("x") + "!");
            }
            long address = BitConverter.ToInt64(buffer, 0);
            string hexAddress = address.ToString("x");
            Debug("+--------------------------------------------------------------------+");
            Debug("| SUCCESS! Base address is pointing to: 0x" + hexAddress + new string(' ', 27 - hexAddress.Length > 0 ? 27 - hexAddress.Length : 0) + "|");
            Debug("+--------------------------------------------------------------------+");
        }

        public long AddAndDeref(long baseAddress, long offset)
        {
            int bytesRead = 0;
            byte[] buffer = new byte[8];

            long address = baseAddress + offset;
            bool success = WinAPI.ReadProcessMemory((int)_handle, address, buffer, buffer.Length, ref bytesRead);
            if (!success)
            {
                Debug("");
                throw new MemoryReadException("CRITICAL ERROR: Could not read memory at 0x" + address.ToString("x") + "!");
            }
            return BitConverter.ToInt64(buffer, 0);
        }

        

        private void PrintPointerPath(List<long> path)
        {
            for (int i = 0; i < path.Count; i++)
            {
                if (i != 0)
                {
                    Console.Write(" -> ");
                }
                Console.Write("0x" + path[i].ToString("x"));
            }
            Debug("");
        }

        public long CalculateAddress(List<long>? pointerPath)
        {
            Console.Write("Calculating pointer path: ");
            PrintPointerPath(pointerPath);
            long address = _baseAddress.ToInt64();
            long previousAddress = 0;
            Debug("   '" + _name + ".exe' = 0x" + address.ToString("x"));
            for (int i = 0; i < pointerPath.Count; i++)
            {
                if (previousAddress == 0)
                {
                    previousAddress = address;
                    address = AddAndDeref(address, pointerPath[i]);
                    Debug("  ['" + _name + ".exe' + 0x" + pointerPath[i].ToString("x") + "] -> 0x" + address.ToString("x"));
                    continue;
                }
                if (i + 1 < pointerPath.Count)
                {
                    previousAddress = address;
                    address = AddAndDeref(address, pointerPath[i]);
                    Debug("  [0x" + previousAddress.ToString("x") + " + 0x" + pointerPath[i].ToString("x") + "] -> 0x" + address.ToString("x"));
                }
                else
                {
                    previousAddress = address;
                    address += pointerPath[i];
                    Debug("   0x" + previousAddress.ToString("x") + " + 0x" + pointerPath[i].ToString("x") + " = 0x" + address.ToString("x"));
                }
            }
            Debug("CALCULATED ADDRESS IS: 0x" + address.ToString("x"));
            Debug("");
            return address;
        }
        private void Debug(string message)
        {
            if (debuggingEnabled)
            {
                Console.WriteLine(message);
            }
        }

        public Process TargetProcess
        {
            get { return _proc; }
        }

        public string ProcessName
        {
            get { return _name; }
            set { _name = value; }
        }

        public int ProcessId
        {
            get { return _pid; }
        }

        public bool Debugging
        {
            get { return debuggingEnabled; }
            set { debuggingEnabled = value; }
        }

        public Permission Permission
        {
            get { return _perm; }
        }
    }
#endif
}
