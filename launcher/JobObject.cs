using System;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace LunarLauncher
{
    public sealed class JobObject : IDisposable
    {
        private IntPtr _handle;
        private bool _disposed;

        public IntPtr Handle => _handle;

        public JobObject()
        {
            // Create unnamed job object; non-inheritable handle
            _handle = NativeMethods.CreateJobObjectW(IntPtr.Zero, null);
            if (_handle == IntPtr.Zero)
            {
                throw new Win32Exception(Marshal.GetLastWin32Error(), "Failed to create Windows Job Object.");
            }

            var info = new NativeMethods.JOBOBJECT_EXTENDED_LIMIT_INFORMATION();
            info.BasicLimitInformation.LimitFlags = NativeMethods.JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE;

            int length = Marshal.SizeOf(typeof(NativeMethods.JOBOBJECT_EXTENDED_LIMIT_INFORMATION));
            IntPtr extendedInfoPtr = Marshal.AllocHGlobal(length);
            try
            {
                Marshal.StructureToPtr(info, extendedInfoPtr, false);
                if (!NativeMethods.SetInformationJobObject(
                    _handle,
                    NativeMethods.JobObjectExtendedLimitInformation,
                    extendedInfoPtr,
                    (uint)length))
                {
                    int err = Marshal.GetLastWin32Error();
                    NativeMethods.CloseHandle(_handle);
                    _handle = IntPtr.Zero;
                    throw new Win32Exception(err, $"Failed to set KILL_ON_JOB_CLOSE on Job Object (error {err}).");
                }
            }
            finally
            {
                Marshal.FreeHGlobal(extendedInfoPtr);
            }
        }

        public void AssignProcess(IntPtr hProcess)
        {
            if (_disposed) throw new ObjectDisposedException(nameof(JobObject));

            if (!NativeMethods.AssignProcessToJobObject(_handle, hProcess))
            {
                int err = Marshal.GetLastWin32Error();
                throw new Win32Exception(err, $"Failed to assign process to Job Object (error {err}).");
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
                if (_handle != IntPtr.Zero)
                {
                    NativeMethods.CloseHandle(_handle);
                    _handle = IntPtr.Zero;
                }
            }
        }
    }
}
