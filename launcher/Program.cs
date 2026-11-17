using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace LunarLauncher
{
    internal static class Program
    {
        internal const string MutexName = "Local\\LunarNavigator_SingleInstance_LunarNavigator";

        [STAThread]
        private static int Main(string[] args)
        {
            try
            {
                bool created;
                using (var mutex = new Mutex(true, MutexName, out created))
                {
                    if (!created)
                    {
                        MessageBox.Show("LunarNavigator is already running or initializing in this Windows session.", "LunarNavigator");
                        return 0;
                    }
                    try { Run(); }
                    finally { mutex.ReleaseMutex(); }
                }
                return 0;
            }
            catch (Exception ex)
            {
                // Run has already cleaned up the owned session before showing a modal dialog.
                MessageBox.Show(ex.Message, "LunarNavigator", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return 1;
            }
        }

        private static NativeMethods.PROCESS_INFORMATION Spawn(JobObject job, string exe, string args, bool hidden)
        {
            var si = new NativeMethods.STARTUPINFO();
            si.cb = Marshal.SizeOf(typeof(NativeMethods.STARTUPINFO));
            NativeMethods.PROCESS_INFORMATION proc;
            uint flags = NativeMethods.CREATE_SUSPENDED | (hidden ? NativeMethods.CREATE_NO_WINDOW : 0);
            if (!NativeMethods.CreateProcessW(exe, Quote(exe) + " " + args, IntPtr.Zero, IntPtr.Zero,
                    false, flags, IntPtr.Zero, Path.GetDirectoryName(exe), ref si, out proc))
                throw new Win32Exception(Marshal.GetLastWin32Error(), "Could not start " + exe);
            try
            {
                job.AssignProcess(proc.hProcess);
                if (NativeMethods.ResumeThread(proc.hThread) == uint.MaxValue)
                    throw new Win32Exception(Marshal.GetLastWin32Error(), "Could not resume owned child.");
                return proc;
            }
            catch
            {
                NativeMethods.TerminateProcess(proc.hProcess, 1);
                NativeMethods.WaitForSingleObject(proc.hProcess, 5000);
                Close(proc);
                throw;
            }
        }

        internal static string Quote(string value)
        {
            var result = new System.Text.StringBuilder("\"");
            int slashes = 0;
            foreach (char c in value)
            {
                if (c == '\\') { slashes++; continue; }
                result.Append('\\', c == '"' ? slashes * 2 + 1 : slashes);
                result.Append(c);
                slashes = 0;
            }
            result.Append('\\', slashes * 2);
            return result.Append('"').ToString();
        }

        private static void Run()
        {
            var session = new BackendSession();
            JobObject job = null;
            IntPtr shutdown = IntPtr.Zero;
            NativeMethods.PROCESS_INFORMATION backend = default;
            NativeMethods.PROCESS_INFORMATION unity = default;
            try
            {
                string root = AppDomain.CurrentDomain.BaseDirectory;
                string python = Path.Combine(root, "backend", "python.exe");
                string script = Path.Combine(root, "backend", "run_backend.py");
                string player = Path.Combine(root, "bin", "LunarNavigator.exe");
                foreach (string path in new[] { python, script, player })
                    if (!File.Exists(path)) throw new FileNotFoundException("Installation incomplete. Please reinstall LunarNavigator.", path);
                job = new JobObject();
                shutdown = NativeMethods.CreateEventW(IntPtr.Zero, true, false, session.ShutdownEventName);
                if (shutdown == IntPtr.Zero) throw new Win32Exception(Marshal.GetLastWin32Error());
                string args = Quote(script) + " --host 127.0.0.1 --port 0 --storage-dir " + Quote(session.DataDir) +
                    " --session-id " + Quote(session.SessionId) + " --startup-file " + Quote(session.StartupFilePath) +
                    " --log-file " + Quote(session.BackendLogPath) + " --shutdown-event " + Quote(session.ShutdownEventName);
                backend = Spawn(job, python, args, true);
                session.Log("Owned backend PID " + backend.dwProcessId);
                var startup = System.Diagnostics.Stopwatch.StartNew();
                session.WaitForStartupDescriptor(backend.hProcess, 30);
                int remaining = Math.Max(1, 30 - (int)startup.Elapsed.TotalSeconds);
                if (!session.VerifyBackendHealth(backend.hProcess, remaining))
                    throw new InvalidOperationException("Scientific backend did not become ready. Unity was not started.");
                unity = Spawn(job, player, "--backend-url " + Quote("http://127.0.0.1:" + session.ActualPort) +
                    " -logFile " + Quote(session.PlayerLogPath), false);
                session.Log("Owned Unity PID " + unity.dwProcessId);
                while (true)
                {
                    uint result = NativeMethods.WaitForSingleObject(unity.hProcess, 250);
                    if (result == NativeMethods.WAIT_OBJECT_0) break;
                    if (result == NativeMethods.WAIT_FAILED) throw new Win32Exception(Marshal.GetLastWin32Error());
                    if (NativeMethods.WaitForSingleObject(backend.hProcess, 0) == NativeMethods.WAIT_OBJECT_0)
                        throw new InvalidOperationException("Scientific backend stopped unexpectedly. The application session has been closed.");
                }
            }
            catch (Exception ex)
            {
                session.Log(ex.ToString());
                throw new InvalidOperationException(ex.Message + "\n\nLogs: " + session.LogsDir, ex);
            }
            finally
            {
                if (backend.hProcess != IntPtr.Zero && shutdown != IntPtr.Zero)
                {
                    NativeMethods.SetEvent(shutdown);
                    NativeMethods.WaitForSingleObject(backend.hProcess, 3000);
                }
                if (job != null) job.Dispose();
                foreach (var child in new[] { backend, unity })
                {
                    if (child.hProcess != IntPtr.Zero) NativeMethods.WaitForSingleObject(child.hProcess, 5000);
                    Close(child);
                }
                if (shutdown != IntPtr.Zero) NativeMethods.CloseHandle(shutdown);
                session.CleanRuntimeDirectory();
                session.Log("Owned session cleaned up.");
            }
        }

        private static void Close(NativeMethods.PROCESS_INFORMATION proc)
        {
            if (proc.hThread != IntPtr.Zero) NativeMethods.CloseHandle(proc.hThread);
            if (proc.hProcess != IntPtr.Zero) NativeMethods.CloseHandle(proc.hProcess);
        }
    }
}
