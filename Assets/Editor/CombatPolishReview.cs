using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
namespace Mist.Editor
{
    public static class CombatPolishReview
    {
        const string Dir="docs/screenshots/combat-polish/";
        static int assertions;
        static readonly List<string> log=new List<string>();
        static void Check(bool ok,string label){if(!ok)throw new Exception("Polish regression: "+label);log.Add("PASS "+label);assertions++;}
        static Simulation Arena(int w=0){var p=new Profile();var sim=new Simulation(Simulation.NewRun(12022,w,p),p);sim.Enter();sim.s.pos=Vector2.zero;sim.s.invulnerable=0;sim.s.enemies=new List<Enemy>{new Enemy{id=71,kind=0,pos=new Vector2(1.3f,0),hp=5000,maxHp=5000,clock=100}};return sim;}
        public static void PrepareRelease(){Validate();ModelExport.Run();BuildGame.Gallery();ReleasePreparation.CleanUnusedGeneratedAssets();Debug.Log("COMBAT_RELEASE_PREPARED");}
        public static void Validate(){RuleChecks.Run();CombatChecks.Run();Run();WardenReview.Check();ModelChecks.Run();}
        public static void Regenerate(){BuildGame.GenerateModels();Run();}
        public static void Run(){log.Clear();assertions=0;Rules();Poses();Effects();File.WriteAllLines("docs/qa/combat-polish/checks.txt",log.Concat(new[]{"POLISH_OK assertions="+assertions}));Debug.Log("POLISH_OK assertions="+assertions);}
        static void Rules(){
            var sim=Arena();sim.s.enemies[0].pos=new Vector2(8,0);for(int i=0;i<22;i++)sim.Tick(1f/60,new InputFrame{attack=i==0});Check(sim.s.hitStop==0&&sim.s.enemies[0].hp==5000,"empty swing does not invent damage or contact pause");
            sim=Arena();Signal contact=default(Signal);Vector2 initial=sim.s.enemies[0].pos;for(int i=0;i<20;i++){sim.Tick(1f/60,new InputFrame{attack=i==0});foreach(var e in sim.signals)if(e.kind=="hit"||e.kind=="reveal")contact=e;if(contact.targetId!=0)break;}
            Check(contact.targetId==71&&contact.amount>0&&contact.direction.x>.99f,"contact carries actual target, direction and damage");Check(Vector2.Distance(initial,sim.s.enemies[0].pos)<.03f&&sim.s.enemies[0].knockVelocity.x>0,"hit imparts velocity without teleporting target");
            sim.s.hitStop=0;float speed=sim.s.enemies[0].knockVelocity.magnitude;sim.Tick(1f/60,new InputFrame());Check(sim.s.enemies[0].pos.x>initial.x&&sim.s.enemies[0].knockVelocity.magnitude<speed,"knockback moves continuously and damps");
            var restored=Saves.Decode(JsonUtility.ToJson(new SaveData{profile=sim.profile,run=sim.s}));Check(restored!=null&&restored.run.enemies[0].knockVelocity==sim.s.enemies[0].knockVelocity,"save preserves ongoing physical reaction");
            sim=Arena();sim.s.enemies[0].pos=new Vector2(8,0);sim.Tick(.01f,new InputFrame{attack=true});while(sim.s.actionAge<sim.s.actionDuration-.07f)sim.Tick(.01f,new InputFrame());sim.Tick(.01f,new InputFrame{attack=true});for(int i=0;i<9;i++)sim.Tick(.01f,new InputFrame());Check(sim.s.combo==2&&sim.s.actionDuration>0,"late light-attack tap buffers into next combo without holding");
            var charged=CombatMotion.Sample(1,1,false,-1,.7f);var released=CombatMotion.Sample(1,1,false,0,.7f);
            Check(Vector3.Distance(charged.arm,released.arm)<.001f&&Vector3.Distance(charged.weapon,released.weapon)<.001f&&Vector3.Distance(charged.offset,released.offset)<.001f,"fully charged hammer keeps its anticipation pose when strike begins");
            foreach(int w in new[]{0,1,2})foreach(string sound in new[]{"windup","attack","hit","kill","parry","heal"}){var samples=Sound.CraftSound(sound,w);Check(samples.All(x=>!float.IsNaN(x)&&!float.IsInfinity(x)&&Mathf.Abs(x)<=.92f)&&samples.Any(x=>Mathf.Abs(x)>.05f)&&Mathf.Abs(samples.Average())<.02f,"sound waveform finite, unclipped and centered "+w+" "+sound);}
        }
        static void Poses(){
            Directory.CreateDirectory(Dir);Directory.CreateDirectory("docs/qa/combat-polish");EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            RenderSettings.fog=false;RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Trilight;RenderSettings.ambientSkyColor=Models.Hex("86969A");RenderSettings.ambientEquatorColor=Models.Hex("546468");RenderSettings.ambientGroundColor=Models.Hex("32373C");
            var key=new GameObject("Pose key").AddComponent<Light>();key.type=LightType.Directional;key.intensity=.85f;key.color=Models.Hex("FFE6CF");key.transform.rotation=Quaternion.Euler(35,-32,0);
            var cam=new GameObject("Pose camera").AddComponent<Camera>();cam.orthographic=true;cam.orthographicSize=2.1f;cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=Models.Hex("29383E");cam.transform.position=new Vector3(0,3,-9);cam.transform.LookAt(new Vector3(0,1.25f,0));
            for(int w=1;w<3;w++){
                var roots=new List<GameObject>();for(int facing=-1;facing<=1;facing+=2){
                    var root=UnityEngine.Object.Instantiate(Resources.Load<GameObject>("Models/Hunter"+w));roots.Add(root);var actor=new CombatActor(root,null,-1);actor.Apply(CombatMotion.Sample(w,1,false,-1,0),new Vector2(facing*2.0f,0),0,facing,0,0,false,0,0);
                    var tip=actor.weapon.TransformPoint(new Vector3(0,w==1?1.17f:1.62f,0));Check(Mathf.Abs(tip.x-root.transform.position.x)>.9f,"idle weapon head clears torso in both facings "+w+" "+facing);
                }Save(cam,Dir+"idle-"+w+".png",1600,600);foreach(var root in roots)UnityEngine.Object.DestroyImmediate(root);
            }
            for(int w=0;w<3;w++)for(int combo=1;combo<=(w==0?3:1);combo++){
                var actors=new List<GameObject>();float worst=1;
                for(int i=0;i<3;i++){
                    var root=UnityEngine.Object.Instantiate(Resources.Load<GameObject>("Models/Hunter"+w));actors.Add(root);var actor=new CombatActor(root,null,-1);
                    actor.Apply(CombatMotion.Sample(w,combo,false,new[]{.32f,.45f,.65f}[i],0),new Vector2((i-1)*3.5f,0),0,1,0,0,false,0,0);
                    foreach(var skin in root.GetComponentsInChildren<SkinnedMeshRenderer>()){
                        var baked=new Mesh();skin.BakeMesh(baked);var a=skin.sharedMesh.vertices;var b=baked.vertices;var indices=skin.sharedMesh.triangles;
                        Check(b.All(v=>!float.IsNaN(v.sqrMagnitude)&&!float.IsInfinity(v.sqrMagnitude)),"finite posed skin "+w+"/"+combo+"/"+i+" "+skin.name);
                        for(int j=0;j<indices.Length;j+=3)for(int edge=0;edge<3;edge++){int u=indices[j+edge],v=indices[j+(edge+1)%3];float rest=(a[u]-a[v]).magnitude;if(rest>.0005f)worst=Mathf.Max(worst,(b[u]-b[v]).magnitude/rest);}UnityEngine.Object.DestroyImmediate(baked);
                    }
                    if(i==1){Check(actor.weapon.GetComponentsInChildren<Renderer>().Any(r=>r.enabled),"weapon pivot owns rendered mesh "+w);var tip=actor.weapon.TransformPoint(new Vector3(w==0?.22f:0,w==0?1.02f:w==1?1.17f:1.62f,0));log.Add("POSE weapon="+w+" combo="+combo+" contact_tip="+(tip-root.transform.position).ToString("F3"));}
                }
                Check(worst<6,"actual combat pose edge stretch < 6, weapon="+w+" combo="+combo+" ratio="+worst.ToString("F3"));Save(cam,Dir+"poses-"+w+"-"+combo+".png",1600,600);foreach(var root in actors)UnityEngine.Object.DestroyImmediate(root);
            }
        }
        static void Effects(){
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);var world=new GameObject("Polish world").AddComponent<World>();world.Setup();var sim=Arena();world.Render(sim.s,1f/60,true);
            for(int i=0;i<90;i++){sim.Tick(1f/60,new InputFrame{attack=true});foreach(var e in sim.signals)world.Signal(e);world.Render(sim.s,1f/60,true);if(i==6||i==21||i==50)Save(world.cam,Dir+"contact-"+i+".png",1600,900);}
            Check(world.ActiveVisualCount<=192,"combat effect pool remains bounded");
            var target=sim.s.enemies[0];world.Signal(new Signal("kill",target.pos){targetId=target.id,actorKind=target.kind,direction=Vector2.right});sim.s.enemies.Clear();sim.s.phase=Phase.Reward;world.Render(sim.s,1f/60,true);
            Check(world.DefeatedVisualCount==1,"killed actor remains as a separate visual after enemy list changes");Save(world.cam,Dir+"defeat-start.png",1600,900);
            for(int i=0;i<55;i++)world.Render(sim.s,1f/60,true);Check(world.DefeatedVisualCount==0&&world.ActiveVisualCount==0,"death and impact effects finish while reward menu freezes gameplay");
            sim.s.phase=Phase.Playing;sim.s.dangers.Add(new Danger{pos=new Vector2(2,0),radius=1.6f,wait=1.2f,age=.6f,damage=10});world.Render(sim.s,1f/60,true);Save(world.cam,Dir+"telegraph.png",1600,900);
            world.ResetModels();for(int area=0;area<3;area++){sim.s.room=area*4;sim.s.dangers.Clear();world.Render(sim.s,1f/60,true);Save(world.cam,Dir+"area-"+area+".png",1600,900);}world.ResetModels();Check(world.ActiveVisualCount==0&&world.DefeatedVisualCount==0,"reset clears retired actors and active effects");
        }
        static void Save(Camera cam,string path,int width,int height){var rt=new RenderTexture(width,height,24){antiAliasing=4};cam.targetTexture=rt;using(var pose=new ModelGeometry.PoseBake())cam.Render();RenderTexture.active=rt;var t=new Texture2D(width,height,TextureFormat.RGB24,false);t.ReadPixels(new Rect(0,0,width,height),0,0);t.Apply();File.WriteAllBytes(path,t.EncodeToPNG());cam.targetTexture=null;RenderTexture.active=null;UnityEngine.Object.DestroyImmediate(t);rt.Release();UnityEngine.Object.DestroyImmediate(rt);}
    }
}
