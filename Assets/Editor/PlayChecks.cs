using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;
namespace Mist.Editor
{
    [InitializeOnLoad] public static class PlayChecks
    {
        static PlayChecks(){EditorApplication.update+=Tick;}
        public static void Run()
        {
            BuildGame.Prepare();SessionState.SetBool("Mist.PlayChecks",true);
            SessionState.SetFloat("Mist.CheckStart",(float)EditorApplication.timeSinceStartup);
            EditorSceneManager.OpenScene(BuildGame.Scene);EditorApplication.isPlaying=true;
        }
        static void Tick()
        {
            if(!SessionState.GetBool("Mist.PlayChecks",false))return;
            if(EditorApplication.timeSinceStartup-SessionState.GetFloat("Mist.CheckStart",0)>900){Debug.LogError("QA_TIMEOUT");SessionState.SetBool("Mist.PlayChecks",false);EditorApplication.Exit(1);}
            if(EditorApplication.isPlaying&&Game.Instance&&!UnityEngine.Object.FindObjectOfType<PlayRunner>())new GameObject("QA runner").AddComponent<PlayRunner>();
        }
    }
    public sealed class PlayRunner:MonoBehaviour
    {
        Game game;readonly List<string> report=new List<string>();readonly List<string> errors=new List<string>();int overflow;
        string previous,backup;int oldMuted;
        void Check(bool ok,string label){if(!ok)throw new Exception(label);report.Add("PASS "+label);}
        void Error(string text,string trace,LogType type){if(type==LogType.Exception||type==LogType.Error||type==LogType.Assert)errors.Add(text+"\n"+trace);}
        void Click(string name)
        {
            Canvas.ForceUpdateCanvases();
            var b=UnityEngine.Object.FindObjectsOfType<Button>().FirstOrDefault(x=>x.gameObject.activeInHierarchy&&x.name==name);
            Check(b&&b.interactable,"button "+name);
            var r=(RectTransform)b.transform;var point=RectTransformUtility.WorldToScreenPoint(null,r.TransformPoint(r.rect.center));
            var e=new PointerEventData(EventSystem.current){position=point,button=PointerEventData.InputButton.Left};
            var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(e,hits);
            Check(hits.Count>0&&hits[0].gameObject==b.gameObject,"actual UI raycast "+name);
            ExecuteEvents.Execute(b.gameObject,e,ExecuteEvents.pointerClickHandler);
        }
        void Audit(string name)
        {
            Canvas.ForceUpdateCanvases();int n=0;
            foreach(var text in UnityEngine.Object.FindObjectsOfType<TextMeshProUGUI>())
            {
                if(!text.gameObject.activeInHierarchy)continue;text.ForceMeshUpdate();
                if(text.isTextOverflowing){n++;report.Add("OVERFLOW "+name+" "+text.name+" "+text.text.Replace('\n',' '));}
            }
            overflow+=n;report.Add("UI "+name+" overflow="+n);
            Capture(name);
            File.WriteAllLines("docs/qa/play-progress.txt",report);
        }
        void Capture(string name)
        {
            var camera=game.world.cam;var canvas=game.canvas.GetComponentInParent<Canvas>();
            var rt=new RenderTexture(1600,900,24);rt.Create();
            var oldTarget=camera.targetTexture;var oldMode=canvas.renderMode;var oldCamera=canvas.worldCamera;float oldPlane=canvas.planeDistance;
            camera.targetTexture=rt;canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;
            Canvas.ForceUpdateCanvases();camera.Render();var previous=RenderTexture.active;RenderTexture.active=rt;
            var image=new Texture2D(1600,900,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1600,900),0,0);image.Apply();
            File.WriteAllBytes("docs/screenshots/"+name+".png",image.EncodeToPNG());
            RenderTexture.active=previous;camera.targetTexture=oldTarget;canvas.renderMode=oldMode;canvas.worldCamera=oldCamera;canvas.planeDistance=oldPlane;
            Destroy(image);rt.Release();Destroy(rt);Canvas.ForceUpdateCanvases();
        }
        IEnumerator Start()
        {
            game=Game.Instance;Saves.IsTest=true;AudioListener.volume=0;
            previous=PlayerPrefs.HasKey(Saves.Key)?PlayerPrefs.GetString(Saves.Key):null;backup=PlayerPrefs.HasKey(Saves.Key+".backup")?PlayerPrefs.GetString(Saves.Key+".backup"):null;
            oldMuted=PlayerPrefs.GetInt("MistLantern.Muted",1);Application.logMessageReceived+=Error;
            game.data=new SaveData();game.Title();Directory.CreateDirectory("docs/screenshots");
            var probe=Probe();
            while(true)
            {
                object value=null;bool more=false;
                try{more=probe.MoveNext();if(more)value=probe.Current;}
                catch(Exception e){report.Add("FAILED "+e);Finish(1);yield break;}
                if(!more)break;yield return value;
            }
            Check(AudioListener.volume==0,"all runtime tests silent");
            report.AddRange(errors);report.Add("PLAY_OK errors="+errors.Count+" overflow="+overflow);
            Finish(errors.Count==0&&overflow==0?0:1);
        }
        void Finish(int code)
        {
            game.TestDriving=false;game.SuppressQuitSave=true;game.enabled=false;Application.logMessageReceived-=Error;
            // Preserve the user's save and the test save that existed before the probe.
            if(previous==null)PlayerPrefs.DeleteKey(Saves.Key);else PlayerPrefs.SetString(Saves.Key,previous);
            if(backup==null)PlayerPrefs.DeleteKey(Saves.Key+".backup");else PlayerPrefs.SetString(Saves.Key+".backup",backup);
            PlayerPrefs.SetInt("MistLantern.Muted",oldMuted);PlayerPrefs.Save();
            File.WriteAllLines("docs/qa/play-results.txt",report);SessionState.SetBool("Mist.PlayChecks",false);
            Debug.Log("MIST_PLAY_END code="+code);EditorApplication.Exit(code);
        }
        IEnumerator Probe()
        {
            for(int i=0;i<8;i++)yield return null;
            Check(Saves.IsTest&&AudioListener.volume==0,"test uses separate save and zero audio");
            Audit("01-title");yield return null;
            Click("Guide");yield return null;Audit("02-guide");yield return null;Click("Back");yield return null;
            Click("Workshop");yield return null;Audit("03-workshop");yield return null;Click("Back");yield return null;
            for(int weapon=0;weapon<3;weapon++)
            {
                Click("New");yield return null;
                if(game.Screen=="confirm"){Click("ConfirmNew");yield return null;}
                Click("Weapon"+weapon);yield return null;Audit("04-loadout-"+weapon);yield return null;Click("Begin");yield return null;
                Check(game.sim.s.weapon==weapon,"loadout chooses real weapon "+weapon);
                Audit("05-gameplay-"+weapon);yield return null;
                Click("Pause");yield return null;float elapsed=game.sim.s.elapsed,hp=game.sim.s.hp;
                for(int i=0;i<15;i++)yield return null;
                Check(game.sim.s.elapsed==elapsed&&game.sim.s.hp==hp,"pause freezes combat "+weapon);
                Audit("06-pause-"+weapon);yield return null;
                Click("SaveTitle");yield return null;Click("Continue");yield return null;
                Check(game.sim.s.weapon==weapon&&game.sim.s.hp==hp&&Mathf.Abs(game.sim.s.elapsed-elapsed)<.11f,"UI save and continue preserves state within first update "+weapon);
                game.TestDriving=true;
                int lastRoom=-1,frames=0;var phases=new HashSet<Phase>();
                while(game.sim.s.phase!=Phase.Won&&game.sim.s.phase!=Phase.Lost&&frames++<20000)
                {
                    if(game.Paused){game.Resume();yield return null;}
                    var s=game.sim.s;phases.Add(s.phase);
                    if(s.room!=lastRoom){lastRoom=s.room;Audit("run-"+weapon+"-room-"+(s.room+1));yield return null;}
                    if(s.phase==Phase.Playing)game.TestInput=RuleChecks.Pilot(game.sim);
                    else
                    {
                        game.TestInput=new InputFrame();
                        // The new canvas is registered at the end of its creation frame.
                        yield return null;
                        if(s.phase==Phase.Reward)Click("Pick0");
                        else if(s.phase==Phase.Route)Click("Safe");
                        else if(s.phase==Phase.Rest)Click("Rest0");
                    }
                    yield return null;
                }
                game.TestDriving=false;
                Check(game.sim.s.phase==Phase.Won,"runtime complete all rooms with weapon "+weapon+" room="+game.sim.s.room);
                Check(phases.Contains(Phase.Rest)&&phases.Contains(Phase.Reward)&&phases.Contains(Phase.Route),"runtime visits all flow types "+weapon);
                Audit("07-ending-"+weapon);yield return null;
                Click("ReturnTitle");yield return null;
            }
            Click("Workshop");yield return null;int embers=game.data.profile.embers;Click("Buy0");yield return null;
            Check(game.data.profile.vitality==1&&game.data.profile.embers==embers-15,"permanent upgrade actual purchase");
            Audit("08-upgraded");yield return null;Click("Back");yield return null;Click("New");yield return null;Click("Begin");yield return null;
            Check(game.sim.s.maxHp==132,"permanent growth reaches new run");
            Click("Bag");yield return null;Audit("09-inventory");yield return null;Click("Back");yield return null;
            game.TestDriving=true;game.TestInput=new InputFrame();
            for(int i=0;i<20000&&game.sim.s.phase==Phase.Playing;i++)yield return null;
            game.TestDriving=false;Check(game.sim.s.phase==Phase.Lost,"natural death reaches loss screen");Audit("10-loss");yield return null;
            Click("Again");yield return null;Click("Begin");yield return null;Check(game.sim.s.hp==game.sim.s.maxHp&&game.sim.s.room==0,"restart after death resets run");
            Check(game.sound.GetComponents<AudioSource>().Count(x=>x.loop)==1,"one ambient loop per game");
            Click("Pause");yield return null;Click("Audio");yield return null;Check(AudioListener.volume==0,"test mute cannot be overridden by game button");Click("Audio");yield return null;
        }
    }
}
