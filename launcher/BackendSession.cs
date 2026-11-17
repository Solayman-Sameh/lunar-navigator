using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Web.Script.Serialization;

namespace LunarLauncher
{
    public class StartupDescriptor
    {
        public string status;
        public string service_identity;
        public string api_profile;
        public string host;
        public int port;
        public int pid;
        public string session_id;
        public string timestamp_utc;
    }

    public class HealthResponse
    {
        public string status;
        public string api_profile;
        public string service_identity;
        public int pid;
        public string session_id;
        public string data_status;
    }

    public class BackendSession
    {
        public string SessionId { get; }
        public string LocalAppDataRoot { get; }
        public string DataDir { get; }
        public string RuntimeDir { get; }
        public string LogsDir { get; }
        public string StartupFilePath { get; }
        public string BackendLogPath { get; }
        public string LauncherLogPath { get; }
        public string PlayerLogPath { get; }
        public string ShutdownEventName { get; }

        public int ActualPort { get; private set; }
        public int BackendPid { get; private set; }

        public BackendSession()
        {
            SessionId = Guid.NewGuid().ToString("N");
            string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            LocalAppDataRoot = Path.Combine(localAppData, "LunarNavigator");

            DataDir = Path.Combine(LocalAppDataRoot, "data");
            RuntimeDir = Path.Combine(LocalAppDataRoot, "runtime", SessionId);
            LogsDir = Path.Combine(LocalAppDataRoot, "logs", SessionId);

            StartupFilePath = Path.Combine(RuntimeDir, "startup.json");
            BackendLogPath = Path.Combine(LogsDir, "backend.log");
            LauncherLogPath = Path.Combine(LogsDir, "launcher.log");
            PlayerLogPath = Path.Combine(LogsDir, "Player.log");

            ShutdownEventName = $"Local\\LunarNavigator_Shutdown_{SessionId}";

            Directory.CreateDirectory(DataDir);
            Directory.CreateDirectory(RuntimeDir);
            Directory.CreateDirectory(LogsDir);
        }

        public void Log(string message)
        {
            try
            {
                string line = $"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss.fffZ}] {message}{Environment.NewLine}";
                File.AppendAllText(LauncherLogPath, line, Encoding.UTF8);
            }
            catch
            {
                // Silently ignore logging failures
            }
        }

        public StartupDescriptor WaitForStartupDescriptor(IntPtr hBackendProcess, int timeoutSeconds = 15)
        {
            Log($"Waiting up to {timeoutSeconds}s for startup descriptor: {StartupFilePath}");
            DateTime deadline = DateTime.UtcNow.AddSeconds(timeoutSeconds);

            while (DateTime.UtcNow < deadline)
            {
                if (!NativeMethods.GetExitCodeProcess(hBackendProcess, out uint exitCode) || exitCode != NativeMethods.STILL_ACTIVE)
                {
                    Log($"Backend process exited prematurely with code {exitCode} while waiting for startup descriptor.");
                    throw new InvalidOperationException($"Backend process exited unexpectedly with code {exitCode}.");
                }

                if (File.Exists(StartupFilePath))
                {
                    try
                    {
                        string json = File.ReadAllText(StartupFilePath, Encoding.UTF8);
                        if (!string.IsNullOrWhiteSpace(json))
                        {
                            var desc = ParseStartupDescriptor(json, NativeMethods.GetProcessId(hBackendProcess), SessionId);
                            if (desc != null && desc.port > 0 && desc.pid > 0)
                            {
                                ActualPort = desc.port;
                                BackendPid = desc.pid;
                                Log($"Startup descriptor received: Port {ActualPort}, PID {BackendPid}, Session {desc.session_id}");
                                return desc;
                            }
                        }
                    }
                    catch (IOException)
                    {
                        // File may be in the middle of being written or replaced
                    }
                }

                Thread.Sleep(100);
            }

            Log("Timed out waiting for backend startup descriptor.");
            throw new TimeoutException($"Timed out after {timeoutSeconds}s waiting for backend startup descriptor at {StartupFilePath}.");
        }

        public bool VerifyBackendHealth(IntPtr hBackendProcess, int timeoutSeconds = 30)
        {
            if (ActualPort <= 0)
            {
                throw new InvalidOperationException("Cannot verify health without an established port.");
            }

            string healthUrl = $"http://127.0.0.1:{ActualPort}/health";
            Log($"Verifying backend readiness via GET {healthUrl} (deadline {timeoutSeconds}s)");
            DateTime deadline = DateTime.UtcNow.AddSeconds(timeoutSeconds);

            while (DateTime.UtcNow < deadline)
            {
                if (!NativeMethods.GetExitCodeProcess(hBackendProcess, out uint exitCode) || exitCode != NativeMethods.STILL_ACTIVE)
                {
                    Log($"Backend process exited prematurely with code {exitCode} during health probe.");
                    return false;
                }

                try
                {
                    var req = (HttpWebRequest)WebRequest.Create(healthUrl);
                    req.Method = "GET";
                    req.Timeout = 2000;
                    req.ReadWriteTimeout = 2000;
                    req.Proxy = null; // Strictly no proxy for loopback
                    req.AllowAutoRedirect = false;

                    using (var resp = (HttpWebResponse)req.GetResponse())
                    {
                        if (resp.StatusCode == HttpStatusCode.OK)
                        {
                            using (var reader = new StreamReader(resp.GetResponseStream(), Encoding.UTF8))
                            {
                                string body = reader.ReadToEnd();
                                var health = ParseHealthResponse(body);
                                if (health != null &&
                                    health.status == "ok" &&
                                    health.pid == BackendPid &&
                                    health.session_id == SessionId &&
                                    health.service_identity == "LunarNavigator-ScienceAPI" &&
                                    health.api_profile == "lunar-analyze-v1")
                                {
                                    Log($"Backend verified healthy: PID={health.pid}, Status={health.status}, Session={health.session_id}");
                                    return true;
                                }
                                else
                                {
                                    Log($"Health probe returned unexpected payload: {body}");
                                }
                            }
                        }
                    }
                }
                catch (WebException)
                {
                    // Backend may still be spinning up Uvicorn on the socket
                }
                catch (Exception ex)
                {
                    Log($"Exception during health probe: {ex.Message}");
                }

                Thread.Sleep(200);
            }

            Log($"Backend health verification timed out after {timeoutSeconds}s.");
            return false;
        }

        public void CleanRuntimeDirectory()
        {
            try
            {
                if (Directory.Exists(RuntimeDir))
                {
                    Directory.Delete(RuntimeDir, true);
                    Log($"Cleaned session runtime directory: {RuntimeDir}");
                }
            }
            catch (Exception ex)
            {
                Log($"Failed to delete session runtime directory: {ex.Message}");
            }
        }

        public static StartupDescriptor ParseStartupDescriptor(string json, uint expectedPid, string expectedSession)
        {
            var desc = new JavaScriptSerializer().Deserialize<StartupDescriptor>(json);
            if (desc == null || expectedPid == 0 || desc.pid != expectedPid ||
                desc.session_id != expectedSession || desc.host != "127.0.0.1" ||
                desc.port < 1 || desc.port > 65535 || desc.status != "ready_to_listen" ||
                desc.service_identity != "LunarNavigator-ScienceAPI" || desc.api_profile != "lunar-analyze-v1")
                throw new InvalidDataException("Backend startup descriptor does not match the owned process/session.");
            return desc;
        }

        private static HealthResponse ParseHealthResponse(string json)
        {
            return new JavaScriptSerializer().Deserialize<HealthResponse>(json);
        }

    }
}
