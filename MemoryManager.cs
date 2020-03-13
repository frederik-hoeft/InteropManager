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
        public long DereferenceWithOffset(long baseAddress, long offset)
        {
            int bytesRead = 0;
            byte[] buffer = new byte[8];

            long address = baseAddress + offset;
            bool success = WinAPI.ReadProcessMemory((int)target.Handle, address, buffer, buffer.Length, ref bytesRead);
            if (!success)
            {
                throw new MemoryReadException("CRITICAL ERROR: Could not read memory at 0x" + address.ToString("x") + "!");
            }
            return BitConverter.ToInt64(buffer, 0);
        }
        public long CalculatePointerPath(Target target, List<long> pointerPath)
        {
            long address = target.Process.MainModule.BaseAddress.ToInt64();
            long previousAddress = 0;
            for (int i = 0; i < pointerPath.Count; i++)
            {
                if (previousAddress == 0)
                {
                    previousAddress = address;
                    address = DereferenceWithOffset(address, pointerPath[i]);
                    continue;
                }
                if (i + 1 < pointerPath.Count)
                {
                    previousAddress = address;
                    address = DereferenceWithOffset(address, pointerPath[i]);
                }
                else
                {
                    previousAddress = address;
                    address += pointerPath[i];
                }
            }
            return address;
        }

        #region read / write methods
        public byte[] ReadBytesFromMemory(long address, int length)
        {
            AssertProcessAttached();
            AssertReadPermission();
            int bytesRead = 0;
            byte[] buffer = new byte[length];
            bool success = WinAPI.ReadProcessMemory((int)target.Handle, address, buffer, buffer.Length, ref bytesRead);
            if (!success)
            {
                throw new MemoryReadException("CRITICAL ERROR: Could not read memory at 0x" + address.ToString("x") + "!");
            }
            return buffer;
        }

        public unsafe T ReadFromMemory<T>(long address) where T : unmanaged
        {
            AssertProcessAttached();
            AssertReadPermission();
            int bytesRead = 0;
            byte[] buffer = new byte[sizeof(T)];
            bool success = WinAPI.ReadProcessMemory((int)target.Handle, address, buffer, buffer.Length, ref bytesRead);
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

        public float ReadFloatFromMemory(long address)
        {
            AssertProcessAttached();
            AssertReadPermission();
            int bytesRead = 0;
            byte[] buffer = new byte[4];

            bool success = WinAPI.ReadProcessMemory((int)target.Handle, address, buffer, buffer.Length, ref bytesRead);
            if (!success)
            {
                throw new MemoryReadException("CRITICAL ERROR: Could not read memory at 0x" + address.ToString("x") + "!");
            }
            return BitConverter.ToSingle(buffer, 0);
        }

        public string ReadStringFromMemory(long address)
        {
            AssertProcessAttached();
            AssertReadPermission();
            StringBuilder builder = new StringBuilder();
            int bytesRead = 0;
            byte[] buffer = new byte[1];
            buffer[0] = 1;
            while (!buffer[0].IsNULL())
            {
                bool success = WinAPI.ReadProcessMemory((int)target.Handle, address, buffer, buffer.Length, ref bytesRead);
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
            AssertProcessAttached();
            AssertWritePermission();
            int bytesWritten = 0;
            byte[] buffer = value.GetBytes();

            bool success = WinAPI.WriteProcessMemory((int)target.Handle, address, buffer, buffer.Length, ref bytesWritten);
            if (!success)
            {
                throw new MemoryWriteException("CRITICAL ERROR: Could not write to memory at 0x" + address.ToString("x") + "!");
            }
        }

        public void WriteStringToMemory(long address, string value, Encoding encoding)
        {
            AssertProcessAttached();
            AssertWritePermission();
            int bytesWritten = 0;
            byte[] buffer = encoding.GetBytes(value);

            bool success = WinAPI.WriteProcessMemory((int)target.Handle, address, buffer, buffer.Length, ref bytesWritten);
            if (!success)
            {
                throw new MemoryWriteException("CRITICAL ERROR: Could not write to memory at 0x" + address.ToString("x") + "!");
            }
        }
        #endregion
        private void AssertProcessAttached()
        {
            if (target.Handle == IntPtr.Zero)
            {
                throw new ProcessNotAttachedException("Not attached to any process.");
            }
        }
        private void AssertWritePermission()
        {
            if (!target.HasProcessPermission(Permissions.ProcessPermission.PROCESS_VM_WRITE))
            {
                throw new UnauthorizedAccessException("Cannot write process memory: missing permission 'PROCESS_VM_WRITE'");
            }
        }
        private void AssertReadPermission()
        {
            if (!target.HasProcessPermission(Permissions.ProcessPermission.PROCESS_VM_READ))
            {
                throw new UnauthorizedAccessException("Cannot read process memory: missing permission 'PROCESS_VM_READ'");
            }
        }
    }
}
