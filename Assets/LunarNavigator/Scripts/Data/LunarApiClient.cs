using System;
using System.Collections;
using System.Globalization;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace LunarNavigator.Data
{
    [Serializable]
    public class AnalyzeRequestPayload
    {
        public string site_id;
        [NonSerialized] public double? latitude_deg;
        [NonSerialized] public double? longitude_deg;
        public string landing_utc;
        public double mission_duration_h;
        public double observer_height_m = 2.0;
        public int sampling_step_minutes = 10;

        /// <summary>
        /// Serializes payload to strict JSON conforming to FastAPI AnalyzeRequest schema.
        /// Enforces mutual exclusivity: site_id OR (latitude_deg AND longitude_deg), never both.
        /// Forbids extra fields.
        /// </summary>
        public string ToJson()
        {
            var sb = new StringBuilder();
            sb.Append("{");

            // Mutual exclusivity per backend/science/api.py validate_mission
            if (!string.IsNullOrEmpty(site_id))
            {
                sb.AppendFormat(CultureInfo.InvariantCulture, "\"site_id\":\"{0}\",", site_id);
            }
            else if (latitude_deg.HasValue && longitude_deg.HasValue)
            {
                sb.AppendFormat(CultureInfo.InvariantCulture, "\"latitude_deg\":{0:R},\"longitude_deg\":{1:R},",
                    latitude_deg.Value, longitude_deg.Value);
            }

            sb.AppendFormat(CultureInfo.InvariantCulture, "\"landing_utc\":\"{0}\",", landing_utc);
            sb.AppendFormat(CultureInfo.InvariantCulture, "\"mission_duration_h\":{0:R},", mission_duration_h);
            sb.AppendFormat(CultureInfo.InvariantCulture, "\"observer_height_m\":{0:R},", observer_height_m);
            sb.AppendFormat(CultureInfo.InvariantCulture, "\"sampling_step_minutes\":{0}", sampling_step_minutes);
            sb.Append("}");
            return sb.ToString();
        }
    }

    public class LunarApiClient : MonoBehaviour
    {
        private static LunarApiClient _instance;
        public static LunarApiClient Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = GameObject.Find("LunarApiClient");
                    if (go == null)
                    {
                        go = new GameObject("LunarApiClient");
                        if (Application.isPlaying) DontDestroyOnLoad(go);
                    }
                    _instance = go.GetComponent<LunarApiClient>() ?? go.AddComponent<LunarApiClient>();
                    _instance.InitializeConfiguration();
                }
                return _instance;
            }
        }

        [Header("API Configuration")]
        [Tooltip("Configurable base URL for FastAPI scientific backend (default: http://localhost:8000)")]
        public string baseUrl = "http://localhost:8000";

        [Tooltip("Legacy fallback timeout in seconds")]
        public int timeoutSeconds = 30;

        [Tooltip("Timeout in seconds for Site Inspector /analyze requests")]
        public int analyzeTimeoutSeconds = 25;

        [Tooltip("Timeout in seconds for Opportunity Finder /opportunities requests (supports uncached multi-site scans)")]
        public int opportunitiesTimeoutSeconds = 90;

        private UnityWebRequest activeRequest;
        private Coroutine activeCoroutine;
        private int currentAnalyzeToken = 0;
        private int currentOpportunitiesToken = 0;

        public bool IsBusy => activeRequest != null && !activeRequest.isDone;

        private bool configurationInitialized = false;
        public string ConfigurationError { get; private set; }

        public static bool TryParseAndValidateBackendUrl(string rawUrl, out string validUrl, out string errorMessage)
        {
            validUrl = null;
            errorMessage = null;

            if (string.IsNullOrWhiteSpace(rawUrl))
            {
                errorMessage = "Backend URL override cannot be empty or whitespace.";
                return false;
            }

            rawUrl = rawUrl.Trim();

            if (!Uri.TryCreate(rawUrl, UriKind.Absolute, out Uri uri))
            {
                errorMessage = $"Invalid URL format: '{rawUrl}'. Must be an absolute URL (e.g., http://127.0.0.1:8000).";
                return false;
            }

            if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
            {
                errorMessage = $"Unsupported scheme '{uri.Scheme}' in '{rawUrl}'. Only HTTP (or HTTPS) is supported.";
                return false;
            }

            if (uri.Port <= 0 || uri.Port > 65535)
            {
                errorMessage = $"Invalid or missing explicit port in '{rawUrl}'. Port must be between 1 and 65535.";
                return false;
            }

            if (!string.IsNullOrEmpty(uri.UserInfo))
            {
                errorMessage = $"Credentials (user:pass) are not permitted in backend URL '{rawUrl}'.";
                return false;
            }

            if (!string.IsNullOrEmpty(uri.Query))
            {
                errorMessage = $"Query parameters are not permitted in backend URL '{rawUrl}'.";
                return false;
            }

            if (!string.IsNullOrEmpty(uri.Fragment))
            {
                errorMessage = $"URL fragments are not permitted in backend URL '{rawUrl}'.";
                return false;
            }

            string host = uri.DnsSafeHost;
            bool isLoopback = uri.IsLoopback ||
                              string.Equals(host, "localhost", StringComparison.OrdinalIgnoreCase) ||
                              string.Equals(host, "127.0.0.1", StringComparison.Ordinal) ||
                              string.Equals(host, "::1", StringComparison.Ordinal);

            if (!isLoopback)
            {
                errorMessage = $"Host '{host}' is not a local loopback address. For security and launcher-managed deployment, host must be 127.0.0.1 or localhost.";
                return false;
            }

            validUrl = $"{uri.Scheme}://{uri.Host}:{uri.Port}";
            return true;
        }

        public void InitializeConfiguration()
        {
            if (configurationInitialized) return;
            configurationInitialized = true;

            string explicitOverride = null;
            string overrideSource = null;

            // 1. Explicit command line arguments: --backend-url <url> or -backendUrl <url>
            try
            {
                string[] args = System.Environment.GetCommandLineArgs();
                if (args != null)
                {
                    for (int i = 0; i < args.Length; i++)
                    {
                        if (i < args.Length - 1 &&
                            (string.Equals(args[i], "--backend-url", StringComparison.OrdinalIgnoreCase) ||
                             string.Equals(args[i], "-backendUrl", StringComparison.OrdinalIgnoreCase)))
                        {
                            explicitOverride = args[i + 1];
                            overrideSource = args[i];
                            break;
                        }
                        else if (args[i].StartsWith("--backend-url=", StringComparison.OrdinalIgnoreCase))
                        {
                            explicitOverride = args[i].Substring("--backend-url=".Length);
                            overrideSource = "--backend-url=";
                            break;
                        }
                        else if (args[i].StartsWith("-backendUrl=", StringComparison.OrdinalIgnoreCase))
                        {
                            explicitOverride = args[i].Substring("-backendUrl=".Length);
                            overrideSource = "-backendUrl=";
                            break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[LunarApiClient] Exception reading command line arguments: " + ex.Message);
            }

            // 2. LUNAR_BACKEND_URL environment variable
            if (string.IsNullOrEmpty(explicitOverride))
            {
                try
                {
                    string envUrl = System.Environment.GetEnvironmentVariable("LUNAR_BACKEND_URL");
                    if (!string.IsNullOrEmpty(envUrl))
                    {
                        explicitOverride = envUrl;
                        overrideSource = "LUNAR_BACKEND_URL environment variable";
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning("[LunarApiClient] Exception reading environment variable: " + ex.Message);
                }
            }

            // 3. Apply or validate
            if (!string.IsNullOrEmpty(explicitOverride))
            {
                if (TryParseAndValidateBackendUrl(explicitOverride, out string validUrl, out string err))
                {
                    baseUrl = validUrl;
                    Debug.Log($"[LunarApiClient] Backend URL configured from {overrideSource}: {baseUrl}");
                }
                else
                {
                    ConfigurationError = $"Configuration Error: Invalid backend URL override via {overrideSource} ('{explicitOverride}'): {err}";
                    Debug.LogError("[LunarApiClient] " + ConfigurationError);
                    baseUrl = "http://invalid-backend-config.local:0";
                }
            }
            else
            {
                if (string.IsNullOrEmpty(baseUrl))
                {
                    baseUrl = "http://localhost:8000";
                }
            }
        }

        private void Awake()
        {
            InitializeConfiguration();
            if (_instance == null)
            {
                _instance = this;
                if (Application.isPlaying) DontDestroyOnLoad(gameObject);
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// Cancels any currently pending HTTP request immediately and invalidates pending response callbacks.
        /// </summary>
        public void CancelCurrentRequest()
        {
            currentAnalyzeToken++;
            currentOpportunitiesToken++;

            if (activeRequest != null)
            {
                try
                {
                    activeRequest.Abort();
                }
                catch (Exception ex)
                {
                    Debug.LogWarning("[LunarApiClient] Abort exception: " + ex.Message);
                }
                activeRequest = null;
            }

            if (activeCoroutine != null)
            {
                StopCoroutine(activeCoroutine);
                activeCoroutine = null;
            }
        }

        /// <summary>
        /// Probes GET /health on the scientific API backend.
        /// </summary>
        public void CheckHealth(Action<bool, string> onComplete)
        {
            StartCoroutine(DoCheckHealth(onComplete));
        }

        private IEnumerator DoCheckHealth(Action<bool, string> onComplete)
        {
            if (!string.IsNullOrEmpty(ConfigurationError))
            {
                onComplete?.Invoke(false, ConfigurationError);
                yield break;
            }
            string url = baseUrl.TrimEnd('/') + "/health";
            using var uwr = UnityWebRequest.Get(url);
            uwr.timeout = 5;

            yield return uwr.SendWebRequest();

            if (uwr.result == UnityWebRequest.Result.Success)
            {
                onComplete?.Invoke(true, uwr.downloadHandler.text);
            }
            else
            {
                string err = $"Cannot connect to scientific API at {baseUrl}: {uwr.error}";
                onComplete?.Invoke(false, err);
            }
        }

        /// <summary>
        /// Queries GET /sites on the FastAPI scientific backend.
        /// </summary>
        public void GetSites(Action<bool, string, string> onComplete)
        {
            StartCoroutine(DoGetSites(onComplete));
        }

        private IEnumerator DoGetSites(Action<bool, string, string> onComplete)
        {
            if (!string.IsNullOrEmpty(ConfigurationError))
            {
                onComplete?.Invoke(false, null, ConfigurationError);
                yield break;
            }
            string url = baseUrl.TrimEnd('/') + "/sites";
            using var uwr = UnityWebRequest.Get(url);
            uwr.timeout = 10;
            yield return uwr.SendWebRequest();

            if (uwr.result == UnityWebRequest.Result.Success)
            {
                onComplete?.Invoke(true, uwr.downloadHandler.text, null);
            }
            else
            {
                string rawError = uwr.downloadHandler != null ? uwr.downloadHandler.text : null;
                string userFriendlyError = FormatError(uwr.responseCode, uwr.error, rawError);
                onComplete?.Invoke(false, rawError, userFriendlyError);
            }
        }

        /// <summary>
        /// Sends an asynchronous POST /analyze request to the FastAPI scientific backend.
        /// </summary>
        /// <param name="payload">Validated request payload.</param>
        /// <param name="onComplete">Callback returning (success, jsonResponse, errorMessage).</param>
        public void PostAnalyze(AnalyzeRequestPayload payload, Action<bool, string, string> onComplete)
        {
            CancelCurrentRequest();
            activeCoroutine = StartCoroutine(DoPostAnalyze(payload, onComplete));
        }

        private IEnumerator DoPostAnalyze(AnalyzeRequestPayload payload, Action<bool, string, string> onComplete)
        {
            if (!string.IsNullOrEmpty(ConfigurationError))
            {
                onComplete?.Invoke(false, null, ConfigurationError);
                yield break;
            }
            int token = ++currentAnalyzeToken;
            string endpoint = baseUrl.TrimEnd('/') + "/analyze";
            string jsonBody = payload.ToJson();

            byte[] rawBytes = Encoding.UTF8.GetBytes(jsonBody);
            var uwr = new UnityWebRequest(endpoint, UnityWebRequest.kHttpVerbPOST)
            {
                uploadHandler = new UploadHandlerRaw(rawBytes),
                downloadHandler = new DownloadHandlerBuffer(),
                timeout = analyzeTimeoutSeconds
            };
            uwr.SetRequestHeader("Content-Type", "application/json");
            uwr.SetRequestHeader("Accept", "application/json");

            activeRequest = uwr;

            yield return uwr.SendWebRequest();

            if (token != currentAnalyzeToken || activeRequest != uwr)
            {
                // Request was canceled or superseded by a newer request
                onComplete?.Invoke(false, null, "Analysis request was canceled.");
                yield break;
            }

            activeRequest = null;
            activeCoroutine = null;

            if (uwr.result == UnityWebRequest.Result.Success)
            {
                string responseText = uwr.downloadHandler.text;
                onComplete?.Invoke(true, responseText, null);
            }
            else
            {
                string rawError = uwr.downloadHandler != null ? uwr.downloadHandler.text : null;
                string userFriendlyError = FormatError(uwr.responseCode, uwr.error, rawError, analyzeTimeoutSeconds);
                onComplete?.Invoke(false, rawError, userFriendlyError);
            }

            uwr.Dispose();
        }

        /// <summary>
        /// Sends an asynchronous POST /opportunities request to the FastAPI scientific backend.
        /// </summary>
        public void PostOpportunities(string jsonPayload, Action<bool, string, string> onComplete)
        {
            CancelCurrentRequest();
            activeCoroutine = StartCoroutine(DoPostOpportunities(jsonPayload, onComplete));
        }

        private IEnumerator DoPostOpportunities(string jsonPayload, Action<bool, string, string> onComplete)
        {
            if (!string.IsNullOrEmpty(ConfigurationError))
            {
                onComplete?.Invoke(false, null, ConfigurationError);
                yield break;
            }
            int token = ++currentOpportunitiesToken;
            string endpoint = baseUrl.TrimEnd('/') + "/opportunities";
            byte[] rawBytes = Encoding.UTF8.GetBytes(jsonPayload);
            var uwr = new UnityWebRequest(endpoint, UnityWebRequest.kHttpVerbPOST)
            {
                uploadHandler = new UploadHandlerRaw(rawBytes),
                downloadHandler = new DownloadHandlerBuffer(),
                timeout = opportunitiesTimeoutSeconds
            };
            uwr.SetRequestHeader("Content-Type", "application/json");
            uwr.SetRequestHeader("Accept", "application/json");

            activeRequest = uwr;

            yield return uwr.SendWebRequest();

            if (token != currentOpportunitiesToken || activeRequest != uwr)
            {
                onComplete?.Invoke(false, null, "Opportunity search request was canceled.");
                yield break;
            }

            activeRequest = null;
            activeCoroutine = null;

            if (uwr.result == UnityWebRequest.Result.Success)
            {
                string responseText = uwr.downloadHandler.text;
                onComplete?.Invoke(true, responseText, null);
            }
            else
            {
                string rawError = uwr.downloadHandler != null ? uwr.downloadHandler.text : null;
                string userFriendlyError = FormatError(uwr.responseCode, uwr.error, rawError, opportunitiesTimeoutSeconds);
                onComplete?.Invoke(false, rawError, userFriendlyError);
            }

            uwr.Dispose();
        }

        private string FormatError(long statusCode, string webError, string responseBody, int timeoutLimit = 30)
        {
            if (webError != null && webError.IndexOf("timeout", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return $"Request timed out: Scientific backend took longer than {timeoutLimit}s to respond. Check server status or narrow the search window.";
            }

            if (statusCode == 0)
            {
                return $"Backend unreachable: Cannot connect to scientific backend at {baseUrl}. Ensure FastAPI is running.";
            }

            string detailMsg = ExtractDetailMessage(responseBody);

            switch (statusCode)
            {
                case 422:
                    return $"Input Validation Error (HTTP 422): {detailMsg}";
                case 429:
                    return "Scientific API Busy (HTTP 429): Two analyses already in progress. Please retry in a moment.";
                case 502:
                    return $"NASA Horizons Parsing Error (HTTP 502): {detailMsg}";
                case 503:
                    return $"NASA Horizons Unavailable (HTTP 503): {detailMsg}";
                case 500:
                    return $"Scientific Server Error (HTTP 500): {detailMsg}";
                default:
                    return $"HTTP {statusCode} Error: {(!string.IsNullOrEmpty(detailMsg) ? detailMsg : webError)}";
            }
        }

        private string ExtractDetailMessage(string json)
        {
            if (string.IsNullOrEmpty(json)) return "No details provided by backend.";

            try
            {
                // FastAPI standard error format: {"detail": ...}
                int detailIdx = json.IndexOf("\"detail\"", StringComparison.OrdinalIgnoreCase);
                if (detailIdx >= 0)
                {
                    int msgIdx = json.IndexOf("\"message\"", detailIdx, StringComparison.OrdinalIgnoreCase);
                    if (msgIdx >= 0)
                    {
                        int colon = json.IndexOf(':', msgIdx);
                        int startQuote = json.IndexOf('"', colon + 1);
                        int endQuote = json.IndexOf('"', startQuote + 1);
                        if (startQuote >= 0 && endQuote > startQuote)
                        {
                            return json.Substring(startQuote + 1, endQuote - startQuote - 1);
                        }
                    }

                    // Fallback to direct string detail
                    int colonDetail = json.IndexOf(':', detailIdx);
                    int strStart = json.IndexOf('"', colonDetail + 1);
                    if (strStart >= 0)
                    {
                        int strEnd = json.IndexOf('"', strStart + 1);
                        if (strEnd > strStart)
                        {
                            return json.Substring(strStart + 1, strEnd - strStart - 1);
                        }
                    }
                }
            }
            catch
            {
                // Ignore extraction failures and return truncated raw response
            }

            return json.Length > 200 ? json.Substring(0, 200) + "..." : json;
        }
    }
}
