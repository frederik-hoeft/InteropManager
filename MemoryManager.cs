using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace InteropMgr
{
    public class MemoryManager
    {
        private readonly Target target;
        public MemoryManager(Target target)
        {
            this.target = target;
        }
        public long DereferenceWithOffset64(long baseAddress, long offset)
        {
            return ReadFromMemory<long>(baseAddress + offset);
        }

        public long DereferenceWithOffset32(long baseAddress, long offset)
        {
            return ReadFromMemory<uint>(baseAddress + offset);
        }

        public long GetBaseAddressPointer()
        {
            return GetBaseAddressPointer(false);
        }

        public long GetBaseAddressPointer(bool printDebugInfo)
        {
            HelperMethods.Debug("Determining base address of main module ...", printDebugInfo);
            long baseAddress = target.Process.MainModule.BaseAddress.ToInt64();
            HelperMethods.Debug("Base address is: 0x" + baseAddress.ToString("x"), printDebugInfo);
            target.Assertions.AssertProcessAttached();
            target.Assertions.AssertReadPermission();
            HelperMethods.Debug("Trying to read memory from attached process ...", printDebugInfo);
            long address;
            if (target.Is32BitProcess)
            {
                address = ReadFromMemory<uint>(baseAddress);
            }
            else
            {
                address = ReadFromMemory<long>(baseAddress);
            }
            if (printDebugInfo)
            {
                string hexAddress = address.ToString("x");
                Console.WriteLine("+--------------------------------------------------------------------+");
                Console.WriteLine("| SUCCESS! Base address is pointing to: 0x" + hexAddress + new string(' ', 27 - hexAddress.Length > 0 ? 27 - hexAddress.Length : 0) + "|");
                Console.WriteLine("+--------------------------------------------------------------------+");
            }
            return address;
        }

        public long CalculatePointerPath(List<long> pointerPath)
        {
            return CalculatePointerPath(pointerPath, false);
        }

        public long CalculatePointerPath(List<long> pointerPath, bool printDebugInfo)
        {
            target.Assertions.AssertProcessAttached();
            target.Assertions.AssertReadPermission();
            HelperMethods.Debug("Calculating pointer path: ", printDebugInfo);
            if (printDebugInfo)
            {
                PrintPointerPath(pointerPath);
            }
            long address = target.Process.MainModule.BaseAddress.ToInt64();
            if (printDebugInfo)
            {
                HelperMethods.Debug("Base address is: 0x" + address.ToString("x"), true);
                long addr;
                if (target.Is32BitProcess)
                {
                    addr = target.MemoryManager.ReadFromMemory<uint>(address);
                }
                else
                {
                    addr = target.MemoryManager.ReadFromMemory<long>(address);
                }
                HelperMethods.Debug("  it points to " + addr.ToString("x"), true);
            }
            long previousAddress = 0;
            for (int i = 0; i < pointerPath.Count; i++)
            {
                if (previousAddress == 0)
                {
                    previousAddress = address;
                    if (target.Is32BitProcess)
                    {
                        address = DereferenceWithOffset32(address, pointerPath[i]);
                    }
                    else
                    {
                        address = DereferenceWithOffset64(address, pointerPath[i]);
                    }
                    HelperMethods.Debug("  ['" + target.Process.ProcessName + "' + 0x" + pointerPath[i].ToString("x") + "] -> 0x" + address.ToString("x"),printDebugInfo);
                    continue;
                }
                if (i + 1 < pointerPath.Count)
                {
                    previousAddress = address;
                    if (target.Is32BitProcess)
                    {
                        address = DereferenceWithOffset32(address, pointerPath[i]);
                    }
                    else
                    {
                        address = DereferenceWithOffset64(address, pointerPath[i]);
                    }
                    HelperMethods.Debug("  [0x" + previousAddress.ToString("x") + " + 0x" + pointerPath[i].ToString("x") + "] -> 0x" + address.ToString("x"), printDebugInfo);
                }
                else
                {
                    previousAddress = address;
                    address += pointerPath[i];
                    HelperMethods.Debug("   0x" + previousAddress.ToString("x") + " + 0x" + pointerPath[i].ToString("x") + " = 0x" + address.ToString("x"), printDebugInfo);
                }
            }
            HelperMethods.Debug("CALCULATED ADDRESS IS: 0x" + address.ToString("x"), printDebugInfo);
            HelperMethods.Debug("", printDebugInfo);
            return address;
        }

        #region read / write methods
        public byte[] ReadBytesFromMemory(long address, int length)
        {
            target.Assertions.AssertProcessAttached();
            target.Assertions.AssertReadPermission();
            int bytesRead = 0;
            byte[] buffer = new byte[length];
            bool success = WinAPI.ReadProcessMemory((uint)target.Handle, address, buffer, (uint)buffer.Length, ref bytesRead);
            if (!success)
            {
                throw new MemoryReadException("CRITICAL ERROR: Could not read memory at 0x" + address.ToString("x") + "!");
            }
            return buffer;
        }

        public unsafe T ReadFromMemory<T>(long address) where T : unmanaged
        {
            target.Assertions.AssertProcessAttached();
            target.Assertions.AssertReadPermission();
            int bytesRead = 0;
            byte[] buffer = new byte[sizeof(T)];
            bool success = WinAPI.ReadProcessMemory((uint)target.Handle, address, buffer, (uint)buffer.Length, ref bytesRead);
            if (!success)
            {
                throw new MemoryReadException("CRITICAL ERROR: Could not read memory at 0x" + address.ToString("x") + "!");
            }
            T value = default;
            fixed (byte* b = buffer)
            {
                value = *((T*)b);
            }
            return value;
        }
        
        public T ReadStructFromMemory<T>(long address) where T : struct
        {
            target.Assertions.AssertProcessAttached();
            target.Assertions.AssertReadPermission();
            int size = Marshal.SizeOf(typeof(T));
            byte[] buffer = ReadBytesFromMemory(address, size);
            IntPtr structPointer = Marshal.AllocHGlobal(size);
            Marshal.Copy(buffer, 0, structPointer, size);
            T result = (T)Marshal.PtrToStructure(structPointer, typeof(T));
            Marshal.FreeHGlobal(structPointer);
            return result;
        }

        public string ReadStringFromMemory(long address)
        {
            target.Assertions.AssertProcessAttached();
            target.Assertions.AssertReadPermission();
            StringBuilder builder = new StringBuilder();
            int bytesRead = 0;
            byte[] buffer = new byte[1];
            buffer[0] = 1;
            while (!buffer[0].IsNULL())
            {
                bool success = WinAPI.ReadProcessMemory((uint)target.Handle, address, buffer, (uint)buffer.Length, ref bytesRead);
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

        public void WriteBytesToMemory(long address, byte[] bytes)
        {
            target.Assertions.AssertProcessAttached();
            target.Assertions.AssertWritePermission();
            int bytesWritten = 0;
            bool success = WinAPI.WriteProcessMemory((uint)target.Handle, address, bytes, (uint)bytes.Length, ref bytesWritten);
            if (!success)
            {
                throw new MemoryWriteException("CRITICAL ERROR: Could not write to memory at 0x" + address.ToString("x") + "!");
            }
        }

        public unsafe void WriteToMemory<T>(long address, T value) where T : unmanaged
        {
            target.Assertions.AssertProcessAttached();
            target.Assertions.AssertWritePermission();
            int bytesWritten = 0;
            byte[] buffer = new byte[sizeof(T)];
            fixed (byte* b = buffer)
            {
                *b = *((byte*)&value);
            }
            bool success = WinAPI.WriteProcessMemory((uint)target.Handle, address, buffer, (uint)buffer.Length, ref bytesWritten);
            if (!success)
            {
                throw new MemoryWriteException("CRITICAL ERROR: Could not write to memory at 0x" + address.ToString("x") + "!");
            }
        }

        public unsafe void WriteStructToMemory<T>(long address, T source) where T : struct
        {
            target.Assertions.AssertProcessAttached();
            target.Assertions.AssertWritePermission();
            int size = Marshal.SizeOf(typeof(T));
            IntPtr structPointer = Marshal.AllocHGlobal(size);
            Marshal.StructureToPtr(source, structPointer, false);
            byte[] buffer = new byte[size];
            for (int i = 0; i < size; i++)
            {
                buffer[i] = *((byte*)structPointer + i);
            }
            Marshal.DestroyStructure(structPointer, typeof(T));
            WriteBytesToMemory(address, buffer);
        }

        public void WriteStringToMemory(long address, string value, Encoding encoding)
        {
            target.Assertions.AssertProcessAttached();
            target.Assertions.AssertWritePermission();
            int bytesWritten = 0;
            byte[] buffer = encoding.GetBytes(value);

            bool success = WinAPI.WriteProcessMemory((uint)target.Handle, address, buffer, (uint)buffer.Length, ref bytesWritten);
            if (!success)
            {
                throw new MemoryWriteException("CRITICAL ERROR: Could not write to memory at 0x" + address.ToString("x") + "!");
            }
        }
        #endregion
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
            Console.WriteLine("");
        }
    }
}
