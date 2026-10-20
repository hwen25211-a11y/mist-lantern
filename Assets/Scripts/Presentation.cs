using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

namespace Mist
{
    public sealed class World:MonoBehaviour
    {
        public Camera cam;
        Transform stage,actors,fx;
        GameObject hero;
        readonly Dictionary<int,GameObject> enemyModels=new Dictionary<int,GameObject>();
        readonly List<GameObject> dangerModels=new List<GameObject>(),boltModels=new List<GameObject>();
        readonly List<Particle> particles=new List<Particle>();
        int area=-1,weapon=-1;
        Vector2 previous;
        float walking,shake;
        Light heroLight;
        class Particle{public GameObject obj;public Vector3 velocity;public float life,total;}
        public static Vector3 P(Vector2 p,float y=0){return new Vector3(p.x,y,p.y);}
        public void Setup()
        {
            cam=new GameObject("Adventure Camera").AddComponent<Camera>();cam.tag="MainCamera";
            cam.orthographic=true;cam.orthographicSize=7.1f;cam.transform.position=new Vector3(0,11.5f,-22);cam.transform.LookAt(new Vector3(0,.6f,1.3f));
            cam.backgroundColor=Models.Hex("172E3C");cam.clearFlags=CameraClearFlags.SolidColor;cam.nearClipPlane=.1f;cam.farClipPlane=100;
            cam.gameObject.AddComponent<AudioListener>();
            RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Flat;RenderSettings.ambientLight=Models.Hex("4C6574");RenderSettings.ambientIntensity=.85f;
            RenderSettings.fog=true;RenderSettings.fogColor=Models.Hex("203B49");RenderSettings.fogMode=FogMode.Linear;RenderSettings.fogStartDistance=33;RenderSettings.fogEndDistance=65;
            var key=new GameObject("Moonlight").AddComponent<Light>();key.type=LightType.Directional;key.color=Models.Hex("B4D3E3");key.intensity=1.2f;key.transform.rotation=Quaternion.Euler(48,-35,0);key.shadows=LightShadows.Soft;key.shadowStrength=.65f;
            var rim=new GameObject("Amber rim").AddComponent<Light>();rim.type=LightType.Directional;rim.color=Models.Hex("F9BD78");rim.intensity=.6f;rim.transform.rotation=Quaternion.Euler(30,140,0);
            actors=new GameObject("Characters").transform;fx=new GameObject("Effects").transform;
            heroLight=new GameObject("Carried lantern light").AddComponent<Light>();heroLight.type=LightType.Point;heroLight.color=Models.Ember;heroLight.range=6;heroLight.intensity=2;
            SetStage(0);
        }
        void SetStage(int next)
        {
            if(next==area)return;area=next;if(stage){stage.gameObject.SetActive(false);Destroy(stage.gameObject);}
            var prefab=Resources.Load<GameObject>("Stages/Area"+next);
            stage=(prefab?Instantiate(prefab):Models.Stage(next)).transform;
            // Combine the static environment into material groups, keeping editable source prefabs.
            StaticBatchingUtility.Combine(stage.gameObject);
        }
        public void ResetModels()
        {
            foreach(var g in enemyModels.Values)if(g){g.SetActive(false);Destroy(g);}enemyModels.Clear();
            if(hero){hero.SetActive(false);Destroy(hero);}weapon=-1;
            foreach(var p in particles)if(p.obj){p.obj.SetActive(false);Destroy(p.obj);}particles.Clear();
            foreach(var n in fx.GetComponentsInChildren<FloatingNumber>()){n.gameObject.SetActive(false);Destroy(n.gameObject);}
            foreach(var o in dangerModels)o.SetActive(false);foreach(var o in boltModels)o.SetActive(false);
        }
        GameObject Make(int kind,int w=0)
        {
            var prefab=Resources.Load<GameObject>("Models/"+(kind<0?"Hunter"+w:"Enemy"+kind));
            var o=prefab?Instantiate(prefab):Models.Character(kind,w);o.transform.SetParent(actors,false);return o;
        }
        public void Render(Run s,float dt,bool live)
        {
            if(s==null){heroLight.intensity=0;return;}
            SetStage(s.room/4);
            if(weapon!=s.weapon||!hero){if(hero)Destroy(hero);hero=Make(-1,s.weapon);weapon=s.weapon;}
            float moved=Vector2.Distance(previous,s.pos);previous=s.pos;
            walking=Mathf.Lerp(walking,live&&moved>.002f?1:0,dt*14);
            Animate(hero,s.pos,s.jump,s.direction.x,walking,s.swing,s.weapon,s.dodgeTime>0,s.invulnerable>0,dt);
            heroLight.transform.position=P(s.pos,1.4f)+new Vector3(-.5f,0,-.2f);heroLight.intensity=s.bright?2.3f:.25f;heroLight.color=s.bright?Models.Ember:Models.Teal;
            var currentIds=new HashSet<int>();foreach(var e in s.enemies)currentIds.Add(e.id);
            var stale=new List<int>();foreach(var pair in enemyModels)if(!currentIds.Contains(pair.Key)){Destroy(pair.Value);stale.Add(pair.Key);}
            foreach(int id in stale)enemyModels.Remove(id);
            foreach(var e in s.enemies)
            {
                GameObject o;if(!enemyModels.TryGetValue(e.id,out o)){o=Make(e.kind);enemyModels.Add(e.id,o);}
                o.SetActive(e.hp>0);if(e.hp<=0)continue;
                float walk=e.stun>0?0:Vector2.Distance(e.pos,s.pos)>1.8f?.7f:0;
                Animate(o,e.pos,e.kind==2?.12f+Mathf.Sin(Time.unscaledTime*2)*.1f:0,e.facing,walk,e.flash,e.kind==1?1:0,false,e.flash>0,dt);
                if(e.flash>0)o.transform.localScale=Vector3.one*(1+e.flash*.35f);
                else o.transform.localScale=Vector3.Lerp(o.transform.localScale,Vector3.one,dt*12);
            }
            for(int i=0;i<s.dangers.Count;i++)
            {
                if(i>=dangerModels.Count)
                {
                    var g=Models.MeshPart("Attack warning",fx,Models.Lathe(new[]{new Vector2(1,0),new Vector2(1,.01f)},48),Vector3.zero,Vector3.one,Models.Red);
                    g.GetComponent<MeshFilter>().sharedMesh.name="WarningDisc";dangerModels.Add(g);
                }
                var h=s.dangers[i];var o=dangerModels[i];o.SetActive(true);
                var mf=o.GetComponent<MeshFilter>();
                if(h.kind==1){if(mf.sharedMesh.name!="WarningBox") {var cube=Models.BevelCube();cube.name="WarningBox";mf.sharedMesh=cube;}}
                else if(mf.sharedMesh.name!="WarningDisc"){var disc=Models.Lathe(new[]{new Vector2(1,0),new Vector2(1,.01f)},48);disc.name="WarningDisc";mf.sharedMesh=disc;}
                o.transform.position=P(h.pos,.09f);o.transform.localScale=h.kind==0?new Vector3(h.radius,1,h.radius):new Vector3(h.width,.018f,h.depth);
                float progress=Mathf.Floor(Mathf.Clamp01(h.age/h.wait)*10)/10;Color c=h.hit?Models.Hex("FFE1B5"):Color.Lerp(Models.Hex("743E49"),Models.Hex("E47257"),progress);
                o.GetComponent<Renderer>().sharedMaterial=Models.Mat(c,true);
            }
            for(int i=s.dangers.Count;i<dangerModels.Count;i++)dangerModels[i].SetActive(false);
            for(int i=0;i<s.bolts.Count;i++)
            {
                if(i>=boltModels.Count)boltModels.Add(Models.Orb("Lantern bolt",fx,Vector3.zero,Vector3.one*.25f,Models.Ember,true));
                boltModels[i].SetActive(true);boltModels[i].transform.position=P(s.bolts[i].pos,1);boltModels[i].transform.localScale=Vector3.one*(s.bolts[i].empowered?.4f:.24f);
            }
            for(int i=s.bolts.Count;i<boltModels.Count;i++)boltModels[i].SetActive(false);
            if(s.swing>0&&live)
            {
                Vector2 side=new Vector2(s.direction.y,-s.direction.x);
                Vector2 p=s.pos+s.direction*(s.weapon==1?1.6f:1.25f)+side*Mathf.Sin(s.swing*15);
                Spawn(P(p,.9f+s.jump),Models.Ember,Vector3.up*.3f,.12f,.12f);
            }
            for(int i=particles.Count-1;i>=0;i--)
            {
                var p=particles[i];p.life-=dt;
                if(p.life<=0){Destroy(p.obj);particles.RemoveAt(i);continue;}
                p.obj.transform.position+=p.velocity*dt;p.velocity+=Vector3.down*dt*2;p.obj.transform.localScale=Vector3.one*(p.life/p.total)*.16f;
            }
            shake=Mathf.Max(0,shake-dt);cam.transform.position=new Vector3(shake>0?Mathf.Sin(Time.unscaledTime*90)*shake*.18f:0,11.5f,-22);
        }
        void Animate(GameObject g,Vector2 pos,float height,float facing,float walk,float attack,int w,bool dodge,bool blink,float dt)
        {
            g.transform.position=P(pos,height);
            var rig=g.transform.Find("Rig");if(!rig)return;
            float bob=walk*Mathf.Abs(Mathf.Sin(Time.unscaledTime*10))*.06f;
            rig.localPosition=new Vector3(0,bob,0);rig.localRotation=Quaternion.Euler(dodge?-30:0,facing>=0?-35:35,dodge?(facing>=0?-30:30):0);
            float stride=Mathf.Sin(Time.unscaledTime*11)*walk*25;
            var l=rig.Find("LegL");var r=rig.Find("LegR");
            if(l)l.localRotation=Quaternion.Euler(stride,0,0);if(r)r.localRotation=Quaternion.Euler(-stride,0,0);
            for(int i=0;i<4;i++){var leg=rig.Find("Leg"+i);if(leg)leg.localRotation=Quaternion.Euler(stride*(i%2==0?1:-1),0,0);}
            var arm=rig.Find("ArmR");
            if(arm)arm.localRotation=Quaternion.Euler(attack>0?-95+Mathf.Sin(attack*17)*70:stride*.4f,attack>0?-40:0,attack>0?-35:0);
            var scarf=rig.Find("ScarfTail");if(scarf)scarf.localRotation=Quaternion.Euler(-20+Mathf.Sin(Time.unscaledTime*7)*walk*15,0,28);
            // Damage feedback is a scale pulse, keeping the whole silhouette visible.
        }
        void Spawn(Vector3 p,Color color,Vector3 velocity,float life,float size=.16f)
        {
            if(particles.Count>160)return;
            var o=Models.Orb("Spark",fx,p,Vector3.one*size,color,true);particles.Add(new Particle{obj=o,velocity=velocity,life=life,total=life});
        }
        public void Signal(Signal e)
        {
            if(e.kind=="hurt"||e.kind=="skill")shake=.25f;
            if(e.kind=="hit"||e.kind=="reveal"||e.kind=="kill"||e.kind=="skill"||e.kind=="heal"||e.kind=="toggle")
            {
                int n=e.kind=="kill"?14:7;
                for(int i=0;i<n;i++){float a=i*6.28f/n;Spawn(P(e.pos,1),e.kind=="heal"?Models.Teal:Models.Ember,new Vector3(Mathf.Cos(a)*1.8f,1.5f,Mathf.Sin(a)*1.8f),.4f+i*.02f);}
            }
            if(e.kind=="hit"||e.kind=="reveal"||e.kind=="hurt"||e.kind=="heal")
            {
                var g=new GameObject("Combat number");g.transform.SetParent(fx);g.transform.position=P(e.pos,2.5f);g.transform.rotation=cam.transform.rotation;g.transform.localScale=Vector3.one*.15f;
                var text=g.AddComponent<TextMeshPro>();text.font=UI.Font;text.fontSize=22;text.alignment=TextAlignmentOptions.Center;text.text=(e.kind=="heal"?"+":"")+Mathf.RoundToInt(e.amount);
                text.color=e.kind=="hurt"?Models.Hex("FF8979"):e.kind=="heal"?Models.Hex("94D4BD"):Models.Ember;text.rectTransform.sizeDelta=new Vector2(20,7);
                g.AddComponent<FloatingNumber>();
            }
        }
    }
    public sealed class FloatingNumber:MonoBehaviour
    {
        float life=.7f;void Update(){life-=Time.unscaledDeltaTime;transform.position+=Vector3.up*Time.unscaledDeltaTime*.9f;if(life<=0)Destroy(gameObject);}
    }
    public sealed class Sound:MonoBehaviour
    {
        AudioSource music,fx;bool focused=true;readonly Dictionary<string,AudioClip> clips=new Dictionary<string,AudioClip>();
        public bool Muted=true;
        public void Setup()
        {
            Muted=PlayerPrefs.GetInt("MistLantern.Muted",1)==1;
            music=gameObject.AddComponent<AudioSource>();fx=gameObject.AddComponent<AudioSource>();
            music.loop=true;music.volume=.12f;music.clip=Ambient();music.Play();Apply();
        }
        public void Toggle(){Muted=!Muted;PlayerPrefs.SetInt("MistLantern.Muted",Muted?1:0);Apply();}
        public void Apply(){AudioListener.volume=Saves.IsTest||Muted||!focused?0:1;}
        void OnApplicationFocus(bool f){focused=f;if(music){if(f)music.UnPause();else music.Pause();}Apply();}
        public void Play(string name)
        {
            if(!fx||name=="enter")return;
            AudioClip c;if(!clips.TryGetValue(name,out c))
            {
                int length=name=="skill"?10000:name=="clear"?16000:4200;
                float[] data=new float[length];float freq=name=="hit"||name=="reveal"?150:name=="hurt"?85:name=="clear"?440:name=="heal"?550:name=="toggle"?330:220;
                for(int i=0;i<length;i++){float t=i/22050f,envelope=Mathf.Pow(1-i/(float)length,2);data[i]=Mathf.Sin(t*freq*6.283f*(1-t*.3f))*envelope*.24f+Mathf.Sin(t*freq*12.56f)*envelope*.07f;}
                c=AudioClip.Create(name,length,1,22050,false);c.SetData(data,0);clips[name]=c;
            }
            fx.PlayOneShot(c,.45f);
        }
        AudioClip Ambient()
        {
            int count=22050*12;var data=new float[count];float[] notes={110,130.8128f,164.8138f,196};
            for(int i=0;i<count;i++)
            {float t=i/22050f;float v=0;foreach(float f in notes)v+=Mathf.Sin(t*f*6.283185f)*.035f;data[i]=v*Mathf.Sin(Mathf.PI*i/count)*Mathf.Sin(Mathf.PI*i/count);}
            var c=AudioClip.Create("Original lantern ambience",count,1,22050,false);c.SetData(data,0);return c;
        }
    }
    public static class UI
    {
        public static TMP_FontAsset Font;
        public static Color Ink=Models.Hex("172C3A"),Cream=Models.Hex("EFE4CD"),Gold=Models.Hex("D5B77A"),Muted=Models.Hex("A9BCC4");
        public static RectTransform Rect(string n,Transform p,float x,float y,float w,float h)
        {
            var g=new GameObject(n,typeof(RectTransform));var r=g.GetComponent<RectTransform>();r.SetParent(p,false);
            r.anchorMin=r.anchorMax=new Vector2(0,1);r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(w,h);return r;
        }
        public static Image Panel(Transform p,string n,float x,float y,float w,float h,Color c)
        {var r=Rect(n,p,x,y,w,h);var im=r.gameObject.AddComponent<Image>();im.color=c;return im;}
        public static TextMeshProUGUI Text(Transform p,string n,string value,float x,float y,float w,float h,float size,Color c,bool bold=false,TextAlignmentOptions align=TextAlignmentOptions.TopLeft)
        {
            var r=Rect(n,p,x,y,w,Mathf.Max(h,size*1.6f));var t=r.gameObject.AddComponent<TextMeshProUGUI>();t.font=Font;t.text=value;t.fontSize=size;t.color=c;
            t.fontStyle=bold?FontStyles.Bold:FontStyles.Normal;t.alignment=align;t.enableWordWrapping=true;t.raycastTarget=false;t.overflowMode=TextOverflowModes.Overflow;return t;
        }
        public static Button Button(Transform p,string n,string label,float x,float y,float w,float h,System.Action action,bool primary=false,bool enabled=true)
        {
            var im=Panel(p,n,x,y,w,h,primary?Gold:Models.Hex("254351"));var b=im.gameObject.AddComponent<Button>();b.targetGraphic=im;b.interactable=enabled;
            var colors=b.colors;colors.normalColor=Color.white;colors.highlightedColor=new Color(1.16f,1.16f,1.16f);colors.pressedColor=new Color(.8f,.8f,.8f);colors.disabledColor=new Color(.45f,.45f,.45f,.6f);b.colors=colors;
            Text(im.transform,"Label",label,12,6,w-24,h-12,22,primary?Ink:Cream,true,TextAlignmentOptions.Midline);
            b.onClick.AddListener(()=>{if(action!=null)action();});return b;
        }
    }
}
