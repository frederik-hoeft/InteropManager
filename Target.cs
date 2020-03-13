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
        private Process _process = null;
        private IntPtr _handle = IntPtr.Zero;
        private int _permission = 0x0;
        private MemoryManager _memoryManager;
        private Target() { }
        #region constructors
        public static Target Create(int processId)
        {
            Target target = new Target
            {
                _process = Process.GetProcessById(processId)
            };
            target._memoryManager = new MemoryManager(target);
            return target;
        }

        public static Target Create(Process process)
        {
            Target target = new Target
            {
                _process = process
            };
            target._memoryManager = new MemoryManager(target);
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
            target._memoryManager = new MemoryManager(target);
            return target;
        }

        public static Target CreateFromWindowName(string windowName)
        {
            Process[] processes = Process.GetProcesses();

            for (int i = 0; i < processes.Length; i++)
            {
                if (processes[i].MainWindowTitle == windowName)
                {
                    Target target = new Target()
                    {
                        _process = processes[i]
                    };
                    target._memoryManager = new MemoryManager(target);
                    return target;
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

        public void SendKeyStroke(ConsoleKey key, bool preserveCurrentWindow)
        {
            IntPtr currentWindow = WinAPI.GetForegroundWindow();
            if (WinAPI.GetForegroundWindow() != _process.MainWindowHandle)
            {
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
            System.Windows.Forms.SendKeys.SendWait(code);
            if (WinAPI.GetForegroundWindow() != currentWindow && preserveCurrentWindow)
            {
                InputManager.SwitchWindow(currentWindow);
            }
        }

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
        public MemoryManager MemoryManager
        {
            get { return _memoryManager; }
        }
        #endregion
    }
}
