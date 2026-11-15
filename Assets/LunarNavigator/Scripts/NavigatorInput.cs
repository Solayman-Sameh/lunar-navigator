using UnityEngine;
using UnityEngine.InputSystem;
namespace LunarNavigator
{
    public class NavigatorInput : MonoBehaviour
    {
        public LunarApplication app;
        Vector2 pressPosition;
        Vector2 previousPointer;
        bool hasPointer;
        bool dragging, blocked;
        float lastClick=-10;
        void Update()
        {
            var k=Keyboard.current;var m=Mouse.current;
            if(k!=null)
            {
                if(k.digit1Key.wasPressedThisFrame)app.Director.ChangeMode(CameraMode.Global);
                if(k.digit2Key.wasPressedThisFrame)app.Director.ChangeMode(CameraMode.Approach);
                if(k.digit3Key.wasPressedThisFrame)app.Director.ChangeMode(CameraMode.LocalOrbit);
                if(k.digit4Key.wasPressedThisFrame)app.Director.ChangeMode(CameraMode.Surface);
                if(k.fKey.wasPressedThisFrame)app.Director.Focus();
                if(k.rKey.wasPressedThisFrame)app.Director.ResetView();
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
            if(m.leftButton.wasPressedThisFrame){pressPosition=p;blocked=app.UI.IsPointerOverUI(p);dragging=false;}
            if(m.leftButton.isPressed&&!blocked)
            {
                if(Vector2.Distance(p,pressPosition)>4)dragging=true;
                if(dragging)app.Director.Drag(pointerDelta);
            }
            app.Director.MouseDragging=dragging&&m.leftButton.isPressed&&!blocked;
            if(m.leftButton.wasReleasedThisFrame&&!blocked&&!dragging)
            {
                bool doubleClick=Time.unscaledTime-lastClick<.35f;lastClick=Time.unscaledTime;
                app.UI.PickMarker(p,doubleClick);
            }
            float scroll=m.scroll.ReadValue().y/120;
            if(Mathf.Abs(scroll)>.001f&&!app.UI.IsPointerOverUI(p))app.Director.Zoom(scroll);
        }
    }
}
