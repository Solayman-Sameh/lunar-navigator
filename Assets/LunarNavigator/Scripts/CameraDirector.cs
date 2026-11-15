using UnityEngine;
namespace LunarNavigator
{
    public enum CameraMode { Global, Approach, LocalOrbit, Surface }
    public class CameraDirector : MonoBehaviour
    {
        public Camera view;
        public NavigatorConfig config;
        public LunarTerrain terrain;
        public LunarSite selected;
        public CameraMode Mode { get; private set; }
        public bool IsTransitioning { get; private set; }
        public float Progress { get; private set; }
        public float ObserverHeight { get; private set; }=1.8f;
        public float yaw=-48, pitch=-25, distance=3600;
        public float localYaw=20, localPitch=28, localDistance=150;
        public float surfaceYaw=45, surfacePitch=3;
        Vector3 startPosition;
        Quaternion startRotation;
        float elapsed, duration;
        Vector2 inertia;
        float targetDistance;
        public Quaternion Frame => LunarCoordinates.Frame(selected.Direction);
        void Start()
        {
            distance=config.globalStart;targetDistance=distance;ObserverHeight=config.observerHeight;
            Mode=CameraMode.Global; ApplyPose(GlobalPosition(),Look(GlobalPosition(),Vector3.down*90,Vector3.up));
        }
        public void Select(LunarSite site)
        {
            selected=site;
            terrain.transform.SetPositionAndRotation(site.Direction*config.radius,Frame);
            if(Mode!=CameraMode.Global) ChangeMode(Mode);
        }
        public void ChangeMode(CameraMode mode)
        {
            if(selected==null)return;
            startPosition=view.transform.position;startRotation=view.transform.rotation;
            Mode=mode;elapsed=0;Progress=0;inertia=Vector2.zero;
            duration=mode==CameraMode.Approach?config.approachDuration:config.transitionDuration;
            IsTransitioning=true;
            if(mode==CameraMode.LocalOrbit||mode==CameraMode.Approach) { localDistance=config.localStart;localPitch=28; }
            if(mode==CameraMode.Global) { var geo=LunarCoordinates.Geographic(startPosition);yaw=geo.y;pitch=Mathf.Clamp(geo.x,-88,88);distance=targetDistance=config.globalStart; }
        }
        public void Focus()
        {
            if(Mode==CameraMode.Global)
            {
                startPosition=view.transform.position;startRotation=view.transform.rotation;
                yaw=selected.longitude;pitch=selected.latitude;distance=targetDistance=config.radius*2.3f;
                IsTransitioning=true;elapsed=0;duration=config.transitionDuration;
            }
            else ChangeMode(CameraMode.LocalOrbit);
        }
        public void ResetView()
        {
            localYaw=20;localPitch=28;localDistance=config.localStart;surfaceYaw=45;surfacePitch=3;
            if(Mode==CameraMode.Global){ChangeMode(CameraMode.Global);yaw=-48;pitch=-25;}
            else ChangeMode(Mode==CameraMode.Approach?CameraMode.LocalOrbit:Mode);
        }
        public void Skip() { if(Mode==CameraMode.Approach){elapsed=duration; Tick(0);} }
        public void SetHeight(float height) { ObserverHeight=Mathf.Clamp(height,.5f,12); }
        public void Drag(Vector2 delta)
        {
            if(Mode==CameraMode.Approach)return;
            IsTransitioning=false;
            if(Mode==CameraMode.Global){yaw-=delta.x*.17f;pitch=Mathf.Clamp(pitch-delta.y*.17f,-89,89);inertia=-delta*.17f;}
            else if(Mode==CameraMode.LocalOrbit){localYaw-=delta.x*.2f;localPitch=Mathf.Clamp(localPitch-delta.y*.18f,7,88);}
            else {surfaceYaw+=delta.x*.12f;surfacePitch=Mathf.Clamp(surfacePitch-delta.y*.12f,-85,85);}
        }
        public void Zoom(float value)
        {
            if(Mode==CameraMode.Approach)return;
            if(Mode==CameraMode.Global)targetDistance=Mathf.Clamp(targetDistance*Mathf.Exp(-value*.12f),config.globalMin,config.globalMax);
            else if(Mode==CameraMode.LocalOrbit)localDistance=Mathf.Clamp(localDistance*Mathf.Exp(-value*.12f),config.localMin,config.localMax);
            else SetHeight(ObserverHeight+value*.25f);
        }
        void LateUpdate(){Tick(Time.unscaledDeltaTime);}
        public void Tick(float delta)
        {
            if(!view||!selected)return;
            if(Mode==CameraMode.Global&&!IsTransitioning)
            {
                if(MouseDragging==false){yaw+=inertia.x*delta*35;pitch=Mathf.Clamp(pitch+inertia.y*delta*35,-89,89);inertia=Vector2.Lerp(inertia,Vector2.zero,1-Mathf.Exp(-delta*7));}
                distance=Mathf.Lerp(distance,targetDistance,1-Mathf.Exp(-delta*9));
            }
            GetDesired(out var pos,out var rot);
            if(IsTransitioning)
            {
                elapsed+=delta; Progress=Mathf.Clamp01(elapsed/Mathf.Max(.05f,duration));
                float s=config.easing.Evaluate(Progress);
                if(Mode==CameraMode.Approach)
                {
                    var finish=LocalPosition();
                    if(Progress<.32f)
                    {
                        float t=Smooth(Progress/.32f);
                        var dir=Vector3.Slerp(startPosition.normalized,finish.normalized,t);
                        pos=dir*Mathf.Lerp(startPosition.magnitude,Mathf.Max(startPosition.magnitude,config.radius+400),t);
                    }
                    else
                    {
                        float t=Smooth((Progress-.32f)/.68f);
                        float far=Mathf.Max(startPosition.magnitude,config.radius+400)-config.radius;
                        float close=finish.magnitude-config.radius;
                        float alt=Mathf.Exp(Mathf.Lerp(Mathf.Log(Mathf.Max(1,far)),Mathf.Log(Mathf.Max(1,close)),t));
                        pos=finish.normalized*(config.radius+alt);
                    }
                    var aim=Vector3.Lerp(Vector3.zero,terrain.Anchor,Smooth(Progress));
                    rot=Look(pos,aim,Vector3.Slerp(Vector3.up,selected.Direction,Smooth(Progress)));
                    rot=Quaternion.Slerp(startRotation,rot,Mathf.Clamp01(Progress*5));
                }
                else if(Mode==CameraMode.Global || startPosition.magnitude>config.radius+300)
                {
                    var dir=Vector3.Slerp(startPosition.normalized,pos.normalized,s);
                    pos=dir*Mathf.Lerp(startPosition.magnitude,pos.magnitude,s);
                    rot=Quaternion.Slerp(startRotation,rot,s);
                }
                else {pos=Vector3.Lerp(startPosition,pos,s);rot=Quaternion.Slerp(startRotation,rot,s);}
                if(Progress>=1){IsTransitioning=false;if(Mode==CameraMode.Approach){Mode=CameraMode.LocalOrbit;GetDesired(out pos,out rot);}}
            }
            // Keep every camera pose outside the globe, then above the local terrain where present.
            float minimum=config.radius+.6f;
            if(pos.magnitude<minimum)pos=pos.normalized*minimum;
            var p=terrain.transform.InverseTransformPoint(pos);
            if(Mathf.Abs(p.x)<LunarTerrain.HalfSize&&Mathf.Abs(p.z)<LunarTerrain.HalfSize)
            {
                float ground=terrain.SampleHeight(p.x,p.z)+.4f;
                if(p.y<ground){p.y=ground;pos=terrain.transform.TransformPoint(p);}
            }
            ApplyPose(pos,rot);
            view.nearClipPlane=Mode==CameraMode.Global?1:.08f;
        }
        public bool MouseDragging {get;set;}
        static float Smooth(float t){t=Mathf.Clamp01(t);return t*t*t*(t*(t*6-15)+10);}
        Vector3 GlobalPosition()=>LunarCoordinates.Direction(pitch,yaw)*distance;
        Vector3 LocalPosition()
        {
            float a=localYaw*Mathf.Deg2Rad,b=localPitch*Mathf.Deg2Rad;
            return terrain.Anchor+Frame*(new Vector3(Mathf.Sin(a)*Mathf.Cos(b),Mathf.Sin(b),-Mathf.Cos(a)*Mathf.Cos(b))*localDistance);
        }
        void GetDesired(out Vector3 pos,out Quaternion rot)
        {
            if(Mode==CameraMode.Global){pos=GlobalPosition();rot=Look(pos,Vector3.down*90,Vector3.up);}
            else if(Mode==CameraMode.Surface){pos=terrain.Anchor+selected.Direction*ObserverHeight;rot=Frame*Quaternion.Euler(surfacePitch,surfaceYaw,0);}
            else {pos=LocalPosition();rot=Look(pos,terrain.Anchor,selected.Direction);}
        }
        static Quaternion Look(Vector3 from,Vector3 to,Vector3 up)
        { var f=(to-from).normalized; if(Vector3.Cross(f,up).sqrMagnitude<.0001f)up=Vector3.forward;return Quaternion.LookRotation(f,up); }
        void ApplyPose(Vector3 pos,Quaternion rot){view.transform.SetPositionAndRotation(pos,rot);}
    }
}
