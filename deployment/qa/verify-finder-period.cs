using System;
using System.Reflection;
using LunarNavigator.UI;
using LunarNavigator.Data;
public static class FinderPeriodCheck {
 public static string Main() {
  int passed=0;
  foreach (double stay in new double[]{72,96,168,672,673}) {
   var view=new OpportunityFinderView();
   view.SetEarliestUtc(new DateTime(2026,11,18,19,17,57,DateTimeKind.Utc));
   view.SetSearchSpan(672); view.SetMissionDuration(stay);
   LiveOpportunityRequest captured=null;
   view.OnSearchRequested += request => captured=request;
   typeof(OpportunityFinderView).GetMethod("TriggerSearch",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(view,null);
   if(stay>672) { if(captured!=null)throw new Exception("Invalid stay accepted"); }
   else if(captured==null || captured.search_window_mode!="CONTAINED_MISSION" || captured.search_window_h!=672 || captured.mission_duration_h!=stay)throw new Exception("Wrong request");
   passed++;
  }
  return passed+" Finder UI request checks passed";
 }
}
