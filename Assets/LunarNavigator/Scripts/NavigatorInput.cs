using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
namespace LunarNavigator
{
    public class NavigatorInput : MonoBehaviour
    {
        public LunarApplication app;
        Vector2 pressPosition;
        Vector2 previousPointer;
        bool hasPointer;
        bool dragging, blocked;
        Vector2 eventPressPosition;
        bool eventHeld, eventDragged, eventGesture;
        float lastClick=-10;
        void OnEnable() { InputSystem.onEvent += ObservePointer; }
        void OnDisable() { InputSystem.onEvent -= ObservePointer; ResetPointer(); }
        void OnApplicationFocus(bool focused) { if (!focused) ResetPointer(); }
        void ResetPointer() { hasPointer=false; dragging=false; blocked=false; eventHeld=false; eventGesture=false; eventDragged=false; if(app && app.Director)app.Director.MouseDragging=false; }
        void ObservePointer(InputEventPtr evt, InputDevice device)
        {
            if (!(device is Mouse mouse) || (!evt.IsA<StateEvent>() && !evt.IsA<DeltaStateEvent>())) return;
            var p = mouse.position.ReadValueFromEvent(evt, out Vector2 value) ? value : mouse.position.ReadValue();
            bool held = mouse.leftButton.ReadValueFromEvent(evt, out float button) ? button > .5f : eventHeld;
            // Redundant mouse events can be merged before onEvent: the press
            // event may already contain the final dragged position. The device
            // state still holds the position immediately before that event.
            if (held && !eventHeld) { eventPressPosition=mouse.position.ReadValue(); eventGesture=true; eventDragged=false; }
            if (eventGesture && (held || eventHeld) && Vector2.Distance(p,eventPressPosition)>4) eventDragged=true;
            eventHeld=held;
        }
        void Update()
        {
            var k=Keyboard.current;var m=Mouse.current;
            if(k!=null && !app.UI.IsEditingText)
            {
                var movement = new Vector2((k.dKey.isPressed?1:0)-(k.aKey.isPressed?1:0),(k.wKey.isPressed?1:0)-(k.sKey.isPressed?1:0));
                app.Director.Move(movement,Time.unscaledDeltaTime,k.leftShiftKey.isPressed||k.rightShiftKey.isPressed);
                if(k.digit1Key.wasPressedThisFrame)app.Director.ChangeMode(CameraMode.Global);
                if(k.digit2Key.wasPressedThisFrame)app.Director.ChangeMode(CameraMode.Approach);
                if(k.digit3Key.wasPressedThisFrame)app.Director.ChangeMode(CameraMode.LocalOrbit);
                if(k.digit4Key.wasPressedThisFrame)app.Director.ChangeMode(CameraMode.Surface);
                if(k.fKey.wasPressedThisFrame)app.Director.Focus();
                if(k.rKey.wasPressedThisFrame)app.Director.ResetView();
                if(k.eKey.wasPressedThisFrame)app.Director.LookAtEarth();
                if((k.leftShiftKey.isPressed||k.rightShiftKey.isPressed)&&k.sKey.wasPressedThisFrame)app.Director.LookAtSun();
                if(k.spaceKey.wasPressedThisFrame)app.Clock.Toggle();
                if(k.tabKey.wasPressedThisFrame)app.UI.ToggleInterface();
                if(k.hKey.wasPressedThisFrame||k.slashKey.wasPressedThisFrame)app.UI.Open("Help");
                if(k.escapeKey.wasPressedThisFrame)app.Back();
                if(k.leftArrowKey.wasPressedThisFrame)
                {
                    double dt = k.shiftKey.isPressed ? 6 * 3600 : 3600;
                    app.Clock.SetTime(app.Clock.seconds - dt);
                }
                if(k.rightArrowKey.wasPressedThisFrame)
                {
                    double dt = k.shiftKey.isPressed ? 6 * 3600 : 3600;
                    app.Clock.SetTime(app.Clock.seconds + dt);
                }
            }
            if(m==null)return;
            Vector2 p=m.position.ReadValue();
            Vector2 pointerDelta=hasPointer?p-previousPointer:Vector2.zero;previousPointer=p;hasPointer=true;
            bool began = m.leftButton.wasPressedThisFrame;
            if(began)
            {
                pressPosition = p;
                blocked = app.UI != null && (app.UI.IsPointerOverUI(p) || (eventGesture && app.UI.IsPointerOverUI(eventPressPosition)));
                dragging = !blocked && eventGesture && eventDragged;
            }
            if(!blocked && eventGesture && eventDragged) dragging = true;
            if(m.leftButton.isPressed && !blocked)
            {
                if(Vector2.Distance(p,pressPosition)>4) dragging = true;
                if(dragging) app.Director.Drag(began ? p-pressPosition : pointerDelta);
            }
            app.Director.MouseDragging = dragging && m.leftButton.isPressed && !blocked;
            if(m.leftButton.wasReleasedThisFrame)
            {
                if (!blocked && dragging && began) app.Director.Drag(p-pressPosition);
                if (!blocked && !dragging && (app.UI == null || !app.UI.IsPointerOverUI(p)))
                {
                    bool doubleClick=Time.unscaledTime-lastClick<.35f;lastClick=Time.unscaledTime;
                    if (!app.UI.PickMarker(p,doubleClick)) app.PickLocation(app.Director.view.ScreenPointToRay(p));
                }
                eventGesture=false;eventDragged=false;dragging=false;blocked=false;
            }
            float rawScroll = m.scroll.ReadValue().y;
            float scroll = 0f;
            if (Mathf.Abs(rawScroll) > 0.0001f)
            {
                if (Mathf.Abs(rawScroll) >= 15f)
                    scroll = rawScroll / 120f;
                else
                    scroll = rawScroll;
            }
            if(Mathf.Abs(scroll)>.001f && (app.UI == null || !app.UI.IsPointerOverUI(p)))
            {
                bool ctrl = k != null && (k.leftCtrlKey.isPressed || k.rightCtrlKey.isPressed);
                if (ctrl) app.Director.OpticalZoom(scroll);
                else app.Director.Zoom(scroll);
            }
        }
    }
}
