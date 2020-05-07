using System;

namespace InteropMgr
{
    public class InterOperationException : InvalidOperationException
    {
        public InterOperationException() { }
        public InterOperationException(string message) : base(message) { }
    }

    public class MemoryReadException : AccessViolationException
    {
        public MemoryReadException() { }
        public MemoryReadException(string message) : base(message) { }
    }

    public class MemoryWriteException : AccessViolationException
    {
        public MemoryWriteException() { }
        public MemoryWriteException(string message) : base(message) { }
    }

    public class ProcessEnumerationException : Exception
    {
        public ProcessEnumerationException() { }
        public ProcessEnumerationException(string message) : base(message) { }
    }

    public class ProcessNotAttachedException : Exception
    {
        public ProcessNotAttachedException() { }
        public ProcessNotAttachedException(string message) : base(message) { }
    }

    public class ModuleNotFoundException : Exception
    {
        public ModuleNotFoundException() { }
        public ModuleNotFoundException(string message) : base(message) { }
    }

    public class TypeMismatchException : TypeAccessException
    {
        public TypeMismatchException() { }
        public TypeMismatchException(string message) : base(message) { }
    }
}
