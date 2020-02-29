using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace InteropMgr
{
    public static class InputManager
    {
        public static IntPtr FindWindow(string name)
        {
            Process[] procs = Process.GetProcesses();

            foreach (Process proc in procs)
            {
                if (proc.MainWindowTitle == name)
                {
                    return proc.MainWindowHandle;
                }
            }

            return IntPtr.Zero;
        }

        public static void SwitchWindow(IntPtr windowHandle)
        {
            if (WinAPI.GetForegroundWindow() == windowHandle)
                return;

            IntPtr foregroundWindowHandle = WinAPI.GetForegroundWindow();
            uint currentThreadId = WinAPI.GetCurrentThreadId();
            uint foregroundThreadId = WinAPI.GetWindowThreadProcessId(foregroundWindowHandle, out uint temp);
            WinAPI.AttachThreadInput(currentThreadId, foregroundThreadId, true);
            WinAPI.SetForegroundWindow(windowHandle);
            WinAPI.AttachThreadInput(currentThreadId, foregroundThreadId, false);
            while (WinAPI.GetForegroundWindow() != windowHandle);
        }

        public static void PressKey(char ch, bool press)
        {
            byte vk = WinAPI.VkKeyScan(ch);
            ushort scanCode = (ushort)WinAPI.MapVirtualKey(vk, 0);

            if (press)
                KeyDown(scanCode);
            else
                KeyUp(scanCode);
        }

        public static void KeyDown(ushort scanCode)
        {
            NativeResources.INPUT[] inputs = new NativeResources.INPUT[1];
            inputs[0].type = (int)NativeResources.InputDevice.INPUT_KEYBOARD;
            inputs[0].ki.dwFlags = 0;
            inputs[0].ki.wScan = (ushort)(scanCode & 0xff);

            uint intReturn = WinAPI.SendInput(1, inputs, Marshal.SizeOf(inputs[0]));
            if (intReturn != 1)
            {
                throw new Exception("Could not send key: " + scanCode);
            }
        }

        public static void KeyUp(ushort scanCode)
        {
            NativeResources.INPUT[] inputs = new NativeResources.INPUT[1];
            inputs[0].type = (int)NativeResources.InputDevice.INPUT_KEYBOARD;
            inputs[0].ki.wScan = scanCode;
            inputs[0].ki.dwFlags = (uint)NativeResources.InputEvents.KEYEVENTF_KEYUP;
            uint intReturn = WinAPI.SendInput(1, inputs, Marshal.SizeOf(inputs[0]));
            if (intReturn != 1)
            {
                throw new Exception("Could not send key: " + scanCode);
            }
        }

        public static class NativeResources
        {
            public enum InputNotifications
            {
                WM_KEYDOWN = 0x100,
                WM_KEYUP = 0x101,
                WM_LBUTTONDOWN = 0x201,
                WM_LBUTTONUP = 0x202,
                WM_CHAR = 0x102,
                MK_LBUTTON = 0x01,
                VK_RETURN = 0x0d,
                VK_ESCAPE = 0x1b,
                VK_TAB = 0x09,
                VK_LEFT = 0x25,
                VK_UP = 0x26,
                VK_RIGHT = 0x27,
                VK_DOWN = 0x28,
                VK_F5 = 0x74,
                VK_F6 = 0x75,
                VK_F7 = 0x76
            }
            public enum InputDevice
            {
                INPUT_MOUSE = 0,
                INPUT_KEYBOARD = 1,
                INPUT_HARDWARE = 2
            }
            public enum InputEvents
            {
                KEYEVENTF_EXTENDEDKEY = 0x0001,
                KEYEVENTF_KEYUP = 0x0002,
                KEYEVENTF_UNICODE = 0x0004,
                KEYEVENTF_SCANCODE = 0x0008,
                XBUTTON1 = 0x0001,
                XBUTTON2 = 0x0002,
                MOUSEEVENTF_MOVE = 0x0001,
                MOUSEEVENTF_LEFTDOWN = 0x0002,
                MOUSEEVENTF_LEFTUP = 0x0004,
                MOUSEEVENTF_RIGHTDOWN = 0x0008,
                MOUSEEVENTF_RIGHTUP = 0x0010,
                MOUSEEVENTF_MIDDLEDOWN = 0x0020,
                MOUSEEVENTF_MIDDLEUP = 0x0040,
                MOUSEEVENTF_XDOWN = 0x0080,
                MOUSEEVENTF_XUP = 0x0100,
                MOUSEEVENTF_WHEEL = 0x0800,
                MOUSEEVENTF_VIRTUALDESK = 0x4000,
                MOUSEEVENTF_ABSOLUTE = 0x8000
            }
            [StructLayout(LayoutKind.Sequential)]
            public struct MOUSEINPUT
            {
                int dx;
                int dy;
                uint mouseData;
                uint dwFlags;
                uint time;
                IntPtr dwExtraInfo;
            }

            [StructLayout(LayoutKind.Sequential)]
            public struct KEYBDINPUT
            {
                public ushort wVk;
                public ushort wScan;
                public uint dwFlags;
                public uint time;
                public IntPtr dwExtraInfo;
            }

            [StructLayout(LayoutKind.Sequential)]
            public struct HARDWAREINPUT
            {
                uint uMsg;
                ushort wParamL;
                ushort wParamH;
            }

            [StructLayout(LayoutKind.Explicit)]
            public struct INPUT
            {
                [FieldOffset(0)]
                public int type;
                [FieldOffset(4)] //*
                public MOUSEINPUT mi;
                [FieldOffset(4)] //*
                public KEYBDINPUT ki;
                [FieldOffset(4)] //*
                public HARDWAREINPUT hi;
            }
        }
    }
}
