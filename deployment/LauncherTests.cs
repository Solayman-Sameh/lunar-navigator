using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading;
using LunarLauncher;

public static class LauncherTests
{
    static int checks;
    static void Check(bool ok, string name)
    {
        if (!ok) throw new Exception(name);
        checks++; Console.WriteLine("PASS " + name);
    }
    static string Descriptor(int pid, string session, int port = 12345)
    {
        return new System.Web.Script.Serialization.JavaScriptSerializer().Serialize(new StartupDescriptor {
            pid = pid, session_id = session, host = "127.0.0.1", port = port,
            status = "ready_to_listen", service_identity = "LunarNavigator-ScienceAPI", api_profile = "lunar-analyze-v1" });
    }
    static void Reject(string json, uint pid, string session, string name)
    {
        bool rejected = false;
        try { BackendSession.ParseStartupDescriptor(json, pid, session); }
        catch { rejected = true; }
        Check(rejected, name);
    }
    public static int Main(string[] args)
    {
        if (args.Length > 0 && args[0] == "--child") { Thread.Sleep(60000); return 0; }
        if (args.Length > 0 && args[0] == "--echo") { File.WriteAllText(args[1], args[2]); return 0; }
        if (args.Length > 0 && args[0] == "--controller") {
            using (var job = new JobObject()) {
                var method = typeof(Program).GetMethod("Spawn", BindingFlags.NonPublic | BindingFlags.Static);
                var child = (NativeMethods.PROCESS_INFORMATION)method.Invoke(null, new object[] {
                    job, Assembly.GetExecutingAssembly().Location, "--child", true });
                File.WriteAllText(args[1], child.dwProcessId.ToString());
                Thread.Sleep(60000);
            }
            return 0;
        }
        var session = "escaped-\"-session";
        Check(BackendSession.ParseStartupDescriptor(Descriptor(123, session), 123, session).pid == 123, "typed JSON escaped values");
        Reject(Descriptor(456, session), 123, session, "reject descriptor PID unrelated to owned child");
        Reject(Descriptor(123, "other"), 123, session, "reject wrong session");
        Reject(Descriptor(123, session, 65536), 123, session, "reject invalid port");
        Reject("{not json}", 123, session, "reject malformed JSON");
        Reject("{\"nested\":" + Descriptor(123, session) + "}", 123, session, "reject nested spoofed fields");
        Check(Program.MutexName == "Local\\LunarNavigator_SingleInstance_LunarNavigator", "installer mutex contract");
        string exe = Assembly.GetExecutingAssembly().Location;
        string file = Path.GetTempFileName();
        try {
            string value = "space and quote \" and trailing slash\\";
            using (var p = Process.Start(new ProcessStartInfo(exe, "--echo " + Program.Quote(file) + " " + Program.Quote(value)) { UseShellExecute = false, CreateNoWindow = true })) {
                Check(p.WaitForExit(5000), "argument echo exits");
            }
            Check(File.ReadAllText(file) == value, "Windows argument quoting roundtrip");
        } finally { File.Delete(file); }
        // Exercise the production suspended-spawn helper and kernel kill-on-close.
        var spawn = typeof(Program).GetMethod("Spawn", BindingFlags.NonPublic | BindingFlags.Static);
        NativeMethods.PROCESS_INFORMATION proc;
        using (var job = new JobObject()) {
            proc = (NativeMethods.PROCESS_INFORMATION)spawn.Invoke(null, new object[] { job, exe, "--child", true });
            Check(NativeMethods.WaitForSingleObject(proc.hProcess, 100) != 0, "owned child runs before job closes");
        }
        try { Check(NativeMethods.WaitForSingleObject(proc.hProcess, 5000) == 0, "job close kills owned child"); }
        finally { NativeMethods.CloseHandle(proc.hProcess); NativeMethods.CloseHandle(proc.hThread); }
        file = Path.GetTempFileName();
        Process controller = null;
        Process descendant = null;
        try {
            controller = Process.Start(new ProcessStartInfo(exe, "--controller " + Program.Quote(file)) { UseShellExecute = false, CreateNoWindow = true });
            var deadline = Stopwatch.StartNew();
            while (File.ReadAllText(file).Length == 0 && deadline.ElapsedMilliseconds < 5000) Thread.Sleep(50);
            descendant = Process.GetProcessById(int.Parse(File.ReadAllText(file)));
            controller.Kill();
            Check(controller.WaitForExit(5000), "forced controller termination");
            Check(descendant.WaitForExit(5000), "kernel kills child when controller crashes");
        } finally {
            if (controller != null) { if (!controller.HasExited) controller.Kill(); controller.Dispose(); }
            if (descendant != null) { if (!descendant.HasExited) descendant.Kill(); descendant.Dispose(); }
            File.Delete(file);
        }
        Console.WriteLine(checks + " launcher regression checks passed");
        return 0;
    }
}
