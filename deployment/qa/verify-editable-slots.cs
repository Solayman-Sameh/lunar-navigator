using System;
using UnityEngine.UIElements;
using LunarNavigator.UI;
public static class EditableSlotChecks {
 static int count;
 static void Check(bool ok,string label) {if(!ok)throw new Exception(label);count++;}
 public static string Main() {
  var host=UnityEngine.ScriptableObject.CreateInstance<UnityEditor.EditorWindow>();
  host.Show();
  try {
  var finder=new OpportunityFinderView();host.rootVisualElement.Add(finder);
  finder.SetEarliestUtc(new DateTime(2028,1,15,12,10,9,DateTimeKind.Utc));
  finder.SetSearchSpan(48);finder.SetMissionDuration(24);
  finder.Q<EditableNumberSlot>("val-search-days").value="30";
  Check(finder.SelectedSearchWindowH==720,"30 days typing");
  finder.Q<EditableNumberSlot>("val-search-hours").value="5";
  Check(finder.SelectedSearchWindowH==725,"search hours");
  finder.Q<EditableNumberSlot>("val-stay-days").value="7";
  finder.Q<EditableNumberSlot>("val-stay-hours").value="3";
  Check(finder.SelectedMissionDurationH==171,"stay days hours");
  finder.Q<EditableNumberSlot>("val-search-hours").value="24";
  Check(finder.SelectedSearchWindowH==725,"reject hours 24");
  finder.Q<EditableNumberSlot>("val-search-days").value="abc";
  Check(finder.SelectedSearchWindowH==725,"reject letters");
  finder.Q<EditableNumberSlot>("val-search-days").value="";
  Check(finder.Q<EditableNumberSlot>("val-search-days").value=="30","restore empty");
  finder.SetSearchSpan(168);
  Check(finder.Q<EditableNumberSlot>("val-search-days").value=="07","preset sync");
  finder.StepSearchDays(1);
  Check(finder.Q<EditableNumberSlot>("val-search-days").value=="08","arrow sync");
  var mission=new MissionConfigView();host.rootVisualElement.Add(mission);mission.SetLandingUtc(new DateTime(2028,1,15,12,10,9,DateTimeKind.Utc));
  foreach(var view in new VisualElement[]{finder,mission}) {
   string[] parts={"year","month","day","hour","minute","second"};
   string[] values={"2028","02","29","23","59","58"};
   for(int i=0;i<parts.Length;i++) {view.Q<EditableNumberSlot>("val-"+parts[i]).value=values[i];}
   DateTime date=view==finder?finder.SelectedEarliestUtc:mission.SelectedLandingUtc;
   Check(date==new DateTime(2028,2,29,23,59,58,DateTimeKind.Utc),"all date parts");
   view.Q<EditableNumberSlot>("val-year").value="2027";
   Check((view==finder?finder.SelectedEarliestUtc:mission.SelectedLandingUtc)==date,"reject invalid leap day");
   view.Q<EditableNumberSlot>("val-second").value="60";
   Check((view==finder?finder.SelectedEarliestUtc:mission.SelectedLandingUtc)==date,"reject second60");
   Check(view.Query<EditableNumberSlot>().ToList().TrueForAll(f=>f.isDelayed),"delayed input");
  }
  finder.LblMinSunVal.value="42.5";Check(finder.SliderMinSun.value==42.5f,"percent edit");
  finder.LblMinEarthVal.value="55";Check(finder.SliderMinEarth.value==55,"earth edit");
  finder.LblMaxDarkVal.value="12.5";Check(finder.SliderMaxDark.value==12.5f,"dark edit");
  finder.LblMaxCommVal.value="8";Check(finder.SliderMaxComm.value==8,"comms edit");
  finder.LblMinSunVal.value="101";Check(finder.SliderMinSun.value==42.5f,"reject percent101");
  finder.SliderMaxDark.value=20;Check(finder.LblMaxDarkVal.value=="20","slider sync");
  Check(finder.Query<EditableNumberSlot>().ToList().Count==14,"14 finder fields");
  Check(mission.Query<EditableNumberSlot>().ToList().Count==6,"6 mission date fields");
  return count+" editable number checks passed";
  } finally {host.Close();}
 }
}
