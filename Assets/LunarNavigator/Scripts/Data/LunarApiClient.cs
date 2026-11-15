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
        public double? latitude_deg;
        public double? longitude_deg;
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
                sb.AppendFormat(CultureInfo.InvariantCulture, "\"latitude_deg\":{0:F6},\"longitude_deg\":{1:F6},",
                    latitude_deg.Value, longitude_deg.Value);
            }

            sb.AppendFormat(CultureInfo.InvariantCulture, "\"landing_utc\":\"{0}\",", landing_utc);
            sb.AppendFormat(CultureInfo.InvariantCulture, "\"mission_duration_h\":{0:F3},", mission_duration_h);
            sb.AppendFormat(CultureInfo.InvariantCulture, "\"observer_height_m\":{0:F1},", observer_height_m);
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

        private void Awake()
        {
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
