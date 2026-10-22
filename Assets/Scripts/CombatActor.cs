using System.Linq;
using UnityEngine;

namespace Mist
{
    public sealed class CombatActor
    {
        public readonly GameObject root,shadow;
        public readonly int kind;
        public readonly Transform rig,body,head,arm,elbow,leftArm,leftElbow,weapon,cape,capeTip;
        readonly Transform[] legs,shins;
        readonly Renderer[] renderers;
        readonly MaterialPropertyBlock block=new MaterialPropertyBlock();
        readonly Quaternion weaponHome;
        readonly Vector3 rigHome;
        public Vector2 position,deathDirection;
        public bool dead;
        public float deathAge,reaction,deathSpin;
        float lastFlash=-1,lastDissolve=-1;
        public CombatActor(GameObject model,GameObject contactShadow,int characterKind)
        {
            root=model;shadow=contactShadow;kind=characterKind;rig=root.transform.Find("Rig");body=rig.Find("Body");head=body?body.Find("Head"):null;
            arm=body?body.Find("ArmR"):null;leftArm=body?body.Find("ArmL"):null;elbow=arm?arm.Find("Forearm"):null;leftElbow=leftArm?leftArm.Find("Forearm"):null;
            weapon=elbow?elbow.Find("Weapon"):arm?arm.Find("Weapon"):null;weaponHome=weapon?weapon.localRotation:Quaternion.identity;rigHome=rig.localPosition;
            cape=body?body.Find("ScarfTail"):null;capeTip=cape?cape.Find("CapeTip"):null;
            legs=new[]{"LegL","LegR","Leg0","Leg1","Leg2","Leg3"}.Select(n=>rig.Find(n)).Where(t=>t).ToArray();shins=legs.Select(t=>t.Find("Shin")).ToArray();
            renderers=root.GetComponentsInChildren<Renderer>().Where(r=>r.enabled).ToArray();
        }
        public void Tint(float flash,float dissolve=0)
        {
            if(Mathf.Abs(lastFlash-flash)<.0001f&&Mathf.Abs(lastDissolve-dissolve)<.0001f)return;lastFlash=flash;lastDissolve=dissolve;
            block.SetFloat("_ImpactFlash",flash);block.SetFloat("_Dissolve",dissolve);
            foreach(var r in renderers)if(r)r.SetPropertyBlock(block);
        }
        public void Apply(CombatMotion.Pose pose,Vector2 at,float height,float facing,float walk,float clock,bool dodge,float hurt,float dt)
        {
            position=at;float sign=facing>=0?1:-1;root.transform.position=World.P(at,height)+new Vector3(pose.offset.x*sign,pose.offset.y,pose.offset.z);root.transform.localScale=new Vector3(sign,1,1);
            float recoil=Mathf.Clamp01(reaction/.18f);reaction=Mathf.Max(0,reaction-dt);
            rig.localPosition=rigHome;rig.localRotation=Quaternion.Euler(dodge?-15:0,-20,dodge?-28:-recoil*(kind>=4?5:14));
            if(body)body.localRotation=Quaternion.Euler(pose.body+new Vector3(-recoil*7,0,-recoil*5));
            if(head)head.localRotation=Quaternion.Euler(pose.head+new Vector3(recoil*5,0,recoil*3));
            if(arm)arm.localRotation=Quaternion.Euler(pose.arm);if(elbow)elbow.localRotation=Quaternion.Euler(pose.elbow);
            if(leftArm)leftArm.localRotation=Quaternion.Euler(pose.leftArm);if(leftElbow)leftElbow.localRotation=Quaternion.Euler(pose.leftElbow);
            if(weapon)weapon.localRotation=weaponHome*Quaternion.Euler(pose.weapon);
            float stride=Mathf.Sin(clock*10.5f)*walk;
            for(int i=0;i<legs.Length;i++){
                float swing=stride*(i%2==0?1:-1);legs[i].localRotation=Quaternion.Euler(swing*25+pose.crouch*.35f,0,(i%2==0?-1:1)*pose.crouch*.15f);
                if(shins[i])shins[i].localRotation=Quaternion.Euler(-Mathf.Max(0,swing)*35-pose.crouch*.7f,0,0);
            }
            if(cape)cape.localRotation=Quaternion.Euler(Mathf.Sin(clock*3)*2+walk*4,Mathf.Sin(clock*3.5f)*walk*5,Mathf.Sin(clock*2.8f)*2);
            if(capeTip)capeTip.localRotation=Quaternion.Euler(Mathf.Sin(clock*3-1)*3+walk*4,0,Mathf.Sin(clock*2.8f-1)*3);
            for(int i=-1;i<=1;i+=2){var wing=body?body.Find(i<0?"WingL":"WingR"):null;if(wing)wing.localRotation=Quaternion.Euler(0,i*Mathf.Sin(clock*7)*22,0);}
            if(shadow){shadow.transform.position=World.P(at,.025f);float width=kind>=4?1.12f:kind==3?.85f:.62f;float lift=Mathf.Clamp(1-height*.22f,.55f,1);shadow.transform.localScale=new Vector3(width*lift,1,(kind==3?.38f:.36f)*lift);}
            Tint(Mathf.Clamp01(hurt/.16f)*.88f);
        }
        public void EnemyPose(Enemy e,float clock,float dt)
        {
            float wind=e.action>0?1-e.action/Mathf.Max(.001f,e.actionDuration):0;
            float recovery=e.action<=0&&e.recovery>0?Mathf.Clamp01(e.recovery/(e.kind>=4?.55f:.22f)):0;
            var p=CombatMotion.Rest(clock,e.action>0||e.stun>0||e.recovery>0?0:.5f);
            float prep=wind*wind,impact=Mathf.Sin(recovery*Mathf.PI*.5f);
            if(e.kind==2){p.arm=new Vector3(-35-prep*48,0,-28);p.leftArm=new Vector3(-25-prep*40,0,25);p.body=new Vector3(-prep*8,0,0);p.offset.y=.19f+Mathf.Sin(clock*3)*.07f;}
            else if(e.kind==3){p.offset.y=-prep*.13f;p.body=new Vector3(prep*12,0,0);p.offset.x=impact*.65f;p.crouch=prep*10;}
            else if(e.kind==5){p.arm=new Vector3(-prep*55,0,-prep*20);p.leftArm=new Vector3(-prep*55,0,prep*20);p.body=new Vector3(-prep*8+impact*10,0,0);}
            else if(e.kind==6){p.body=new Vector3(0,Mathf.Sin(clock*1.3f)*4,prep*7-impact*9);p.arm=new Vector3(-prep*80+impact*35,0,-20);p.leftArm=new Vector3(-prep*65,0,20);p.offset.y=Mathf.Sin(clock*2)*.035f;}
            else{p.arm=new Vector3(-prep*(e.kind==4?125:85)+impact*60,prep*20,-prep*12);p.elbow=new Vector3(-prep*35,0,0);p.body=new Vector3(-prep*6+impact*10,-prep*12+impact*20,0);p.offset.x=impact*.13f;}
            Apply(p,e.pos,0,e.facing,e.action>0||e.stun>0||e.recovery>0?0:.5f,clock,false,e.flash,dt);
            if(e.action>0&&wind>.76f)Tint((wind-.76f)*.6f);
        }
        public void StartDeath(Vector2 direction){dead=true;deathAge=0;deathDirection=direction;deathSpin=direction.x>=0?-1:1;Tint(1);}
        public bool TickDeath(float dt)
        {
            deathAge+=dt;float t=deathAge;float distance=(kind>=4?.22f:.65f)*(1-Mathf.Exp(-t*8));
            root.transform.position=World.P(position+deathDirection*distance,Mathf.Sin(Mathf.Clamp01(t/.48f)*Mathf.PI)*(kind>=4?.08f:.28f));
            rig.localRotation=Quaternion.Euler(0,-20,deathSpin*Mathf.Min(1,t*4)*(kind>=4?32:78));
            Tint(Mathf.Max(0,1-t*12),Mathf.InverseLerp(.25f,.72f,t));
            if(shadow)shadow.transform.localScale*=Mathf.Max(0,1-dt*2);
            return t>.76f;
        }
    }
}
