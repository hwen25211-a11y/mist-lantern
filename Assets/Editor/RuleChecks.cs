using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Mist.Editor
{
    public static class RuleChecks
    {
        static readonly List<string> report=new List<string>();
        static int checks;
        static void Assert(bool condition,string label){checks++;if(!condition)throw new Exception("FAIL "+label);report.Add("PASS "+label);}
        [MenuItem("雾灯遗城/运行规则检查")]
        public static void Run()
        {
            checks=0;report.Clear();
            try
            {
                var p=new Profile();var s=Simulation.NewRun(42,0,p);var sim=new Simulation(s,p);sim.Enter();
                Assert(s.enemies.Count==3&&s.phase==Phase.Playing,"new run enters first battle");
                var duplicate=new Simulation(Simulation.NewRun(42,0,p),p);duplicate.Enter();
                Assert(JsonUtility.ToJson(s)==JsonUtility.ToJson(duplicate.s),"seed reproduces full room");
                Vector2 original=s.pos;sim.Tick(.05f,new InputFrame{move=Vector2.one});
                Assert(Vector2.Distance(s.pos,original)<=.226f,"diagonal movement normalized");
                s.pos=new Vector2(9.49f,3.39f);sim.Tick(.05f,new InputFrame{move=Vector2.one});Assert(s.pos.x<=9.5f&&s.pos.y<=3.4f,"arena boundaries");
                float hp=s.hp;s.invulnerable=1;sim.Hurt(20);Assert(s.hp==hp,"invulnerable prevents damage");s.invulnerable=0;sim.Hurt(20);Assert(s.hp==hp-20,"damage changes actual health");
                sim.Tick(.01f,new InputFrame{potion=true});Assert(s.potions==2&&s.hp==s.maxHp,"potion heals and consumes once");
                sim.Tick(.01f,new InputFrame{potion=true});Assert(s.potions==2,"full health does not waste potion");
                s.oil=0;s.bright=true;sim.Tick(.01f,new InputFrame());Assert(!s.bright,"empty oil switches to dark");
                sim.Tick(.05f,new InputFrame{toggle=true});Assert(!s.bright,"insufficient oil cannot relight");
                s.oil=40;sim.Tick(.01f,new InputFrame{toggle=true});Assert(s.bright,"relight when oil available");
                var h=new Danger{pos=new Vector2(1,2),kind=1,width=4,depth=1};
                Assert(Simulation.InDanger(new Vector2(1,2),h)&&!Simulation.InDanger(new Vector2(1,3),h),"rectangular warning uses depth");
                h.kind=0;h.radius=1;Assert(Simulation.InDanger(new Vector2(1.9f,2),h)&&!Simulation.InDanger(new Vector2(1.8f,2.8f),h),"circle damage matches circle");
                s.phase=Phase.Reward;sim.RollOffers();Assert(s.offers.Distinct().Count()==3,"reward choices unique");
                s.offers=new[]{5,9,0};float before=s.hp;Assert(sim.ChooseRelic(0)&&s.maxHp==140&&s.hp==before+20,"health relic applies immediately");
                Assert(!sim.ChooseRelic(0)&&s.relics[5]==1,"cannot double claim");
                Assert(sim.Travel(false)&&s.phase==Phase.Rest&&s.room==1,"first battle leads to camp");
                Assert(sim.Rest(2)&&s.weapon==1&&s.phase==Phase.Route,"camp weapon swap");
                Assert(!sim.Rest(0),"camp choice is exclusive");
                Assert(sim.Travel(true)&&s.elite,"elite route");
                var saved=new SaveData{profile=p,run=s};
                string json=JsonUtility.ToJson(saved);var loaded=Saves.Decode(json);
                Assert(loaded!=null&&JsonUtility.ToJson(loaded)==json,"complete run save roundtrip");
                Assert(Saves.Decode("{}")==null&&Saves.Decode("{oops")==null,"broken save rejected");
                loaded.run.room=99;Assert(Saves.Decode(JsonUtility.ToJson(loaded))==null,"out of range save rejected");
                s.settled=false;s.earned=9;sim.Settle(false);int bank=p.embers;sim.Settle(false);Assert(p.embers==bank&&p.runs==1,"settlement idempotent");
                for(int weapon=0;weapon<3;weapon++)foreach(int seed in new[]{7,42,2026})FullRun(seed,weapon);
                var death=new Simulation(Simulation.NewRun(12,0,new Profile()),new Profile());death.Enter();
                for(int i=0;i<30000&&death.s.phase==Phase.Playing;i++)death.Tick(.05f,new InputFrame());
                Assert(death.s.phase==Phase.Lost&&death.s.hp==0,"idle player dies without softlock");
                Assert(death.s.settled,"loss settles currency");
                report.Add("RULES_OK assertions="+checks);Directory.CreateDirectory("docs/qa");File.WriteAllLines("docs/qa/rules.txt",report);Debug.Log(report.Last());
            }
            catch(Exception e){report.Add(e.ToString());Directory.CreateDirectory("docs/qa");File.WriteAllLines("docs/qa/rules.txt",report);Debug.LogException(e);throw;}
        }
        public static InputFrame Pilot(Simulation sim)
        {
            var s=sim.s;var enemies=s.enemies.Where(e=>e.hp>0).ToList();if(enemies.Count==0)return new InputFrame();
            var enemy=enemies.OrderBy(e=>Vector2.Distance(s.pos,e.pos)).First();Vector2 delta=enemy.pos-s.pos;
            float distance=delta.magnitude,desired=s.weapon==2?4.5f:1.3f;
            Vector2 move=distance>desired?delta.normalized:distance<desired-.5f?-delta.normalized:Vector2.zero;
            var danger=s.dangers.Where(h=>!h.hit&&Simulation.InDanger(s.pos,h)).OrderBy(h=>h.wait-h.age).FirstOrDefault();
            bool dodge=false,jump=false;
            if(danger!=null)
            {
                float best=-999;Vector2 direction=Vector2.up;
                for(int i=0;i<16;i++)
                {
                    float a=i*Mathf.PI/8;var d=new Vector2(Mathf.Cos(a),Mathf.Sin(a));Vector2 future=Simulation.Bound(s.pos+d*2.7f);
                    float score=Vector2.Distance(future,s.pos)*2-Vector2.Distance(future,enemy.pos)*.1f;
                    foreach(var h in s.dangers.Where(h=>!h.hit))if(Simulation.InDanger(future,h))score-=20;
                    if(score>best){best=score;direction=d;}
                }
                move=direction;dodge=danger.wait-danger.age<.3f;jump=danger.wait-danger.age<.27f;
            }
            return new InputFrame{move=move,attack=true,skill=distance<(s.weapon==2?7:3),dodge=dodge,jump=jump,potion=s.hp<s.maxHp-44,toggle=(s.bright&&s.oil<22)||(!s.bright&&s.oil>85)};
        }
        static void FullRun(int seed,int weapon)
        {
            var p=new Profile();var sim=new Simulation(Simulation.NewRun(seed,weapon,p),p);sim.Enter();int tick=0,rooms=1;
            while(tick++<90000&&sim.s.phase!=Phase.Won&&sim.s.phase!=Phase.Lost)
            {
                if(sim.s.phase==Phase.Playing)sim.Tick(.05f,Pilot(sim));
                else if(sim.s.phase==Phase.Reward)sim.ChooseRelic(0);
                else if(sim.s.phase==Phase.Rest)sim.Rest(0);
                else if(sim.s.phase==Phase.Route){sim.Travel(seed==2026);rooms++;}
                if(tick%1000==0)
                {
                    var d=Saves.Decode(JsonUtility.ToJson(new SaveData{profile=p,run=sim.s}));
                    if(d==null)throw new Exception("mid-combat reload failed");sim=new Simulation(d.run,d.profile);p=d.profile;
                }
            }
            Assert(sim.s.phase==Phase.Won,"complete seed="+seed+" weapon="+weapon+" phase="+sim.s.phase+" room="+sim.s.room+" hp="+sim.s.hp+" time="+sim.s.elapsed);
            Assert(rooms==12&&p.wins==1&&sim.s.settled,"all rooms and one final settlement "+seed+"/"+weapon);
        }
    }
}
