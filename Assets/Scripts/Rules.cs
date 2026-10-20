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
        public float facing=1;
    }
    [Serializable] public class Danger
    {
        public Vector2 pos;
        public float radius, width, depth, age, wait, damage;
        public bool hit;
        public int kind;
    }
    [Serializable] public class Bolt
    {
        public Vector2 pos, velocity;
        public float life=2, damage;
        public bool empowered;
    }
    [Serializable] public class Run
    {
        public int version=1, seed, room, weapon, kills, potions=3, earned, nextId, combo, rewardCount, wave, waves;
        public int maxHp=120;
        public uint random;
        public float hp=120, oil=100, elapsed, attackCd, dodgeCd, dodgeTime, invulnerable, skillCd, jump, jumpVelocity, comboClock, swing, skillFlash;
        public Vector2 pos, direction=Vector2.right, dodgeDir=Vector2.right;
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
        public bool attack,dodge,jump,skill,toggle,potion;
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
            "迅捷三连击 · 第三击伤害更高\n武器技：向前斩出一道破雾弧光",
            "挥击较慢 · 范围大并击退敌人\n武器技：震击周围，打断蓄力",
            "远程灯弹 · 保持距离持续输出\n武器技：向前发射五道星焰"};
        public static readonly string[] RelicNames={"锋利灯芯","余温绷带","回流灯罩","折光铜镜","轻羽靴","旧城护符","深雾契约","满溢灯杯","余烬火花","复燃药瓶","鸣钟刻印","夜行围巾"};
        public static readonly string[] RelicText={
            "所有攻击伤害 +15%。",
            "清理战斗房间时回复 8 点生命。",
            "暗灯状态下，灯油恢复每秒 +3。",
            "明灯对幽影的伤害额外 +35%。",
            "闪避冷却缩短 18%，移动速度 +8%。",
            "最大生命 +20，立即回复 20 生命。",
            "暗灯攻击伤害 +25%。",
            "武器技消耗的灯油减少 20%。",
            "三连击的最后一击额外造成 12 伤害。",
            "获得一瓶药水；药水回复量 +12。",
            "武器技伤害 +30%。",
            "从暗灯切至明灯时，获得 0.7 秒无敌。"};
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
                float hp=BossRoom?850+Area*450:kind==1?105:kind==3?58:72;
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
        public void Tick(float dt,InputFrame input)
        {
            signals.Clear();
            if(s.phase!=Phase.Playing)return;
            dt=Mathf.Clamp(dt,0,.05f);s.elapsed+=dt;
            s.attackCd=Mathf.Max(0,s.attackCd-dt);s.skillCd=Mathf.Max(0,s.skillCd-dt);
            s.dodgeCd=Mathf.Max(0,s.dodgeCd-dt);s.invulnerable=Mathf.Max(0,s.invulnerable-dt);
            s.swing=Mathf.Max(0,s.swing-dt);s.skillFlash=Mathf.Max(0,s.skillFlash-dt);s.comboClock-=dt;
            if(s.comboClock<0)s.combo=0;
            s.oil=Mathf.Clamp(s.oil+dt*(s.bright?-2.5f:7+3*Rel(2)),0,100);
            if(s.oil<=0&&s.bright){s.bright=false;Emit("empty",s.pos);}
            if(input.toggle)
            {
                if(s.bright){s.bright=false;Emit("toggle",s.pos);}
                else if(s.oil>=12){s.bright=true;s.invulnerable=Mathf.Max(s.invulnerable,Rel(11)>.5f?.7f:0);Emit("toggle",s.pos);}
                else Emit("lowoil",s.pos);
            }
            if(input.potion&&s.potions>0&&s.hp<s.maxHp)
            {s.potions--;s.hp=Mathf.Min(s.maxHp,s.hp+46+Rel(9)*12);Emit("heal",s.pos,46+Rel(9)*12);}
            Vector2 move=Vector2.ClampMagnitude(input.move,1);
            if(input.dodge&&s.dodgeCd<=0)
            {
                s.dodgeTime=.27f;s.dodgeCd=Mathf.Max(.65f,1.1f*(1-.18f*Rel(4)));
                s.dodgeDir=move.sqrMagnitude>.1f?move:s.direction;s.invulnerable=.38f;Emit("dodge",s.pos);
            }
            if(input.jump&&s.jump<=0){s.jumpVelocity=6.3f;Emit("jump",s.pos);}
            if(s.jumpVelocity!=0||s.jump>0){s.jumpVelocity-=18*dt;s.jump=Mathf.Max(0,s.jump+s.jumpVelocity*dt);if(s.jump==0)s.jumpVelocity=0;}
            if(s.dodgeTime>0){s.dodgeTime-=dt;s.pos=Bound(s.pos+s.dodgeDir*13*dt);}
            else
            {
                s.pos=Bound(s.pos+move*(4.5f+Rel(4)*.36f)*dt);
                if(move.sqrMagnitude>.01f)s.direction=move.normalized;
                if(input.skill&&s.skillCd<=0)
                {
                    if(s.oil>=SkillCost){s.oil-=SkillCost;s.skillCd=4.8f;Skill();}
                    else if(s.attackCd<=0){Emit("lowoil",s.pos);s.attackCd=.15f;}
                }
                if(input.attack&&s.attackCd<=0)Attack();
            }
            for(int i=s.bolts.Count-1;i>=0;i--)
            {
                var b=s.bolts[i];b.pos+=b.velocity*dt;b.life-=dt;
                foreach(var e in s.enemies)
                {
                    if(e.hp>0&&Vector2.Distance(b.pos,e.pos)<(e.kind>=4?.95f:.55f))
                    {Hit(e,b.damage,b.empowered);b.life=0;break;}
                }
                if(b.life<=0)s.bolts.RemoveAt(i);
            }
            foreach(var e in s.enemies)
            {
                e.flash=Mathf.Max(0,e.flash-dt);e.stun=Mathf.Max(0,e.stun-dt);
                if(e.hp<=0||e.stun>0)continue;
                Vector2 delta=s.pos-e.pos;float distance=delta.magnitude;
                e.facing=delta.x>=0?1:-1;e.clock-=dt;
                float range=e.kind==2?5.7f:e.kind>=4?3.2f:1.55f;
                if(distance>range&&e.clock>.35f)
                {
                    float speed=e.kind==1?1.25f:e.kind==3?2.7f:e.kind>=4?1.4f:1.75f;
                    e.pos=Bound(e.pos+delta.normalized*speed*dt);
                }
                if(e.clock<=0&&distance<range+.4f)
                {EnemyAttack(e);e.clock=e.kind>=4?2.8f:e.kind==2?2.4f:1.9f;}
                else if(e.clock<=.35f&&distance>range+.4f)e.clock=.6f;
            }
            // Separate bodies without trapping the player against decorative scenery.
            for(int i=0;i<s.enemies.Count;i++)for(int j=i+1;j<s.enemies.Count;j++)
            {
                var a=s.enemies[i];var b=s.enemies[j];if(a.hp<=0||b.hp<=0)continue;
                Vector2 d=a.pos-b.pos;float m=d.magnitude;
                if(m<.8f){Vector2 n=m<.01f?Vector2.right:d/m;a.pos=Bound(a.pos+n*dt);b.pos=Bound(b.pos-n*dt);}
            }
            for(int i=s.dangers.Count-1;i>=0;i--)
            {
                var h=s.dangers[i];h.age+=dt;
                if(h.age>=h.wait&&!h.hit)
                {
                    h.hit=true;
                    if(InDanger(s.pos,h)&&s.jump<.6f)Hurt(h.damage);
                    Emit("impact",h.pos,h.radius);
                }
                if(h.age>h.wait+.35f)s.dangers.RemoveAt(i);
            }
            if(s.hp<=0){s.hp=0;s.phase=Phase.Lost;Settle(false);Emit("lost",s.pos);return;}
            if(s.enemies.TrueForAll(e=>e.hp<=0))
            {
                if(s.wave<s.waves){s.wave++;SpawnWave();Emit("wave",s.pos,s.wave);return;}
                s.dangers.Clear();s.bolts.Clear();s.earned+=BossRoom?12:5+(s.elite?4:0);
                s.hp=Mathf.Min(s.maxHp,s.hp+Rel(1)*8);
                if(s.room==11){s.phase=Phase.Won;Settle(true);Emit("won",s.pos);}
                else
                {
                    s.phase=Phase.Reward;RollOffers();Emit("clear",s.pos);
                    if(BossRoom){s.hp=Mathf.Min(s.maxHp,s.hp+30);s.potions=Math.Min(8,s.potions+1);}
                }
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
        void Attack()
        {
            Aim();s.combo=s.combo%3+1;s.comboClock=1.3f;
            float cooldown=s.weapon==0?.32f:s.weapon==1?.65f:.43f;
            s.attackCd=cooldown;s.swing=cooldown*.85f;
            float damage=(s.weapon==0?15:s.weapon==1?29:17)*(s.combo==3?1.5f:1)+(s.combo==3?Rel(8)*12:0);
            if(s.weapon==2)s.bolts.Add(new Bolt{pos=s.pos+s.direction*.6f,velocity=s.direction*12,damage=damage});
            else
            {
                float range=s.weapon==1?2.6f:2.15f;
                foreach(var e in s.enemies)
                {
                    Vector2 delta=e.pos-s.pos;
                    if(e.hp>0&&delta.magnitude<range+(e.kind>=4?.45f:0)&&Vector2.Dot(delta.normalized,s.direction)>-.3f)
                    {Hit(e,damage,false);e.pos=Bound(e.pos+delta.normalized*(s.weapon==1?.55f:.15f));}
                }
            }
            Emit("attack",s.pos,s.combo);
        }
        void Skill()
        {
            Aim();s.skillFlash=.5f;s.swing=.5f;
            if(s.weapon==2)
            {
                for(int i=-2;i<=2;i++)
                {
                    float a=i*.2f;Vector2 d=new Vector2(s.direction.x*Mathf.Cos(a)-s.direction.y*Mathf.Sin(a),s.direction.x*Mathf.Sin(a)+s.direction.y*Mathf.Cos(a));
                    s.bolts.Add(new Bolt{pos=s.pos+d*.5f,velocity=d*10,damage=36,empowered=true});
                }
            }
            else foreach(var e in s.enemies)
            {
                Vector2 delta=e.pos-s.pos;
                float range=s.weapon==1?4:5.3f;
                if(e.hp>0&&delta.magnitude<range&&(s.weapon==1||Vector2.Dot(delta.normalized,s.direction)>0))
                {Hit(e,s.weapon==1?54:46,true);e.stun=1;e.pos=Bound(e.pos+delta.normalized*.7f);}
            }
            Emit("skill",s.pos);
        }
        void Hit(Enemy e,float baseDamage,bool skill)
        {
            float damage=baseDamage*DamageMultiplier*(skill?1+.3f*Rel(10):1);
            if(e.shadow)damage*=s.bright?1.4f+.35f*Rel(3):.55f;
            if(e.kind==1&&!s.bright)damage*=.8f;
            e.hp=Mathf.Max(0,e.hp-damage);e.flash=.17f;
            if(e.kind<4)e.stun=Mathf.Max(e.stun,s.weapon==1?.4f:.15f);
            Emit(e.shadow&&s.bright?"reveal":"hit",e.pos,damage);
            if(e.hp<=0){s.kills++;s.oil=Mathf.Min(100,s.oil+4);Emit("kill",e.pos);}
        }
        public void Hurt(float damage)
        {
            if(s.invulnerable>0||s.dodgeTime>0)return;
            s.hp=Mathf.Max(0,s.hp-damage);s.invulnerable=.85f;Emit("hurt",s.pos,damage);
        }
        void EnemyAttack(Enemy e)
        {
            if(e.kind<4)
            {
                Vector2 at=e.kind==2?s.pos:e.pos+(s.pos-e.pos).normalized*.8f;
                s.dangers.Add(new Danger{pos=at,radius=e.kind==3?1.1f:1.35f,wait=e.kind==3?.6f:.85f,damage=10+Area*3+(s.elite?2:0)});
            }
            else
            {
                int type=Rng(3);
                if(type==0)s.dangers.Add(new Danger{pos=e.pos,radius=3.5f,wait=1.1f,damage=20+Area*3});
                if(type==1)s.dangers.Add(new Danger{pos=new Vector2(0,s.pos.y),kind=1,width=21,depth=1.7f,wait=1.15f,damage=22+Area*3});
                if(type==2)
                {
                    s.dangers.Add(new Danger{pos=s.pos,radius=1.8f,wait=1.15f,damage=20+Area*3});
                    if(Area>0)for(int i=0;i<2;i++)s.dangers.Add(new Danger{pos=new Vector2(-4+8*i,2-4*i),radius=2,wait=1.35f,damage=18+Area*3});
                }
                Emit("warning",e.pos,type);
            }
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
                if(r!=null&&(r.version!=1||r.room<0||r.room>11||r.weapon<0||r.weapon>2||r.maxHp<1||r.hp<0||r.hp>r.maxHp||float.IsNaN(r.hp)||float.IsInfinity(r.hp)||r.random==0||r.relics==null||r.relics.Length!=12||r.offers==null||r.offers.Length!=3||r.enemies==null||r.dangers==null||r.bolts==null||!Enum.IsDefined(typeof(Phase),r.phase)))return null;
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
