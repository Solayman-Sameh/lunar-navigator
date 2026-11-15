// Explicitly launched verification harness. Never included in the scene or started by the app.
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UIElements;
namespace LunarNavigator
{
    public class InteractionProbe : MonoBehaviour
    {
        readonly List<string> results=new List<string>();
        Keyboard keyboard;Mouse mouse;LunarApplication app;
        void Check(bool value,string name){results.Add((value?"PASS ":"FAIL ")+name);}
        IEnumerator KeyPress(Key key)
        {InputSystem.QueueStateEvent(keyboard,new KeyboardState(key));yield return null;yield return null;InputSystem.QueueStateEvent(keyboard,new KeyboardState());yield return null;yield return null;}
        IEnumerator Pointer(Vector2 pos,bool down=false,float scroll=0)
        {var state=new MouseState{position=pos,scroll=new Vector2(0,scroll)};state=state.WithButton(UnityEngine.InputSystem.LowLevel.MouseButton.Left,down);InputSystem.QueueStateEvent(mouse,state);yield return null;yield return null;}
        Vector2 ScreenPoint(VisualElement e)
        {var root=app.UI.GetComponent<UIDocument>().rootVisualElement;var p=e.worldBound.center;return new Vector2(p.x/root.worldBound.width*Screen.width,Screen.height-p.y/root.worldBound.height*Screen.height);}
        Button FindButton(string text)
        {return app.UI.GetComponent<UIDocument>().rootVisualElement.Query<Button>().ToList().Find(b=>b.text==text);}
        IEnumerator Click(Button button)
        {var p=ScreenPoint(button);yield return Pointer(p);yield return Pointer(p,true);yield return Pointer(p);yield return null;yield return null;}
        IEnumerator Start()
        {
            app=FindFirstObjectByType<LunarApplication>();
            keyboard=InputSystem.AddDevice<Keyboard>("LunarTestKeyboard");mouse=InputSystem.AddDevice<Mouse>("LunarTestMouse");keyboard.MakeCurrent();mouse.MakeCurrent();
            app.UI.Close();if(!app.UI.InterfaceVisible)app.UI.ToggleInterface();
            yield return KeyPress(Key.H);Check(app.UI.CurrentPopup=="Help","H opens help through Input System");
            yield return KeyPress(Key.Escape);Check(app.UI.CurrentPopup=="","Escape dismisses help");
            yield return KeyPress(Key.Tab);Check(!app.UI.InterfaceVisible,"Tab hides interface");yield return KeyPress(Key.Tab);Check(app.UI.InterfaceVisible,"Tab restores interface");
            app.Clock.paused=true;yield return KeyPress(Key.Space);Check(!app.Clock.paused,"Space resumes clock");yield return KeyPress(Key.Space);Check(app.Clock.paused,"Space pauses clock");
            foreach(var pair in new[]{(Key.Digit1,CameraMode.Global),(Key.Digit3,CameraMode.LocalOrbit),(Key.Digit4,CameraMode.Surface),(Key.Digit2,CameraMode.Approach)})
            {yield return KeyPress(pair.Item1);Check(app.Director.Mode==pair.Item2,"Keyboard mode "+pair.Item2);yield return new WaitForSecondsRealtime(.15f);}
            yield return KeyPress(Key.Escape);Check(app.Director.Mode==CameraMode.LocalOrbit,"Escape skips approach");
            yield return KeyPress(Key.Digit1);yield return new WaitForSecondsRealtime(2);
            float yaw=app.Director.yaw;
            var p0=new Vector2(Screen.width*.5f,Screen.height*.6f);yield return Pointer(p0);yield return Pointer(p0,true);yield return Pointer(p0+new Vector2(100,35),true);yield return Pointer(p0+new Vector2(100,35));
            Check(Mathf.Abs(app.Director.yaw-yaw)>1,"Mouse drag changes global orbit");
            float distance=app.Director.distance;yield return Pointer(p0,false,120);yield return Pointer(p0);yield return new WaitForSecondsRealtime(.4f);Check(app.Director.distance<distance,"Scroll zoom changes distance");
            yield return KeyPress(Key.F);Check(app.Director.IsTransitioning,"F begins focus transition");yield return KeyPress(Key.R);Check(app.Director.IsTransitioning,"R resets through transition");yield return new WaitForSecondsRealtime(2);
            yield return Click(FindButton("Sites"));Check(app.UI.CurrentPopup=="Sites","Sites button opens popover through pointer");
            var option=app.UI.GetComponent<UIDocument>().rootVisualElement.Query<Button>().ToList().Find(b=>b.text.StartsWith(app.sites[1].displayName));
            if(option!=null){yield return Click(option);Check(app.SelectedSite==app.sites[1],"Site option changes shared selection");}
            else Check(false,"Site option exists");
            yield return KeyPress(Key.Escape);
            yield return Click(FindButton("Settings"));Check(app.UI.CurrentPopup=="Settings","Settings button opens popover");
            yield return Click(FindButton("Performance"));Check(app.World.Preset=="Performance","Performance button changes actual preset");
            yield return KeyPress(Key.Escape);
            yaw=app.Director.yaw;var button=FindButton("Settings");var bp=ScreenPoint(button);
            yield return Pointer(bp);yield return Pointer(bp,true);yield return Pointer(bp+new Vector2(25,-10),true);yield return Pointer(bp+new Vector2(25,-10));Check(Mathf.Abs(app.Director.yaw-yaw)<.1f,"Dragging UI suppresses world orbit");
            app.UI.Close();yield return Click(FindButton("Layers"));yield return Pointer(p0);yield return Pointer(p0,true);yield return Pointer(p0);Check(app.UI.CurrentPopup=="","Outside click dismisses popover");
            var slider=app.UI.GetComponent<UIDocument>().rootVisualElement.Q<Slider>("time-slider");var sp=ScreenPoint(slider);sp.x+=slider.worldBound.width/4*(Screen.width/app.UI.GetComponent<UIDocument>().rootVisualElement.worldBound.width);
            yield return Pointer(sp);yield return Pointer(sp,true);yield return Pointer(sp);Check(app.Clock.seconds>86400,"Timeline pointer scrubs UTC");
            app.Clock.SetTime(0);app.Clock.paused=true;app.World.ApplyQuality("Balanced");app.SelectSite(app.sites[0]);app.Director.ChangeMode(CameraMode.Global);app.Director.ResetView();
            yield return new WaitForSecondsRealtime(2);
            var markerButtons=app.UI.GetComponent<UIDocument>().rootVisualElement.Query<Button>(className:"marker").ToList();
            for(int i=0;i<markerButtons.Count;i++){yield return Click(markerButtons[i]);Check(app.SelectedSite==app.sites[i],"Projected marker selects "+app.sites[i].displayName);}
            yield return Click(markerButtons[0]);yield return Click(markerButtons[0]);Check(app.Director.IsTransitioning,"Double-click marker focuses destination");
            yield return KeyPress(Key.Digit3);yield return new WaitForSecondsRealtime(2);float localYaw=app.Director.localYaw;
            yield return Pointer(p0);yield return Pointer(p0,true);yield return Pointer(p0+new Vector2(70,20),true);yield return Pointer(p0+new Vector2(70,20));Check(Mathf.Abs(app.Director.localYaw-localYaw)>1,"Mouse drag changes local orbit");
            yield return KeyPress(Key.Digit4);yield return new WaitForSecondsRealtime(2);float look=app.Director.surfaceYaw;
            yield return Pointer(p0);yield return Pointer(p0,true);yield return Pointer(p0+new Vector2(70,20),true);yield return Pointer(p0+new Vector2(70,20));Check(Mathf.Abs(app.Director.surfaceYaw-look)>1,"Mouse drag changes surface look");
            float height=app.Director.ObserverHeight;yield return Pointer(p0,false,120);yield return Pointer(p0);Check(app.Director.ObserverHeight>height,"Surface scroll changes eye height");
            app.Director.ChangeMode(CameraMode.Global);app.Director.ResetView();
            InputSystem.RemoveDevice(keyboard);InputSystem.RemoveDevice(mouse);
            Directory.CreateDirectory("Documentation/Verification");File.WriteAllText("Documentation/Verification/interaction.txt",string.Join("\n",results));
            Debug.Log("LUNAR_INTERACTION_DONE "+results.FindAll(x=>x.StartsWith("PASS")).Count+" pass / "+results.FindAll(x=>x.StartsWith("FAIL")).Count+" fail");Destroy(this);
        }
        void OnDestroy(){if(keyboard!=null&&keyboard.added)InputSystem.RemoveDevice(keyboard);if(mouse!=null&&mouse.added)InputSystem.RemoveDevice(mouse);}
    }
}
