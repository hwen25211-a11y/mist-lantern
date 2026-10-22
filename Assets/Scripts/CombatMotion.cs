using UnityEngine;

namespace Mist
{
    // The contact pose is shared with the simulation's windup boundary. Each
    // weapon has anticipation, a short accelerating strike, and a settling tail.
    public static class CombatMotion
    {
        public struct Pose
        {
            public Vector3 body,arm,elbow,leftArm,leftElbow,head,offset,weapon;
            public float crouch;
            public static Pose Blend(Pose a,Pose b,float t){
                t=Mathf.Clamp01(t);return new Pose{body=Vector3.Lerp(a.body,b.body,t),arm=Vector3.Lerp(a.arm,b.arm,t),elbow=Vector3.Lerp(a.elbow,b.elbow,t),leftArm=Vector3.Lerp(a.leftArm,b.leftArm,t),leftElbow=Vector3.Lerp(a.leftElbow,b.leftElbow,t),head=Vector3.Lerp(a.head,b.head,t),offset=Vector3.Lerp(a.offset,b.offset,t),weapon=Vector3.Lerp(a.weapon,b.weapon,t),crouch=Mathf.Lerp(a.crouch,b.crouch,t)};
            }
        }
        static Vector3 V(float x=0,float y=0,float z=0){return new Vector3(x,y,z);}
        static float Ease(float t){t=Mathf.Clamp01(t);return t*t*(3-2*t);}
        public static Pose Rest(float clock,float walk)
        {
            float stride=Mathf.Sin(clock*10.5f)*walk,breathe=Mathf.Sin(clock*2.3f);
            return new Pose{body=V(breathe*.7f,stride*3,-stride*1.5f),arm=V(-8+stride*10,0,-2),elbow=V(-12),leftArm=V(3-stride*6,0,4),leftElbow=V(-5),head=V(-breathe*.4f,-stride,0),offset=V(0,Mathf.Abs(stride)*.028f+breathe*.006f)};
        }
        static Pose WeaponRest(int weapon,float clock,float walk)
        {
            var p=Rest(clock,walk);
            if(weapon>0){p.arm=V(18+Mathf.Sin(clock*10.5f)*walk*8,0,-9);p.elbow=V(10);p.weapon=V(0,0,weapon==1?55:45);}
            return p;
        }
        public static Pose Sample(int weapon,int combo,bool skill,float phase,float charge,float clock=0,float walk=0)
        {
            var rest=WeaponRest(weapon,clock,walk);var ready=rest;var hit=rest;var follow=rest;
            if(weapon==0){
                if(combo==2){
                    ready.body=V(0,15,2);ready.arm=V(25,5,8);ready.elbow=V(15);ready.weapon=V(0,0,12);ready.offset=V(-.04f,-.04f);
                    hit.body=V(3,-10,-4);hit.arm=V(55,-15,66);hit.elbow=V(10);hit.weapon=V(0,0,-18);hit.offset=V(.11f,.015f);
                    follow.body=V(-2,-18,-5);follow.arm=V(78,-10,94);follow.elbow=V(12);follow.weapon=V(0,0,-28);follow.offset=V(.08f,.035f);
                }else if(combo==3||skill){
                    ready.body=V(-5,-12,-5);ready.arm=V(-105,8,-12);ready.elbow=V(-30);ready.weapon=V(0,0,-12);ready.offset=V(-.07f,.035f);
                    hit.body=V(8,15,10);hit.arm=V(38,-8,66);hit.elbow=V(10);hit.weapon=V(0,0,4);hit.offset=V(.24f,-.08f);hit.crouch=10;
                    follow.body=V(10,20,9);follow.arm=V(25,-10,78);follow.elbow=V(12);follow.offset=V(.18f,-.04f);follow.crouch=5;
                }else{
                    ready.body=V(0,-14,-4);ready.arm=V(15,-15,-22);ready.elbow=V(20);ready.offset=V(-.045f);
                    hit.body=V(4,15,7);hit.arm=V(35,-8,66);hit.elbow=V(10);hit.offset=V(.13f,-.025f);
                    follow.body=V(5,22,7);follow.arm=V(55,-15,87);follow.elbow=V(12);follow.offset=V(.10f);
                }
            }else if(weapon==1){
                ready.body=V(-7,-12,-6);ready.arm=V(-116,0,-8);ready.elbow=V(-32);ready.weapon=V(0,0,180);ready.offset=V(-.10f,-.06f);ready.crouch=8;
                hit.body=V(12,18,12);hit.arm=V(42,-8,64);hit.elbow=V(10);hit.weapon=V(0,0,skill?-174:-149);hit.offset=V(.22f,-.14f);hit.crouch=16;
                follow.body=V(10,20,9);follow.arm=V(22,-5,44);follow.elbow=V(12);follow.weapon=V(0,0,-168);follow.offset=V(.17f,-.09f);follow.crouch=10;
            }else{
                ready.body=V(-3,-10,-3);ready.arm=V(15,-12,8);ready.elbow=V(12);ready.weapon=V(0,0,-35);ready.offset=V(-.04f);
                hit.body=V(2,9,4);hit.arm=V(32,-8,58);hit.elbow=V(10,0,-4);hit.weapon=V(0,0,-120);hit.offset=V(.07f);
                follow.body=V(-1,3,2);follow.arm=V(25,-8,48);follow.elbow=V(15);follow.weapon=V(0,0,-105);follow.offset=V(.03f);
            }
            hit.leftArm=V(-4,0,-4);follow.leftArm=V(4,0,10);ready.head=V(-2,5,1);hit.head=V(-3,-4,-2);
            if(phase<0)return weapon==1&&charge>0?Pose.Blend(rest,ready,Ease(charge/.55f)):rest;
            if(phase<.32f)return Pose.Blend(weapon==1&&charge>0?Pose.Blend(rest,ready,Ease(charge/.55f)):rest,ready,Ease(phase/.32f));
            if(phase<.45f)return Pose.Blend(ready,hit,Mathf.Pow((phase-.32f)/.13f,2));
            if(phase<.61f)return Pose.Blend(hit,follow,Ease((phase-.45f)/.16f));
            return Pose.Blend(follow,WeaponRest(weapon,clock,walk*.3f),Ease((phase-.61f)/.39f));
        }
        public static float Phase(Run s){return s.actionDuration<=0?-1:s.actionAge<s.windup?s.actionAge/Mathf.Max(.001f,s.windup)*.45f:.45f+(s.actionAge-s.windup)/Mathf.Max(.001f,s.actionDuration-s.windup)*.55f;}
    }
}
