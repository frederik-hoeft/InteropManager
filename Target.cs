using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace InteropMgr
{
    public class Target
    {
        private const byte NULL = 0;

        private Process _process = null;
        private IntPtr _handle = IntPtr.Zero;
        private int _permission = 0x0;
        private Target() { }
        #region constructors
        public static Target Create(int processId)
        {
            Target target = new Target
            {
                _process = Process.GetProcessById(processId)
            };
            return target;
        }

        public static Target Create(Process process)
        {
            Target target = new Target
            {
                _process = process
            };
            return target;
        }

        public static Target CreateFromName(string processName)
        {
            Process[] processes = Process.GetProcessesByName(processName);
            if (processes.Length != 1)
            {
                throw new ProcessEnumerationException("Found more than one process called \"" + processName + "\".");
            }
            Target target = new Target()
            {
                _process = processes[0]
            };
            return target;
        }

        public static Target CreateFromWindowName(string windowName)
        {
            Process[] processes = Process.GetProcesses();

            for (int i = 0; i < processes.Length; i++)
            {
                if (processes[i].MainWindowTitle == windowName)
                {
                    return new Target()
                    {
                        _process = processes[i]
                    };
                }
            }
            throw new ProcessEnumerationException("Could not find window name \'" + windowName + "\'");
        }
        #endregion
        #region public methods
        public bool HasProcessPermission(Permissions.ProcessPermission permission)
        {
            return (_permission & (int)permission) == (int)permission;
        }

        public bool IsAttached()
        {
            return _handle != IntPtr.Zero;
        }

        public void Attach(Permissions.ProcessPermission permission)
        {
            _permission = (int)permission;
            _handle = WinAPI.OpenProcess(_permission, false, _process.Id);
            if (_handle == IntPtr.Zero)
            {
                throw new UnauthorizedAccessException("Could not attach to process with PID " + _process.Id.ToString() + ". Handle was NULL.");
            }
        }

        public Task SendKeyPressAsync(ConsoleKey key, int millisecondsHoldtime) => Task.Run(() => SendKeyPress(key, millisecondsHoldtime));

        public void SendKeyPress(ConsoleKey key, int millisecondsHoldtime)
        {
            SendKeyDown(key);
            Thread.Sleep(millisecondsHoldtime);
            SendKeyUp(key);
        }

        public void SendKeyStroke(ConsoleKey key, bool preserveCurrentWindow)
        {
            IntPtr currentWindow = WinAPI.GetForegroundWindow();
            // Console.WriteLine("Current window is 0x" + currentWindow.ToInt64().ToString("x"));
            if (WinAPI.GetForegroundWindow() != _process.MainWindowHandle)
            {
                // Console.WriteLine("switching to 0x" + _process.MainWindowHandle.ToInt64().ToString("x"));
                InputManager.SwitchWindow(_process.MainWindowHandle);
            }
            string code = key switch
            {
                ConsoleKey.Backspace => "{BACKSPACE}",
                ConsoleKey.Delete => "{DELETE}",
                ConsoleKey.DownArrow => "{DOWN}",
                ConsoleKey.End => "{END}",
                ConsoleKey.Enter => "{ENTER}",
                ConsoleKey.Escape => "{ESC}",
                ConsoleKey.Help => "{HELP}",
                ConsoleKey.Home => "{HOME}",
                ConsoleKey.Insert => "{INS}",
                ConsoleKey.LeftArrow => "{LEFT}",
                ConsoleKey.PageDown => "{PGDN}",
                ConsoleKey.PageUp => "{PGUP}",
                ConsoleKey.PrintScreen => "{PRTSC}",
                ConsoleKey.RightArrow => "{RIGHT}",
                ConsoleKey.Tab => "{TAB}",
                ConsoleKey.UpArrow => "{UP}",
                ConsoleKey.F1 => "{F1}",
                ConsoleKey.F2 => "{F2}",
                ConsoleKey.F3 => "{F3}",
                ConsoleKey.F4 => "{F4}",
                ConsoleKey.F5 => "{F5}",
                ConsoleKey.F6 => "{F6}",
                ConsoleKey.F7 => "{F7}",
                ConsoleKey.F8 => "{F8}",
                ConsoleKey.F9 => "{F9}",
                ConsoleKey.F10 => "{F10}",
                ConsoleKey.F11 => "{F11}",
                ConsoleKey.F12 => "{F12}",
                ConsoleKey.F13 => "{F13}",
                ConsoleKey.F14 => "{F14}",
                ConsoleKey.F15 => "{F15}",
                ConsoleKey.F16 => "{F16}",
                ConsoleKey.Add => "{ADD}",
                ConsoleKey.Subtract => "{SUBTRACT}",
                ConsoleKey.Multiply => "{MULTIPLY}",
                ConsoleKey.Divide => "{DIVIDE}",
                _ => key.ToString()
            };
            // Console.WriteLine("Sending \'" + code + "\' to target process...");
            System.Windows.Forms.SendKeys.SendWait(code);
            // Console.WriteLine("Success?");
            if (WinAPI.GetForegroundWindow() != currentWindow && preserveCurrentWindow)
            {
                // Console.WriteLine("Switching back to 0x" + currentWindow.ToInt64().ToString("x"));
                InputManager.SwitchWindow(currentWindow);
            }
            // Console.WriteLine("All done!");
        }

        public void SendKeys(string keys, bool preserveCurrentWindow)
        {
            IntPtr currentWindow = WinAPI.GetForegroundWindow();
            if (WinAPI.GetForegroundWindow() != _process.MainWindowHandle)
            {
                InputManager.SwitchWindow(_process.MainWindowHandle);
            }
            System.Windows.Forms.SendKeys.SendWait(keys);
            if (WinAPI.GetForegroundWindow() != currentWindow && preserveCurrentWindow)
            {
                InputManager.SwitchWindow(currentWindow);
            }
        }

        public Task SendKeyPressAsync(ConsoleKey key, int millisecondsHoldtime, bool preserveCurrentWindow) => Task.Run(() => SendKeyPress(key, millisecondsHoldtime, preserveCurrentWindow));

        public void SendKeyPress(ConsoleKey key, int millisecondsHoldtime, bool preserveCurrentWindow)
        {
            IntPtr currentWindow = WinAPI.GetForegroundWindow();
            SendKeyDown(key);
            Thread.Sleep(millisecondsHoldtime);
            SendKeyUp(key);
            if (WinAPI.GetForegroundWindow() != currentWindow && preserveCurrentWindow)
            {
                InputManager.SwitchWindow(currentWindow);
            }
        }

        public void SendKeyDown(ConsoleKey key)
        {
            if (WinAPI.GetForegroundWindow() != _process.MainWindowHandle)
            {
                InputManager.SwitchWindow(_process.MainWindowHandle);
            }
            InputManager.KeyDown((ushort)key);
        }

        public void SendKeyUp(ConsoleKey key)
        {
            if (WinAPI.GetForegroundWindow() != _process.MainWindowHandle)
            {
                InputManager.SwitchWindow(_process.MainWindowHandle);
            }
            InputManager.KeyUp((ushort)key);
        }

        #region read / write methods
        public byte[] ReadBytesFromMemory(long address, int length)
        {
            CheckProcessAttached();
            CheckReadPermission();
            int bytesRead = 0;
            byte[] buffer = new byte[length];
            bool success = WinAPI.ReadProcessMemory((int)_handle, address, buffer, buffer.Length, ref bytesRead);
            if (!success)
            {
                throw new MemoryReadException("CRITICAL ERROR: Could not read memory at 0x" + address.ToString("x") + "!");
            }
            return buffer;
        }

        public float ReadFloatFromMemory(long address)
        {
            CheckProcessAttached();
            CheckReadPermission();
            int bytesRead = 0;
            byte[] buffer = new byte[4];

            bool success = WinAPI.ReadProcessMemory((int)_handle, address, buffer, buffer.Length, ref bytesRead);
            if (!success)
            {
                throw new MemoryReadException("CRITICAL ERROR: Could not read memory at 0x" + address.ToString("x") + "!");
            }
            return BitConverter.ToSingle(buffer, 0);
        }

        public string ReadStringFromMemory(long address)
        {
            CheckProcessAttached();
            CheckReadPermission();
            StringBuilder builder = new StringBuilder();
            int bytesRead = 0;
            byte[] buffer = new byte[1];
            buffer[0] = 1;
            while (buffer[0] != NULL)
            {
                bool success = WinAPI.ReadProcessMemory((int)_handle, address, buffer, buffer.Length, ref bytesRead);
                if (!success)
                {
                    throw new MemoryReadException("CRITICAL ERROR: Could not read memory at 0x" + address.ToString("x") + "!");
                }
                builder.Append((char)buffer[0]);
                address += sizeof(byte);
            }
            return builder.ToString();
        }
        //-----------------------------------------------------------------------------------------------------------
        //                                      WRITE PROCESS MEMORY
        //-----------------------------------------------------------------------------------------------------------
        public void WriteFloatToMemory(long address, float value)
        {
            CheckProcessAttached();
            CheckWritePermission();
            int bytesWritten = 0;
            byte[] buffer = value.GetBytes();

            bool success = WinAPI.WriteProcessMemory((int)_handle, address, buffer, buffer.Length, ref bytesWritten);
            if (!success)
            {
                throw new MemoryWriteException("CRITICAL ERROR: Could not write to memory at 0x" + address.ToString("x") + "!");
            }
        }

        public void WriteStringToMemory(long address, string value, Encoding encoding)
        {
            CheckProcessAttached();
            CheckWritePermission();
            int bytesWritten = 0;
            byte[] buffer = encoding.GetBytes(value);

            bool success = WinAPI.WriteProcessMemory((int)_handle, address, buffer, buffer.Length, ref bytesWritten);
            if (!success)
            {
                throw new MemoryWriteException("CRITICAL ERROR: Could not write to memory at 0x" + address.ToString("x") + "!");
            }
        }
        #endregion
        #endregion
        #region getters / setters
        public Process Process
        {
            get { return _process; }
        }
        public Permissions.ProcessPermission Permission
        {
            get { return (Permissions.ProcessPermission)_permission; }
        }
        public IntPtr Handle
        {
            get { return _handle; }
        }
        #endregion
        #region private methods
        private void CheckProcessAttached()
        {
            if (_handle == IntPtr.Zero)
            {
                throw new ProcessNotAttachedException("Not attached to any process.");
            }
        }
        private void CheckWritePermission()
        {
            if ((_permission & (int)Permissions.ProcessPermission.PROCESS_VM_WRITE) != (int)Permissions.ProcessPermission.PROCESS_VM_WRITE)
            {
                throw new UnauthorizedAccessException("Cannot write process memory: missing permission 'PROCESS_VM_WRITE'");
            }
        }
        private void CheckReadPermission()
        {
            if ((_permission & (int)Permissions.ProcessPermission.PROCESS_VM_READ) != (int)Permissions.ProcessPermission.PROCESS_VM_READ)
            {
                throw new UnauthorizedAccessException("Cannot read process memory: missing permission 'PROCESS_VM_READ'");
            }
        }
        #endregion
    }
}
