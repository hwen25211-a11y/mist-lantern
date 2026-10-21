using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using UnityEngine;
namespace Mist.Editor
{
    public static class CombatChecks
    {
        static List<string> log=new List<string>();
        static void Check(bool ok,string label){if(!ok)throw new Exception("Combat regression: "+label);log.Add("PASS "+label);}
        static Simulation Arena(int weapon=0)
        {
            var p=new Profile();var sim=new Simulation(Simulation.NewRun(77,weapon,p),p);sim.Enter();sim.s.pos=Vector2.zero;sim.s.invulnerable=0;
            sim.s.enemies=new List<Enemy>{new Enemy{id=1,kind=0,pos=new Vector2(1.5f,0),hp=5000,maxHp=5000,clock=100}};return sim;
        }
        static void Advance(Simulation s,float seconds,InputFrame input=default(InputFrame),float dt=.02f){for(float t=0;t<seconds-.0001f;t+=dt)s.Tick(dt,input);}
        public static void Run()
        {
            log.Clear();
            var sim=Arena();var s=sim.s;float hp=s.enemies[0].hp;
            sim.Tick(.02f,new InputFrame{attack=true});Check(s.enemies[0].hp==hp,"press begins windup without instant damage");
            Advance(sim,.08f);Check(s.enemies[0].hp<hp,"damage begins at active frame");float after=s.enemies[0].hp;Advance(sim,.2f);Check(s.enemies[0].hp==after,"one strike does not repeatedly hit one target");
            sim=Arena();s=sim.s;sim.Tick(.02f,new InputFrame{attack=true});sim.Tick(.02f,new InputFrame{skill=true});Advance(sim,.55f);
            Check(s.skillCd>0&&s.oil<70,"one skill tap during normal attack buffers into recovery");float cooldown=s.skillCd;Advance(sim,1);
            Check(s.skillCd<cooldown,"buffered skill executes only once");
            sim=Arena();s=sim.s;s.hitStop=.1f;sim.Tick(.02f,new InputFrame{skill=true});Advance(sim,.7f);Check(s.skillCd>0,"hit stop preserves a tapped skill input");
            sim=Arena();s=sim.s;s.enemies[0].kind=5;Vector2 bossPosition=s.enemies[0].pos;sim.Tick(.02f,new InputFrame{attack=true});Advance(sim,.25f);Check(s.enemies[0].pos==bossPosition,"light attacks do not slide heavy bosses out of reach");
            sim=Arena();s=sim.s;sim.Tick(.02f,new InputFrame{attack=true});s.enemies[0].pos=new Vector2(1.5f,3);Advance(sim,.18f);Check(s.enemies[0].hp==5000,"target leaving attack reach before active frame avoids hit");
            sim=Arena();s=sim.s;sim.Tick(.02f,new InputFrame{attack=true});Advance(sim,.1f);var json=JsonUtility.ToJson(new SaveData{profile=sim.profile,run=s});var copy=Saves.Decode(json);Check(copy!=null,"mid-action save is readable");after=s.enemies[0].hp;sim=new Simulation(copy.run,copy.profile);Advance(sim,.4f);Check(sim.s.enemies[0].hp==after,"reloading resolved action does not duplicate damage");
            sim=Arena();s=sim.s;sim.Tick(.02f,new InputFrame{guard=true});sim.Hurt(40);Check(s.hp==120&&s.perfectGuards==1&&s.resource==40,"precise guard prevents damage and grants resource");
            s.invulnerable=0;s.hitStop=0;Advance(sim,.3f,new InputFrame{guard=true});sim.Hurt(40);Check(s.hp<120&&s.hp>80,"held guard is reduced damage, not endless parry");
            sim=Arena();s=sim.s;sim.Tick(.02f,new InputFrame{place=true});Check(s.lampPlaced&&s.lampPos==Vector2.zero,"lamp placement keeps world position");s.pos=new Vector2(8,0);Check(sim.Lit(Vector2.zero)&&!sim.Lit(s.pos),"light zone stays at placed lamp when player moves");Advance(sim,.5f);sim.Tick(.02f,new InputFrame{place=true});Check(!s.lampPlaced&&sim.Lit(s.pos),"recall returns light zone to player");
            sim=Arena();s=sim.s;s.lampPlaced=true;s.lampPos=Vector2.zero;s.pos=new Vector2(8,0);s.enemies[0].kind=3;s.enemies[0].pos=new Vector2(1,0);s.enemies[0].clock=0;sim.Tick(.02f,new InputFrame());Check(s.dangers.Count==1&&s.dangers[0].lampTarget,"hound targets placed lamp");float oil=s.oil;Advance(sim,.8f);Check(s.oil<oil-10,"enemy lamp attack drains oil");
            sim=Arena(1);s=sim.s;Advance(sim,.1f,new InputFrame{attack=true});sim.Tick(.02f,new InputFrame());Advance(sim,.25f);float light=5000-s.enemies[0].hp;
            sim=Arena(1);Advance(sim,.72f,new InputFrame{attack=true});Advance(sim,.25f);Check(5000-sim.s.enemies[0].hp>light*1.4f,"charged hammer has stronger impact than tap");
            sim=Arena(1);s=sim.s;s.enemies[0].action=1;s.dangers.Add(new Danger{owner=1,pos=s.pos,radius=2,wait=3,damage=20});Advance(sim,.74f,new InputFrame{attack=true});Advance(sim,.25f);Check(s.dangers.Count==0,"hammer stagger cancels pending enemy hit area");
            sim=Arena(2);s=sim.s;s.resource=100;sim.Tick(.02f,new InputFrame{attack=true});Advance(sim,.12f);Check(s.bolts.Count==3&&s.resource==0,"full star energy produces three projectiles and spends energy");
            sim=Arena(2);s=sim.s;s.enemies.Add(new Enemy{id=2,kind=0,pos=new Vector2(3,0),hp=5000,maxHp=5000,clock=100});
            sim.Tick(.02f,new InputFrame{skill=true});Advance(sim,.6f);Check(s.enemies[0].hp<5000&&s.enemies[1].hp<5000,"star skill penetrates two aligned enemies");
            sim=Arena(2);s=sim.s;s.enemies[0].pos=Vector2.zero;sim.Tick(.02f,new InputFrame{attack=true});Advance(sim,.16f);Check(s.enemies[0].hp<5000,"staff can hit overlapping point-blank enemy");
            sim=Arena();s=sim.s;s.dangers.Add(new Danger{owner=1,pos=s.pos,radius=1,wait=.1f,damage=30});s.jump=1;s.jumpVelocity=2;Advance(sim,.14f);Check(s.hp==120,"jump clears timed ground strike");
            sim=Arena();s=sim.s;s.dangers.Add(new Danger{owner=1,pos=s.pos,radius=2,wait=.1f,damage=30});sim.Tick(.02f,new InputFrame{dodge=true});Advance(sim,.15f);Check(s.hp==120,"dodge passes through telegraph without damage");
            for(int kind=4;kind<=6;kind++)
            {
                sim=Arena();s=sim.s;s.enemies[0].kind=kind;s.enemies[0].clock=0;s.enemies[0].hp=100;s.enemies[0].maxHp=500;
                sim.Tick(.02f,new InputFrame());Check(s.enemies[0].action>0&&s.dangers.All(x=>x.wait>=1),"boss "+kind+" has visible windup");
                Check(kind==4?s.dangers.Count==1:kind==5?s.dangers.Count==3:s.dangers.Count==2,"boss "+kind+" has its own opening pattern");
            }
            foreach(float dt in new[]{1f/30,1f/60})
            {sim=Arena();s=sim.s;s.dangers.Add(new Danger{owner=1,pos=s.pos,radius=1,wait=.5f,damage=20});Advance(sim,.7f,default(InputFrame),dt);Check(s.hp==100,"hazard applies exactly once at "+Mathf.RoundToInt(1/dt)+" FPS");}
            sim=Arena();s=sim.s;s.bright=true;s.oil=.01f;Advance(sim,.1f);Check(!s.bright,"exhausted lamp stops lighting area");
            sim=Arena();s=sim.s;s.lampPlaced=true;s.lampPos=new Vector2(3,1);s.phase=Phase.Route;s.room=2;sim.Travel(false);Check(!s.lampPlaced&&s.actionDuration==0,"travel resets placed lamp and action state");
            Directory.CreateDirectory("docs/qa");log.Add("COMBAT_OK assertions="+log.Count);File.WriteAllLines("docs/qa/combat-v2.txt",log);Debug.Log(log[log.Count-1]);
        }
    }
}
