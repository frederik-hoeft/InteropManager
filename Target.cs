using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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
        private bool _is32bit = false;
        private MemoryManager _memoryManager;
        private Injector _injector;
        private Assertions _assersions;
        private Target() { }
        #region constructors
        public static Target Create(int processId)
        {
            Target target = new Target
            {
                _process = Process.GetProcessById(processId)
            };
            target._memoryManager = new MemoryManager(target);
            target._injector = new Injector(target);
            target._assersions = new Assertions(target);
            target._is32bit = target._process.IsWin64Emulator();
            return target;
        }

        public static Target Create(Process process)
        {
            Target target = new Target
            {
                _process = process,
                _is32bit = process.IsWin64Emulator()
            };
            target._memoryManager = new MemoryManager(target);
            target._injector = new Injector(target);
            target._assersions = new Assertions(target);
            return target;
        }

        public static Target CreateFromName(string processName)
        {
            Process[] processes = Process.GetProcessesByName(processName);
            if (processes.Length != 1)
            {
                throw new ProcessEnumerationException("Found zero or more than one process called \"" + processName + "\".");
            }
            Target target = new Target()
            {
                _process = processes[0]
            };
            target._is32bit = target._process.IsWin64Emulator();
            target._memoryManager = new MemoryManager(target);
            target._injector = new Injector(target);
            target._assersions = new Assertions(target);
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
                    target._injector = new Injector(target);
                    target._assersions = new Assertions(target);
                    target._is32bit = target._process.IsWin64Emulator();
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

        public void SetWorkingDirectory(string path)
        {
            if (!Directory.Exists(path))
            {
                throw new DirectoryNotFoundException("Directory does not exist.");
            }
            if (Is32BitProcess)
            {
                _injector.Inject(Directory.GetCurrentDirectory() + "\\cdlib.dll");
            }
            else
            {
                _injector.Inject(Directory.GetCurrentDirectory() + "\\cdlib64.dll", true);
            }
            _injector.Invoke("ChangeRemoteDirectory", path);
            _injector.Free();
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
        public Injector Injector
        {
            get { return _injector; }
        }
        public Assertions Assertions
        {
            get { return _assersions; }
        }
        public bool Is32BitProcess
        {
            get { return _is32bit; }
        }
        #endregion
    }
}
