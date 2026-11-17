using System;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;
using LunarNavigator;
using LunarNavigator.UI;
using LunarNavigator.Data;

System.IO.Directory.CreateDirectory("Builds/ClockValidation");
Application.runInBackground = true;
var app = UnityEngine.Object.FindAnyObjectByType<LunarApplication>();
var root = app.UI.GetComponent<UIDocument>().rootVisualElement;
var field = typeof(NavigatorUI).GetField("missionConfigView", BindingFlags.Instance | BindingFlags.NonPublic);
var mission = (MissionConfigView)field.GetValue(app.UI);
var server = Newtonsoft.Json.Linq.JObject.Parse(System.IO.File.ReadAllText("Builds/ClockValidation/backend-startup.json"));
LunarApiClient.Instance.baseUrl = "http://127.0.0.1:" + server["port"];
mission.TxtApiUrl.SetValueWithoutNotify(LunarApiClient.Instance.baseUrl);
void Click(Button button) {
    typeof(Clickable).GetMethod("SimulateSingleClick", BindingFlags.Instance | BindingFlags.NonPublic)
        .Invoke(button.clickable, new object[] { null, 0 });
}
Click(root.Q<Button>("mission-use-current-utc"));
await Task.Delay(1200);
var requested = mission.SelectedLandingUtc;
var input = mission.TxtLandingUtc.value;
var before = app.Clock.seconds;
var watch = System.Diagnostics.Stopwatch.StartNew();
Click(mission.BtnAnalyze);
await Task.Delay(300);
var started = mission.IsLoading;
while (mission.IsLoading && watch.Elapsed.TotalSeconds < 150) await Task.Delay(500);
await Task.Delay(1200);
var elapsed = watch.Elapsed.TotalSeconds;
var advanced = app.Clock.seconds - before;
var assessed = typeof(NavigatorUI).GetField("assessedData", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(app.UI);
var state = typeof(NavigatorUI).GetField("assessmentState", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(app.UI)?.ToString();
var checks = new {
    actualAnalysisStarted = started,
    completedSuccessfully = !mission.IsLoading && assessed != null && state == "Analysis ready",
    selectedDatePreserved = mission.SelectedLandingUtc == requested && mission.TxtLandingUtc.value == input,
    responseOriginMatchesRequest = assessed != null && AnalysisRepository.Instance.T0 == requested,
    playbackStillRunningAt1x = !app.Clock.paused && app.Clock.speed == 1,
    playheadAdvancedDuringAnalysis = advanced > 1 && Math.Abs(advanced - elapsed) < 1.5
};
var result = new {checks, requested, input, elapsed, advanced, clockUtc=app.Clock.Utc, deviceUtc=DateTime.UtcNow, state, error=mission.LblErrorMessage.text, api=LunarApiClient.Instance.baseUrl};
System.IO.File.WriteAllText("Builds/ClockValidation/live-analysis.json", Newtonsoft.Json.JsonConvert.SerializeObject(result, Newtonsoft.Json.Formatting.Indented));
return result;
