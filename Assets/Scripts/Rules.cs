using System;
using System.Collections.Generic;
using UnityEngine;

namespace Mist
{
    public enum Phase { Playing, Reward, Route, Rest, Lost, Won }
    [Serializable] public class Enemy
    {
        public int id, kind;
        public Vector2 pos;
        public float hp, maxHp, clock, stun, flash;
        public bool shadow;
        public float facing=1, action, actionDuration, recovery;
        public int moveIndex;
        public Vector2 target;
    }
    [Serializable] public class Danger
    {
        public Vector2 pos;
        public float radius, width, depth, age, wait, damage;
        public bool hit;
        public int kind,owner;
        public bool lampTarget;
    }
    [Serializable] public class Bolt
    {
        public Vector2 pos, velocity;
        public float life=2, damage;
        public bool empowered;
        public int pierce;
        public List<int> hitIds=new List<int>();
    }
    [Serializable] public class Run
    {
        public int version=2, seed, room, weapon, kills, potions=3, earned, nextId, combo, rewardCount, wave, waves;
        public int maxHp=120;
        public uint random;
        public float hp=120, oil=100, elapsed, attackCd, dodgeCd, dodgeTime, invulnerable, skillCd, jump, jumpVelocity, comboClock, swing, skillFlash;
        public Vector2 pos, direction=Vector2.right, dodgeDir=Vector2.right, lampPos, attackDirection;
        public float actionAge,actionDuration,windup,hitStop,guardTime,guardCooldown,lampCooldown,charge,resource,hurtFlash,skillBuffer,dodgeBuffer;
        public bool actionResolved,skillAction,guarding,lampPlaced;
        public int perfectGuards;
        public bool bright=true, elite, settled;
        public Phase phase;
        public int[] relics=new int[12];
        public int[] offers=new int[3];
        public List<Enemy> enemies=new List<Enemy>();
        public List<Danger> dangers=new List<Danger>();
        public List<Bolt> bolts=new List<Bolt>();
    }
    [Serializable] public class Profile
    {
        public int version=1, embers, vitality, power, flask, runs, wins, bestRoom;
    }
    public struct InputFrame
    {
        public Vector2 move;
        public bool attack,dodge,jump,skill,toggle,potion,guard,place;
    }
    public struct Signal
    {
        public string kind;
        public Vector2 pos;
        public float amount;
        public Signal(string k,Vector2 p,float a=0){kind=k;pos=p;amount=a;}
    }
    public static class Catalog
    {
        public static readonly string[] Areas={"暮色旧桥","萤雾庭院","停摆钟楼"};
        public static readonly string[] Bosses={"守桥铜卫","荆冠园主","第十三声钟"};
        public static readonly string[] Weapons={"逐雾弯刀","铸钟重锤","引星灯杖"};
        public static readonly string[] WeaponInfo={
            "三段弯刃 · 精准格挡积攒锋意\n武器技：消耗锋意追斩破雾",
            "按住蓄力 · 松开或蓄满重击\n武器技：钟震破势，打断敌人",
            "命中蓄星 · 满星强化下一轮\n武器技：五道贯穿星焰"};
        public static readonly string[] RelicNames={"锋利灯芯","余温绷带","回流灯罩","折光铜镜","轻羽靴","旧城护符","深雾契约","满溢灯杯","余烬火花","复燃药瓶","鸣钟刻印","夜行围巾"};
        public static readonly string[] RelicText={
            "攻击伤害 +15%；破势后追加灯油。",
            "清理战斗房间时回复 8 点生命。",
            "暗灯回油每秒 +3；光区内命中回油。",
            "明灯对幽影的伤害额外 +35%。",
            "闪避冷却缩短 18%，移动速度 +8%。",
            "最大生命 +20，立即回复 20 生命。",
            "暗灯攻击伤害 +25%。",
            "武器技消耗的灯油减少 20%。",
            "连击末段 +12 伤害；近战末段激起余烬震波。",
            "获得一瓶药水；药水回复量 +12。",
            "武器技伤害 +30%；重锤破势延长。",
            "暗灯切明灯获得 0.7 秒无敌；重复获得不延长。"};
        public static readonly string[] EnemyNames={"瓷面游魂","持盾铜偶","夜蛾术士","雾犬","铜卫","园主","钟灵"};
    }
    public sealed class Simulation
    {
        public Run s;
        public Profile profile;
        public readonly List<Signal> signals=new List<Signal>();
        public Simulation(Run run,Profile p){s=run;profile=p;}
        public static Run NewRun(int seed,int weapon,Profile p)
        {
            return new Run{seed=seed,random=(uint)Math.Max(1,seed),weapon=Mathf.Clamp(weapon,0,2),maxHp=120+p.vitality*12,hp=120+p.vitality*12,potions=3+p.flask};
        }
        public int Rng(int max)
        {
            uint x=s.random;x^=x<<13;x^=x>>17;x^=x<<5;s.random=x;
            return (int)(x%(uint)max);
        }
        public static Vector2 Bound(Vector2 p){return new Vector2(Mathf.Clamp(p.x,-9.5f,9.5f),Mathf.Clamp(p.y,-3.4f,3.4f));}
        public int Area{get{return s.room/4;}}
        public bool BossRoom{get{return s.room%4==3;}}
        public void Enter()
        {
            s.enemies.Clear();s.dangers.Clear();s.bolts.Clear();s.pos=new Vector2(-6,0);s.direction=Vector2.right;
            s.attackCd=s.skillCd=s.dodgeCd=s.dodgeTime=s.jump=s.jumpVelocity=s.invulnerable=0;
            s.combo=s.rewardCount=0;s.oil=Mathf.Max(55,s.oil);
            s.actionAge=s.actionDuration=s.hitStop=s.guardTime=s.guardCooldown=s.charge=s.hurtFlash=0;
            s.guarding=s.lampPlaced=false;s.lampCooldown=s.skillBuffer=s.dodgeBuffer=0;
            s.phase=s.room%4==1?Phase.Rest:Phase.Playing;
            if(s.phase==Phase.Rest)return;
            s.wave=1;s.waves=BossRoom?1:s.elite?3:2;SpawnWave();
            Emit("enter",s.pos);
        }
        void SpawnWave()
        {
            s.enemies.Clear();s.dangers.Clear();s.bolts.Clear();
            int count=BossRoom?1:3+Area+(s.room%4==2?1:0)+(s.elite?1:0);
            for(int i=0;i<count;i++)
            {
                int kind=BossRoom?4+Area:Rng(Math.Min(4,2+Area+(s.room>0?1:0)));
                float hp=BossRoom?720+Area*380:kind==1?105:kind==3?58:72;
                hp*=s.elite?1.18f:1;
                float side=s.wave%2==0?-1:1;
                s.enemies.Add(new Enemy{id=++s.nextId,kind=kind,pos=new Vector2(side*(2+(i%3)*2.4f),(i%3-1)*2),hp=hp,maxHp=hp,clock=1.5f+i*.37f,shadow=kind==0||kind==2||kind==6});
            }
            s.invulnerable=Mathf.Max(s.invulnerable,.8f);
        }
        void Emit(string kind,Vector2 p,float amount=0){signals.Add(new Signal(kind,p,amount));}
        float Rel(int i){return s.relics[i];}
        public float DamageMultiplier{get{return (1+profile.power*.08f+Rel(0)*.15f)*(s.bright?1:1+Rel(6)*.25f);}}
        public float SkillCost{get{return Mathf.Max(12,35*(1-.2f*Rel(7)));}}
        public bool Lit(Vector2 p){return s.bright&&Vector2.Distance(p,s.lampPlaced?s.lampPos:s.pos)<=3.4f;}
        public void Tick(float dt,InputFrame input)
        {
            signals.Clear();if(s.phase!=Phase.Playing)return;dt=Mathf.Clamp(dt,0,.05f);
            if(s.hitStop>0){s.hitStop=Mathf.Max(0,s.hitStop-dt);dt=0;}
            s.skillBuffer=Mathf.Max(0,s.skillBuffer-dt);s.dodgeBuffer=Mathf.Max(0,s.dodgeBuffer-dt);
            if(input.skill){if(s.oil>=SkillCost)s.skillBuffer=.5f;else Emit("lowoil",s.pos);}
            if(input.dodge)s.dodgeBuffer=.18f;
            s.elapsed+=dt;s.attackCd=Mathf.Max(0,s.attackCd-dt);s.skillCd=Mathf.Max(0,s.skillCd-dt);
            s.dodgeCd=Mathf.Max(0,s.dodgeCd-dt);s.invulnerable=Mathf.Max(0,s.invulnerable-dt);
            s.guardCooldown=Mathf.Max(0,s.guardCooldown-dt);s.guardTime=Mathf.Max(0,s.guardTime-dt);
            s.lampCooldown=Mathf.Max(0,s.lampCooldown-dt);s.hurtFlash=Mathf.Max(0,s.hurtFlash-dt);
            s.skillFlash=Mathf.Max(0,s.skillFlash-dt);s.comboClock-=dt;if(s.comboClock<0)s.combo=0;
            s.oil=Mathf.Clamp(s.oil+dt*(s.bright?(s.lampPlaced?-3.3f:-2):7+3*Rel(2)),0,100);
            if(s.oil<=0&&s.bright){s.bright=false;Emit("empty",s.pos);}
            if(input.toggle){if(s.bright)s.bright=false;else if(s.oil>=12){s.bright=true;s.invulnerable=Mathf.Max(s.invulnerable,Rel(11)>0?.7f:0);}else Emit("lowoil",s.pos);Emit("toggle",s.lampPlaced?s.lampPos:s.pos);}
            if(input.place&&s.lampCooldown<=0){s.lampPlaced=!s.lampPlaced;s.lampPos=s.pos;s.lampCooldown=.4f;Emit(s.lampPlaced?"place":"recall",s.pos);}
            if(input.potion&&s.potions>0&&s.hp<s.maxHp){s.potions--;s.hp=Mathf.Min(s.maxHp,s.hp+46+Rel(9)*12);Emit("heal",s.pos,46+Rel(9)*12);}
            if(input.guard&&!s.guarding&&s.guardCooldown<=0&&s.actionDuration<=0&&s.dodgeTime<=0){s.guardTime=.22f;s.guardCooldown=.65f;Emit("guard",s.pos);}
            s.guarding=input.guard&&s.actionDuration<=0&&s.dodgeTime<=0;
            Vector2 move=Vector2.ClampMagnitude(input.move,1);
            if(s.dodgeBuffer>0&&s.dodgeCd<=0&&(s.actionDuration<=0||s.actionAge>=s.windup))
            {
                s.dodgeBuffer=0;s.dodgeTime=.27f;s.dodgeCd=Mathf.Max(.65f,1.1f*(1-.18f*Rel(4)));s.dodgeDir=move.sqrMagnitude>.1f?move:s.direction;
                s.invulnerable=.38f;s.actionDuration=0;s.swing=0;s.charge=0;s.guarding=false;Emit("dodge",s.pos);
            }
            if(input.jump&&s.jump<=0){s.jumpVelocity=6.3f;Emit("jump",s.pos);}
            if(s.jumpVelocity!=0||s.jump>0){s.jumpVelocity-=18*dt;s.jump=Mathf.Max(0,s.jump+s.jumpVelocity*dt);if(s.jump==0)s.jumpVelocity=0;}
            if(s.dodgeTime>0){s.dodgeTime=Mathf.Max(0,s.dodgeTime-dt);s.pos=Bound(s.pos+s.dodgeDir*13*dt);}
            else
            {
                float mobility=s.actionDuration>0?.35f:s.guarding?.4f:s.charge>0?.45f:1;
                s.pos=Bound(s.pos+move*(4.5f+Rel(4)*.36f)*mobility*dt);
                if(move.sqrMagnitude>.01f&&s.actionDuration<=0)s.direction=move.normalized;
                if(s.actionDuration<=0&&!s.guarding)
                {
                    if(s.skillBuffer>0&&s.skillCd<=0&&s.oil>=SkillCost){s.skillBuffer=0;s.oil-=SkillCost;s.skillCd=4.8f;s.charge=0;BeginAttack(true);}
                    else if(s.attackCd<=0)
                    {
                        if(s.weapon==1){if(input.attack){s.charge+=dt;if(s.charge>=.7f)BeginAttack(false);}else if(s.charge>0)BeginAttack(false);}
                        else if(input.attack)BeginAttack(false);
                    }
                }
            }
            if(s.actionDuration>0)
            {
                s.actionAge+=dt;s.swing=Mathf.Max(0,s.actionDuration-s.actionAge);
                if(!s.actionResolved&&s.actionAge>=s.windup){s.actionResolved=true;ResolveAttack();}
                if(s.actionAge>=s.actionDuration){s.actionDuration=0;s.swing=0;}
            }
            for(int i=s.bolts.Count-1;i>=0;i--)
            {
                var b=s.bolts[i];var before=b.pos;b.pos+=b.velocity*dt;b.life-=dt;
                foreach(var e in s.enemies)
                {
                    var segment=b.pos-before;float a=segment.sqrMagnitude>0?Mathf.Clamp01(Vector2.Dot(e.pos-before,segment)/segment.sqrMagnitude):0;
                    if(e.hp>0&&!b.hitIds.Contains(e.id)&&Vector2.Distance(before+segment*a,e.pos)<(e.kind>=4?.9f:.5f)){b.hitIds.Add(e.id);Hit(e,b.damage,b.empowered);if(b.pierce--<=0){b.life=0;break;}}
                }
                if(b.life<=0)s.bolts.RemoveAt(i);
            }
            foreach(var e in s.enemies)
            {
                e.flash=Mathf.Max(0,e.flash-dt);e.stun=Mathf.Max(0,e.stun-dt);e.action=Mathf.Max(0,e.action-dt);e.recovery=Mathf.Max(0,e.recovery-dt);
                if(e.hp<=0||e.stun>0||e.action>0||e.recovery>0)continue;
                bool bait=s.lampPlaced&&s.bright&&(e.kind==1||e.kind==3)&&Vector2.Distance(e.pos,s.lampPos)<5;
                Vector2 target=bait?s.lampPos:s.pos,delta=target-e.pos;float distance=delta.magnitude;e.facing=delta.x>=0?1:-1;e.clock-=dt;
                float range=e.kind==2?6:e.kind>=4?4:1.65f;
                if(distance>range){float speed=e.kind==1?1.35f:e.kind==3?3.1f:e.kind>=4?1.35f:1.9f;e.pos=Bound(e.pos+delta.normalized*speed*dt);}
                if(e.clock<=0&&distance<range+.5f){e.target=target;EnemyAttack(e,bait);e.clock=e.kind>=4?1.15f:1.25f;}
            }
            for(int i=0;i<s.enemies.Count;i++)for(int j=i+1;j<s.enemies.Count;j++)
            {var a=s.enemies[i];var b=s.enemies[j];if(a.hp<=0||b.hp<=0)continue;Vector2 d=a.pos-b.pos;float m=d.magnitude;if(m<.85f){Vector2 n=m<.01f?Vector2.right:d/m;a.pos=Bound(a.pos+n*dt);b.pos=Bound(b.pos-n*dt);}}
            for(int i=s.dangers.Count-1;i>=0;i--)
            {
                var h=s.dangers[i];h.age+=dt;
                if(h.age>=h.wait&&!h.hit)
                {
                    h.hit=true;
                    if(InDanger(s.pos,h)&&s.jump<.6f)Hurt(h.damage);
                    if(h.lampTarget&&s.lampPlaced&&InDanger(s.lampPos,h)){s.oil=Mathf.Max(0,s.oil-12);Emit("lampHit",s.lampPos);}
                    Emit("impact",h.pos,h.radius);
                }
                if(h.age>h.wait+.4f)s.dangers.RemoveAt(i);
            }
            if(s.hp<=0){s.hp=0;s.phase=Phase.Lost;Settle(false);Emit("lost",s.pos);return;}
            if(s.enemies.TrueForAll(e=>e.hp<=0))
            {
                if(s.wave<s.waves){s.wave++;SpawnWave();Emit("wave",s.pos,s.wave);return;}
                s.dangers.Clear();s.bolts.Clear();s.earned+=BossRoom?12:5+(s.elite?4:0);s.hp=Mathf.Min(s.maxHp,s.hp+Rel(1)*8);
                if(s.room==11){s.phase=Phase.Won;Settle(true);Emit("won",s.pos);}
                else{s.phase=Phase.Reward;RollOffers();Emit("clear",s.pos);if(BossRoom){s.hp=Mathf.Min(s.maxHp,s.hp+30);s.potions=Math.Min(8,s.potions+1);}}
            }
        }
        public static bool InDanger(Vector2 p,Danger h)
        {
            return h.kind==0?Vector2.Distance(p,h.pos)<=h.radius:Mathf.Abs(p.x-h.pos.x)<=h.width*.5f&&Mathf.Abs(p.y-h.pos.y)<=h.depth*.5f;
        }
        void Aim()
        {
            Enemy nearest=null;float best=s.weapon==2?12:3.4f;
            foreach(var e in s.enemies){float d=Vector2.Distance(e.pos,s.pos);if(e.hp>0&&d<best){best=d;nearest=e;}}
            if(nearest!=null&&(nearest.pos-s.pos).sqrMagnitude>.001f)s.direction=(nearest.pos-s.pos).normalized;
        }
        void BeginAttack(bool skill)
        {
            Aim();s.attackDirection=s.direction;s.combo=s.combo%3+1;s.comboClock=1.5f;s.skillAction=skill;s.actionAge=0;s.actionResolved=false;
            s.windup=skill?(s.weapon==1?.35f:.18f):s.weapon==0?.09f:s.weapon==1?.19f:.12f;
            s.actionDuration=skill?.65f:s.weapon==0?(s.combo==3?.48f:.34f):s.weapon==1?.62f:.43f;
            s.attackCd=s.actionDuration;s.swing=s.actionDuration;Emit("windup",s.pos);
        }
        void ResolveAttack()
        {
            bool skill=s.skillAction;Vector2 direction=s.attackDirection;
            float damage=(s.weapon==0?20:s.weapon==1?38+50*Mathf.Clamp01(s.charge/.7f):24)*(s.combo==3?1.4f:1)+(s.combo==3?Rel(8)*12:0);
            if(skill)damage=s.weapon==0?65+s.resource*.5f:s.weapon==1?105:38;
            if(s.weapon==2)
            {
                bool boosted=s.resource>=100;if(boosted)s.resource=0;int count=skill?5:boosted?3:1;
                for(int i=0;i<count;i++){float a=(i-(count-1)*.5f)*.13f;Vector2 d=new Vector2(direction.x*Mathf.Cos(a)-direction.y*Mathf.Sin(a),direction.x*Mathf.Sin(a)+direction.y*Mathf.Cos(a));s.bolts.Add(new Bolt{pos=s.pos+d*.18f,velocity=d*14,damage=damage,empowered=skill||boosted,pierce=skill?2:0});}
            }
            else
            {
                float range=skill?(s.weapon==1?3.7f:3.3f):s.weapon==1?2.65f:2.2f;
                bool hit=false;
                foreach(var e in s.enemies)
                {
                    Vector2 delta=e.pos-s.pos;
                    if(e.hp>0&&delta.magnitude<range+(e.kind>=4?.8f:.3f)&&(skill&&s.weapon==1||Vector2.Dot(delta.normalized,direction)>-.15f))
                    {Hit(e,damage,skill);e.pos=Bound(e.pos+delta.normalized*(e.kind>=4?(s.weapon==1?.12f:0):s.weapon==1?.65f:.12f));hit=true;}
                }
                if(hit)s.hitStop=skill||s.weapon==1?.065f:.035f;
                if(skill&&s.weapon==0)s.resource=0;
                if(s.combo==3&&Rel(8)>0){foreach(var e in s.enemies)if(e.hp>0&&Vector2.Distance(e.pos,s.pos)<3.2f)Hit(e,8*Rel(8),false);Emit("burst",s.pos);}
            }
            s.charge=0;s.skillFlash=skill?.45f:0;Emit(skill?"skill":"attack",s.pos,s.combo);
        }
        void Hit(Enemy e,float baseDamage,bool skill)
        {
            if(e.hp<=0)return;
            bool lit=Lit(e.pos);float damage=baseDamage*DamageMultiplier*(skill?1+.3f*Rel(10):1);
            if(e.shadow)damage*=lit?1.35f+.35f*Rel(3):.7f;
            bool shield=e.kind==1&&e.stun<=0&&!skill&&s.weapon!=1&&!lit;
            if(shield)damage*=.45f;
            e.hp=Mathf.Max(0,e.hp-damage);e.flash=.2f;
            bool stagger=e.kind<4&&!shield||skill&&s.weapon==1;
            if(stagger){e.stun=Mathf.Max(e.stun,s.weapon==1?.65f+Rel(10)*.15f:.14f);if(s.weapon==1||skill){e.action=0;s.dangers.RemoveAll(h=>h.owner==e.id&&!h.hit);s.oil=Mathf.Min(100,s.oil+Rel(0)*2);Emit("break",e.pos);}}
            s.resource=Mathf.Min(100,s.resource+(s.weapon==2?18:8));if(lit&&Rel(2)>0)s.oil=Mathf.Min(100,s.oil+Rel(2)*.8f);
            Emit(shield?"block":e.shadow&&lit?"reveal":"hit",e.pos,damage);
            if(e.hp<=0){s.kills++;s.oil=Mathf.Min(100,s.oil+5);s.dangers.RemoveAll(h=>h.owner==e.id&&!h.hit);Emit("kill",e.pos);}
        }
        public void Hurt(float damage)
        {
            if(s.invulnerable>0||s.dodgeTime>0)return;
            if(s.guarding&&s.guardTime>0){s.invulnerable=.3f;s.resource=Mathf.Min(100,s.resource+40);s.oil=Mathf.Min(100,s.oil+10);s.perfectGuards++;s.hitStop=.07f;Emit("parry",s.pos);foreach(var e in s.enemies)if(e.hp>0&&Vector2.Distance(e.pos,s.pos)<3){e.stun=.9f;e.action=0;}return;}
            if(s.guarding&&s.oil>=7){damage*=.3f;s.oil-=7;Emit("block",s.pos);}
            s.hp=Mathf.Max(0,s.hp-damage);s.invulnerable=.85f;s.hurtFlash=.3f;Emit("hurt",s.pos,damage);
        }
        void AddDanger(Enemy e,Vector2 pos,float radius,float wait,float damage,int kind=0,float width=0,float depth=0,bool lamp=false)
        {s.dangers.Add(new Danger{owner=e.id,pos=pos,radius=radius,wait=wait,damage=damage,kind=kind,width=width,depth=depth,lampTarget=lamp});}
        void EnemyAttack(Enemy e,bool lamp)
        {
            int step=e.moveIndex++%3;bool rage=e.hp<e.maxHp*.5f;float wait=e.kind==3?.65f:e.kind>=4?1.1f:.85f;
            e.action=e.actionDuration=wait;e.recovery=e.kind>=4?wait+.55f:wait+.22f;
            if(e.kind<4)
            {
                Vector2 at=e.kind==2||e.kind==3?e.target:e.pos+(e.target-e.pos).normalized*.9f;
                AddDanger(e,at,e.kind==3?1.15f:e.kind==2?1.45f:1.25f,wait,10+Area*3,0,0,0,lamp);
            }
            else if(e.kind==4)
            {
                if(step==0)AddDanger(e,e.target,2.1f,wait,22);
                else if(step==1)AddDanger(e,new Vector2(0,e.target.y),0,wait,24,1,21,1.5f);
                else{AddDanger(e,e.pos,3,wait,24);if(rage)AddDanger(e,e.target,1.6f,wait+.45f,18);}
            }
            else if(e.kind==5)
            {
                if(step==0){for(int i=-1;i<=1;i++)AddDanger(e,Bound(e.target+new Vector2(i*2.4f,0)),1.3f,wait+Mathf.Abs(i)*.18f,22);}
                else if(step==1){for(int i=0;i<(rage?4:3);i++)AddDanger(e,new Vector2(-6+i*4,e.target.y+(i%2==0?-1:1)),1.7f,wait+i*.16f,23);}
                else AddDanger(e,e.pos,3.7f,wait,27);
            }
            else
            {
                if(step==0){AddDanger(e,new Vector2(0,e.target.y),0,wait,26,1,21,1.3f);AddDanger(e,new Vector2(e.target.x,0),0,wait+.35f,26,1,1.5f,8);}
                else if(step==1){for(int i=0;i<(rage?6:4);i++){float a=i*6.283f/(rage?6:4);AddDanger(e,new Vector2(Mathf.Cos(a)*4.5f,Mathf.Sin(a)*2),1.45f,wait+i*.12f,22);}}
                else{AddDanger(e,e.target,2,wait,26);AddDanger(e,e.pos,2.7f,wait+.5f,26);}
            }
            Emit("warning",e.pos,step);
        }
        public void RollOffers()
        {
            for(int i=0;i<3;i++){int id;do{id=Rng(12);}while(Array.IndexOf(s.offers,id,0,i)>=0);s.offers[i]=id;}
        }
        public bool ChooseRelic(int choice)
        {
            if(s.phase!=Phase.Reward||choice<0||choice>2)return false;
            int id=s.offers[choice];s.relics[id]++;
            if(id==5){s.maxHp+=20;s.hp=Mathf.Min(s.maxHp,s.hp+20);}
            if(id==9)s.potions=Math.Min(8,s.potions+1);
            s.phase=Phase.Route;s.rewardCount++;return true;
        }
        public bool Rest(int choice)
        {
            if(s.phase!=Phase.Rest||choice<0||choice>2)return false;
            if(choice==0){s.hp=Mathf.Min(s.maxHp,s.hp+55);s.potions=Math.Min(8,s.potions+1);}
            else if(choice==1){s.relics[0]++;s.oil=100;}
            else{s.weapon=(s.weapon+1)%3;s.hp=Mathf.Min(s.maxHp,s.hp+20);}
            s.phase=Phase.Route;return true;
        }
        public bool Travel(bool elite)
        {
            if(s.phase!=Phase.Route||s.room>=11)return false;
            s.room++;s.elite=elite&&s.room%4!=3;Enter();return true;
        }
        public void Settle(bool victory)
        {
            if(s.settled)return;
            s.settled=true;profile.embers+=s.earned+(victory?25:Math.Min(8,s.kills/2));
            profile.runs++;if(victory)profile.wins++;profile.bestRoom=Math.Max(profile.bestRoom,s.room+1);
        }
    }
    [Serializable] public class SaveData
    {
        public int schema=1;
        public Profile profile=new Profile();
        public Run run;
    }
    public static class Saves
    {
        public static bool IsTest=Array.Exists(Environment.GetCommandLineArgs(),x=>x=="-silent-test"||x=="-qa");
        public static string Key{get{return IsTest?"MistLantern.QA.v1":"MistLantern.Save.v1";}}
        public static string Error="";
        public static SaveData Read()
        {
            Error="";
            var primary=Decode(PlayerPrefs.GetString(Key,""));
            if(primary!=null)return primary;
            var backup=Decode(PlayerPrefs.GetString(Key+".backup",""));
            if(backup!=null){Error="上次保存未能读取，已恢复备份。";return backup;}
            if(PlayerPrefs.HasKey(Key))Error="存档无法读取。可以重新开始；原始数据仍保留。";
            return new SaveData();
        }
        public static SaveData Decode(string json)
        {
            try
            {
                if(string.IsNullOrEmpty(json)||!json.Contains("\"schema\""))return null;
                var d=JsonUtility.FromJson<SaveData>(json);
                if(d==null||d.schema!=1||d.profile==null||d.profile.embers<0||d.profile.vitality<0||d.profile.vitality>5||d.profile.power<0||d.profile.power>5||d.profile.flask<0||d.profile.flask>2)return null;
                var r=d.run;
                if(r!=null&&((r.version!=1&&r.version!=2)||r.room<0||r.room>11||r.weapon<0||r.weapon>2||r.maxHp<1||r.hp<0||r.hp>r.maxHp||float.IsNaN(r.hp)||float.IsInfinity(r.hp)||r.random==0||r.relics==null||r.relics.Length!=12||r.offers==null||r.offers.Length!=3||r.enemies==null||r.dangers==null||r.bolts==null||!Enum.IsDefined(typeof(Phase),r.phase)))return null;
                if(r!=null){foreach(var bolt in r.bolts)if(bolt.hitIds==null)bolt.hitIds=new List<int>();}
                if(r!=null&&r.version==1){r.version=2;r.actionDuration=r.actionAge=r.hitStop=r.charge=0;r.lampPlaced=r.guarding=false;}
                return d;
            }catch{return null;}
        }
        public static void Write(SaveData d)
        {
            string old=PlayerPrefs.GetString(Key,"");
            if(Decode(old)!=null)PlayerPrefs.SetString(Key+".backup",old);
            PlayerPrefs.SetString(Key,JsonUtility.ToJson(d));PlayerPrefs.Save();
        }
    }
}
