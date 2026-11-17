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
        public float yaw=-45, pitch=-60, distance=3600;
        public float localYaw=20, localPitch=28, localDistance=150;
        public float surfaceYaw=45, surfacePitch=3;
        Vector3 startPosition;
        Quaternion startRotation;
        float elapsed, duration;
        MeshCollider surfaceCollider;
        Vector2 inertia;
        public Vector2 NavigationOffset { get; private set; }
        Vector3 NavigationAnchor
        {
            get
            {
                float travel = NavigationOffset.magnitude;
                if (travel <= LunarTerrain.HalfSize)
                    return terrain.transform.TransformPoint(NavigationOffset.x, terrain.SampleHeight(NavigationOffset.x, NavigationOffset.y), NavigationOffset.y);
                // Continue over the nominal sphere beyond the illustrative local
                // patch, without an invisible movement wall. Match its edge direction.
                float edgeAngle = Mathf.Asin(Mathf.Clamp01(LunarTerrain.HalfSize / config.radius));
                float angle = edgeAngle + (travel - LunarTerrain.HalfSize) / config.radius;
                var direction = NavigationOffset / travel;
                return Frame * new Vector3(direction.x * Mathf.Sin(angle), Mathf.Cos(angle), direction.y * Mathf.Sin(angle)) * (config.radius + .15f);
            }
        }
        public void Move(Vector2 input, float delta, bool fast=false)
        {
            if (input.sqrMagnitude < .000001f || (Mode != CameraMode.LocalOrbit && Mode != CameraMode.Surface)) return;
            // Deliberate navigation interrupts a local camera blend, just as mouse drag does.
            IsTransitioning = false;
            var inverse = Quaternion.Inverse(Frame);
            var forward = Vector3.ProjectOnPlane(inverse * view.transform.forward, Vector3.up).normalized;
            var right = Vector3.ProjectOnPlane(inverse * view.transform.right, Vector3.up).normalized;
            Vector3 motion = (forward * input.y + right * input.x);
            if (motion.sqrMagnitude > 1) motion.Normalize();
            float speed = (Mode == CameraMode.Surface ? 5f : 25f) * (fast ? 3f : 1f);
            NavigationOffset += new Vector2(motion.x,motion.z) * speed * Mathf.Max(0,delta);
        }
        float targetDistance;
        public Quaternion Frame => LunarCoordinates.Frame(selected.Direction);
        public float DefaultFov { get; set; } = 42f;
        public float TargetFov { get; set; } = 42f;
        public bool IsLookingAtEarth { get; private set; } = false;
        public float earthYaw = 0f, earthPitch = 0f;
        public bool IsLookingAtSun { get; private set; } = false;
        public float sunYaw = 0f, sunPitch = 0f;
        LunarWorld world;

        public void OpticalZoom(float value)
        {
            if (Mathf.Abs(value) < 0.0001f) return;
            float step = Mathf.Sign(value) * Mathf.Max(Mathf.Abs(value) * 12f, 6f);
            TargetFov = Mathf.Clamp(TargetFov - step, 6f, 55f);
        }

        public void LookAtEarth()
        {
            IsLookingAtEarth = !IsLookingAtEarth;
            if (IsLookingAtEarth) IsLookingAtSun = false;
            earthYaw = 0f;
            earthPitch = 0f;
            if (IsLookingAtEarth)
            {
                var app = FindAnyObjectByType<LunarApplication>();
                if (app && Mode == CameraMode.Surface)
                {
                    var ed = Frame * app.Clock.Provider.EarthDirection(app.Clock.Utc);
                    surfaceYaw = Mathf.Repeat(Mathf.Atan2(ed.x, ed.z) * Mathf.Rad2Deg, 360f);
                    surfacePitch = Mathf.Asin(Mathf.Clamp(ed.y, -1f, 1f)) * Mathf.Rad2Deg;
                }
            }
        }

        public void LookAtSun()
        {
            IsLookingAtSun = !IsLookingAtSun;
            if (IsLookingAtSun) IsLookingAtEarth = false;
            sunYaw = 0f;
            sunPitch = 0f;
            if (IsLookingAtSun)
            {
                var app = FindAnyObjectByType<LunarApplication>();
                if (app && Mode == CameraMode.Surface)
                {
                    var sd = Frame * app.Clock.Provider.SunDirection(app.Clock.Utc);
                    surfaceYaw = Mathf.Repeat(Mathf.Atan2(sd.x, sd.z) * Mathf.Rad2Deg, 360f);
                    surfacePitch = Mathf.Asin(Mathf.Clamp(sd.y, -1f, 1f)) * Mathf.Rad2Deg;
                }
            }
        }

        void Start()
        {
            world=FindAnyObjectByType<LunarWorld>();
            if(world && world.globe && world.globe.TryGetComponent<MeshFilter>(out var mesh))
            {
                surfaceCollider=world.globe.GetComponent<MeshCollider>();
                if(!surfaceCollider) surfaceCollider=world.globe.AddComponent<MeshCollider>();
                surfaceCollider.sharedMesh=mesh.sharedMesh;
            }
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
            IsLookingAtEarth = false;
            earthYaw = 0f;
            earthPitch = 0f;
            IsLookingAtSun = false;
            sunYaw = 0f;
            sunPitch = 0f;
            TargetFov = DefaultFov;
            if (view) view.fieldOfView = DefaultFov;
            startPosition=view.transform.position;startRotation=view.transform.rotation;
            Mode=mode;elapsed=0;Progress=0;inertia=Vector2.zero;NavigationOffset=Vector2.zero;
            duration=mode==CameraMode.Approach?config.approachDuration:config.transitionDuration;
            IsTransitioning=true;
            if(mode==CameraMode.LocalOrbit||mode==CameraMode.Approach) { localDistance=config.localStart;localPitch=28; }
            if(mode==CameraMode.Global) { yaw=-45;pitch=-60;distance=targetDistance=config.globalStart; }
        }
        public void Focus()
        {
            IsLookingAtEarth = false;
            earthYaw = 0f;
            earthPitch = 0f;
            IsLookingAtSun = false;
            sunYaw = 0f;
            sunPitch = 0f;
            TargetFov = DefaultFov;
            if (view) view.fieldOfView = DefaultFov;
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
            IsLookingAtEarth = false;
            earthYaw = 0f;
            earthPitch = 0f;
            IsLookingAtSun = false;
            sunYaw = 0f;
            sunPitch = 0f;
            TargetFov = DefaultFov;
            if (view) view.fieldOfView = DefaultFov;
            NavigationOffset=Vector2.zero;localYaw=20;localPitch=28;localDistance=config.localStart;surfaceYaw=45;surfacePitch=3;
            if(Mode==CameraMode.Global){ChangeMode(CameraMode.Global);yaw=-45;pitch=-60;}
            else ChangeMode(Mode==CameraMode.Approach?CameraMode.LocalOrbit:Mode);
        }
        public void Skip() { if(Mode==CameraMode.Approach){elapsed=duration; Tick(0);} }
        public void SetHeight(float height) { ObserverHeight=Mathf.Clamp(height,.5f,12); }
        public void Drag(Vector2 delta)
        {
            if(Mode==CameraMode.Approach)return;
            IsTransitioning=false;
            if (IsLookingAtEarth)
            {
                if (Mode == CameraMode.Surface)
                {
                    surfaceYaw += delta.x * .12f;
                    surfacePitch = Mathf.Clamp(surfacePitch - delta.y * .12f, -85, 85);
                }
                else
                {
                    earthYaw += delta.x * .17f;
                    earthPitch = Mathf.Clamp(earthPitch - delta.y * .17f, -85f, 85f);
                }
                return;
            }
            if (IsLookingAtSun)
            {
                if (Mode == CameraMode.Surface)
                {
                    surfaceYaw += delta.x * .12f;
                    surfacePitch = Mathf.Clamp(surfacePitch - delta.y * .12f, -85, 85);
                }
                else
                {
                    sunYaw += delta.x * .17f;
                    sunPitch = Mathf.Clamp(sunPitch - delta.y * .17f, -85f, 85f);
                }
                return;
            }
            if(Mode==CameraMode.Global){yaw-=delta.x*.17f;pitch=Mathf.Clamp(pitch-delta.y*.17f,-89,89);inertia=-delta*.17f;}
            else if(Mode==CameraMode.LocalOrbit){localYaw-=delta.x*.2f;localPitch=Mathf.Clamp(localPitch-delta.y*.18f,7,88);}
            else {surfaceYaw+=delta.x*.12f;surfacePitch=Mathf.Clamp(surfacePitch-delta.y*.12f,-85,85);}
        }
        public void Zoom(float value)
        {
            if(Mode==CameraMode.Approach)return;
            if(Mode==CameraMode.Global)targetDistance=Mathf.Clamp(targetDistance*Mathf.Exp(-value*.22f),config.globalMin,config.globalMax);
            else if(Mode==CameraMode.LocalOrbit)localDistance=Mathf.Clamp(localDistance*Mathf.Exp(-value*.22f),config.localMin,config.localMax);
            else SetHeight(ObserverHeight+value*1.0f);
        }
        void LateUpdate(){Tick(Time.unscaledDeltaTime);}
        public void Tick(float delta)
        {
            if(!view||!selected)return;
            view.fieldOfView = Mathf.Lerp(view.fieldOfView, TargetFov, 1f - Mathf.Exp(-delta * 12f));
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
            if (surfaceCollider && surfaceCollider.Raycast(new Ray(pos.normalized*(config.radius*5),-pos.normalized),out var surface,config.radius*10))
            {
                float clearance = surface.point.magnitude + .6f;
                if (pos.magnitude < clearance) pos = pos.normalized * clearance;
            }
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
            return NavigationAnchor+Frame*(new Vector3(Mathf.Sin(a)*Mathf.Cos(b),Mathf.Sin(b),-Mathf.Cos(a)*Mathf.Cos(b))*localDistance);
        }
        void GetDesired(out Vector3 pos,out Quaternion rot)
        {
            if (!world) world = FindAnyObjectByType<LunarWorld>();
            if(Mode==CameraMode.Global)
            {
                pos=GlobalPosition();
                if (IsLookingAtEarth && world && world.earth)
                    rot=Look(pos, world.earth.position, Vector3.up) * Quaternion.Euler(earthPitch, earthYaw, 0f);
                else if (IsLookingAtSun && world && world.sun)
                    rot=Look(pos, world.sun.position, Vector3.up) * Quaternion.Euler(sunPitch, sunYaw, 0f);
                else
                    rot=Look(pos,Vector3.down*90,Vector3.up);
            }
            else if(Mode==CameraMode.Surface)
            {
                pos=NavigationAnchor+selected.Direction*ObserverHeight;
                rot=Frame*Quaternion.Euler(surfacePitch,surfaceYaw,0);
            }
            else 
            {
                pos=LocalPosition();
                if (IsLookingAtEarth && world && world.earth)
                    rot=Look(pos, world.earth.position, selected.Direction) * Quaternion.Euler(earthPitch, earthYaw, 0f);
                else if (IsLookingAtSun && world && world.sun)
                    rot=Look(pos, world.sun.position, selected.Direction) * Quaternion.Euler(sunPitch, sunYaw, 0f);
                else
                    rot=Look(pos,NavigationAnchor,selected.Direction);
            }
        }
        static Quaternion Look(Vector3 from,Vector3 to,Vector3 up)
        { var f=(to-from).normalized; if(Vector3.Cross(f,up).sqrMagnitude<.0001f)up=Vector3.forward;return Quaternion.LookRotation(f,up); }
        void ApplyPose(Vector3 pos,Quaternion rot){view.transform.SetPositionAndRotation(pos,rot);}
    }
}
