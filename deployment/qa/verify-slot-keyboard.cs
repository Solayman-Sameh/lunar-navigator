using System;
using UnityEngine;
using UnityEngine.UIElements;
using LunarNavigator.UI;
public static class SlotKeyboardChecks {
 static void Key(VisualElement target, string key) {using(var e=KeyDownEvent.GetPooled(Event.KeyboardEvent(key)))target.SendEvent(e);}
 static void Character(VisualElement target,char c) {using(var e=KeyDownEvent.GetPooled(new Event{type=EventType.KeyDown,character=c}))target.SendEvent(e);}
 public static string Main() {
  var host=ScriptableObject.CreateInstance<UnityEditor.EditorWindow>();host.Show();
  try {
   var view=new OpportunityFinderView();host.rootVisualElement.Add(view);view.SetSearchSpan(48);
   var field=view.Q<EditableNumberSlot>("val-search-days");field.Focus();
   var input=field.Q<TextElement>();Key(input,"^a");Character(input,'3');Character(input,'0');
   if(view.SelectedSearchWindowH!=48)throw new Exception("Partial input committed");
   Key(input,"return");
   if(view.SelectedSearchWindowH!=720)throw new Exception("Enter did not commit 30 days: "+field.value);
   Key(input,"^a");Character(input,'7');Key(input,"escape");
   if(view.SelectedSearchWindowH!=720 || field.value!="30")throw new Exception("Escape did not restore");
   field.Focus();Key(input,"^a");Character(input,'2');Character(input,'0');field.Blur();
   if(view.SelectedSearchWindowH!=480)throw new Exception("Blur did not commit");
   return "Keyboard typing, delayed commit, Enter, Escape and blur passed";
  }finally{host.Close();}
 }
}
