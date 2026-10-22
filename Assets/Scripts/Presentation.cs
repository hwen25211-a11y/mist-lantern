using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

namespace Mist
{
    public sealed partial class World:MonoBehaviour
    {
        public Camera cam;
        Transform stage,actors,fx;
        CombatActor hero;
        GameObject placedLamp,ward,backdrop;
        readonly Dictionary<int,CombatActor> enemies=new Dictionary<int,CombatActor>();
        readonly List<CombatActor> defeated=new List<CombatActor>();
        readonly List<DangerMarker> dangerMarkers=new List<DangerMarker>();
        readonly List<GameObject> boltModels=new List<GameObject>();
        static Mesh disc,ring,diamond,rectangle,rectangleBorder;
        static Material fxMaterial;
        static MaterialPropertyBlock fxBlock;
        int area=-1,weapon=-1,room=-1;Vector2 previous;float walking,animClock,visualClock,footClock;
        Vector3 cameraHome,cameraOffset,cameraVelocity;float cameraPunch;
        static void Release(UnityEngine.Object o){if(!o)return;if(Application.isPlaying)UnityEngine.Object.Destroy(o);else UnityEngine.Object.DestroyImmediate(o);}
        public static Vector3 P(Vector2 p,float y=0){return new Vector3(p.x,y,p.y);}
        static Material FX(){if(!fxMaterial)fxMaterial=new Material(Shader.Find("Mist/SoftFX")){color=Color.white};return fxMaterial;}
        static Mesh Radial(float inner,float outer,float arc=6.283185f)
        {
            const int n=64;var v=new Vector3[(n+1)*2];var c=new Color[v.Length];var t=new int[n*6];
            for(int i=0;i<=n;i++){float a=-arc*.5f+arc*i/n;v[i*2]=new Vector3(Mathf.Cos(a)*inner,0,Mathf.Sin(a)*inner);v[i*2+1]=new Vector3(Mathf.Cos(a)*outer,0,Mathf.Sin(a)*outer);c[i*2]=Color.white;c[i*2+1]=new Color(1,1,1,inner==0?0:1);if(i<n){int q=i*6,k=i*2;t[q]=k;t[q+1]=k+2;t[q+2]=k+1;t[q+3]=k+1;t[q+4]=k+2;t[q+5]=k+3;}}
            var m=new Mesh{name="Soft radial geometry"};m.vertices=v;m.colors=c;m.triangles=t;m.RecalculateNormals();return m;
        }
        static Mesh RectMesh(bool border)
        {
            var v=new List<Vector3>();var c=new List<Color>();var t=new List<int>();
            Action<float,float,float,float> quad=(x0,z0,x1,z1)=>{int n=v.Count;v.Add(new Vector3(x0,0,z0));v.Add(new Vector3(x1,0,z0));v.Add(new Vector3(x1,0,z1));v.Add(new Vector3(x0,0,z1));for(int i=0;i<4;i++)c.Add(Color.white);t.AddRange(new[]{n,n+2,n+1,n,n+3,n+2});};
            if(!border)quad(-.5f,-.5f,.5f,.5f);else{quad(-.5f,-.5f,.5f,-.485f);quad(-.5f,.485f,.5f,.5f);quad(-.5f,-.485f,-.495f,.485f);quad(.495f,-.485f,.5f,.485f);}
            var m=new Mesh{name=border?"Warning rectangle edge":"Warning rectangle"};m.SetVertices(v);m.SetColors(c);m.SetTriangles(t,0);m.RecalculateNormals();return m;
        }
        GameObject Effect(string name,Mesh mesh,Color color,Transform parent=null)
        {
            var g=Models.Root(name,parent?parent:fx);g.AddComponent<MeshFilter>().sharedMesh=mesh;var renderer=g.AddComponent<MeshRenderer>();renderer.sharedMaterial=FX();renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;renderer.receiveShadows=false;TintFX(renderer,color);return g;
        }
        static void TintFX(Renderer renderer,Color color){fxBlock.Clear();fxBlock.SetColor("_Color",color);renderer.SetPropertyBlock(fxBlock);}
        public void Setup()
        {
            if(fxBlock==null)fxBlock=new MaterialPropertyBlock();
            if(!disc){disc=Radial(0,1);ring=Radial(.97f,1);rectangle=RectMesh(false);rectangleBorder=RectMesh(true);diamond=ShardMesh();}
            cam=new GameObject("Adventure Camera").AddComponent<Camera>();cam.tag="MainCamera";cam.orthographic=true;cam.orthographicSize=6.25f;
            cameraHome=new Vector3(0,11,-23);cam.transform.position=cameraHome;cam.transform.LookAt(new Vector3(0,1.1f,1.3f));cam.backgroundColor=Models.Hex("233D49");cam.clearFlags=CameraClearFlags.SolidColor;cam.nearClipPlane=.1f;cam.farClipPlane=120;cam.gameObject.AddComponent<AudioListener>();
            RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Trilight;RenderSettings.ambientSkyColor=Models.Hex("586D79");RenderSettings.ambientEquatorColor=Models.Hex("2D424A");RenderSettings.ambientGroundColor=Models.Hex("222E2E");RenderSettings.fog=true;RenderSettings.fogColor=Models.Hex("243D49");RenderSettings.fogMode=FogMode.Linear;RenderSettings.fogStartDistance=33;RenderSettings.fogEndDistance=65;
            var key=new GameObject("Warm directional key").AddComponent<Light>();key.type=LightType.Directional;key.color=Models.Hex("FFE5C4");key.intensity=.9f;key.transform.rotation=Quaternion.Euler(45,-35,0);key.shadows=LightShadows.Soft;key.shadowStrength=.65f;
            var rim=new GameObject("Cool skylight").AddComponent<Light>();rim.type=LightType.Directional;rim.color=Models.Hex("7EBFC9");rim.intensity=.18f;rim.transform.rotation=Quaternion.Euler(25,140,0);
            actors=new GameObject("Characters").transform;fx=new GameObject("Effects").transform;
            placedLamp=Sculpt.KeeperLantern(actors,.68f);placedLamp.SetActive(false);
            ward=Effect("Lantern soft pool",disc,new Color(.92f,.65f,.25f,.15f));ward.transform.localScale=new Vector3(3.4f,1,3.4f);
            var edge=Effect("Lantern field fine boundary",ring,new Color(.95f,.76f,.4f,.14f),ward.transform);edge.transform.localPosition=Vector3.up*.008f;
            backdrop=new GameObject("Distant atmospheric skyline");backdrop.transform.SetParent(cam.transform,false);backdrop.transform.localPosition=new Vector3(0,2.1f,80);
            var mesh=new Mesh{name="Distant matte"};mesh.vertices=new[]{new Vector3(-16,-5.33f,0),new Vector3(16,-5.33f,0),new Vector3(16,5.33f,0),new Vector3(-16,5.33f,0)};mesh.uv=new[]{new Vector2(0,0),new Vector2(1,0),new Vector2(1,1),new Vector2(0,1)};mesh.triangles=new[]{0,2,1,0,3,2};mesh.RecalculateNormals();backdrop.AddComponent<MeshFilter>().sharedMesh=mesh;
            var material=new Material(Shader.Find("Mist/Backdrop"));material.mainTexture=Resources.Load<Texture2D>("Art/Worlds-v2");backdrop.AddComponent<MeshRenderer>().sharedMaterial=material;
            SetupVisuals();SetStage(0);
        }
        void SetStage(int next)
        {
            if(next==area)return;area=next;if(stage){stage.gameObject.SetActive(false);Release(stage.gameObject);}
            var prefab=Resources.Load<GameObject>("Stages/Area"+next);stage=(prefab?Instantiate(prefab):Models.Stage(next)).transform;StaticBatchingUtility.Combine(stage.gameObject);
            if(backdrop){var m=backdrop.GetComponent<Renderer>().sharedMaterial;m.mainTextureScale=new Vector2(1,.331f);m.mainTextureOffset=new Vector2(0,(2-next)/3f+.001f);}
            RenderSettings.ambientSkyColor=Models.Hex(next==0?"657581":next==1?"60786F":"756C7D");RenderSettings.ambientEquatorColor=Models.Hex(next==0?"364B54":next==1?"324B42":"45414F");
            SetupAtmosphere(next);
        }
        void DeleteActor(CombatActor a){if(a==null)return;Release(a.root);Release(a.shadow);}
        public void ResetModels()
        {
            foreach(var a in enemies.Values)DeleteActor(a);enemies.Clear();foreach(var a in defeated)DeleteActor(a);defeated.Clear();DeleteActor(hero);hero=null;weapon=-1;room=-1;walking=0;cameraPunch=0;cameraOffset=cameraVelocity=Vector3.zero;
            ClearVisuals();foreach(var d in dangerMarkers)d.root.SetActive(false);foreach(var g in boltModels)g.SetActive(false);placedLamp.SetActive(false);ward.SetActive(false);
        }
        CombatActor Make(int kind,int w=0)
        {
            var prefab=Resources.Load<GameObject>("Models/"+(kind<0?"Hunter"+w:"Enemy"+kind));var g=prefab?Instantiate(prefab):Models.Character(kind,w);g.transform.SetParent(actors,false);
            return new CombatActor(g,Effect("Grounded contact shadow",disc,new Color(.025f,.035f,.04f,.58f)),kind);
        }
        public void Render(Run s,float dt,bool live)
        {
            float visualDt=live?Mathf.Min(dt,.05f):0;visualClock+=visualDt;
            if(s==null){ward.SetActive(false);TickVisuals(0);return;}SetStage(s.room/4);
            if(weapon!=s.weapon||hero==null){DeleteActor(hero);hero=Make(-1,s.weapon);weapon=s.weapon;ResetBlade();}
            if(room!=s.room){room=s.room;previous=s.pos;}
            bool playing=s.phase==Phase.Playing;float step=playing&&live&&s.hitStop<=0?visualDt:0;animClock+=step;
            float moved=Vector2.Distance(previous,s.pos);previous=s.pos;walking=Mathf.Lerp(walking,playing&&live&&moved>.001f?1:0,visualDt*14);
            float phase=CombatMotion.Phase(s);var pose=CombatMotion.Sample(s.weapon,s.combo,s.skillAction,phase,s.charge,animClock,walking);
            if(s.guarding){pose.arm=new Vector3(-45,0,35);pose.elbow=new Vector3(-30,0,0);pose.body=new Vector3(3,-8,-3);pose.weapon=new Vector3(0,0,s.weapon==0?-42:-25);}
            if(s.hurtFlash>0)hero.reaction=Mathf.Max(hero.reaction,s.hurtFlash*.6f);
            hero.Apply(pose,s.pos,s.jump,s.direction.x,walking,animClock,s.dodgeTime>0,s.hurtFlash,step);
            FlushSignals();
            placedLamp.SetActive(s.lampPlaced);placedLamp.transform.position=P(s.lampPos,.08f);ward.SetActive(s.bright);ward.transform.position=P(s.lampPlaced?s.lampPos:s.pos,.035f);
            var lantern=hero.leftElbow?hero.leftElbow.Find("Lantern"):null;if(lantern)lantern.gameObject.SetActive(!s.lampPlaced);
            var current=new HashSet<int>();foreach(var e in s.enemies)if(e.hp>0)current.Add(e.id);
            var stale=new List<int>();foreach(var pair in enemies)if(!current.Contains(pair.Key)){if(!pair.Value.dead)DeleteActor(pair.Value);stale.Add(pair.Key);}foreach(int id in stale)enemies.Remove(id);
            foreach(var e in s.enemies){if(e.hp<=0)continue;CombatActor a;if(!enemies.TryGetValue(e.id,out a)){a=Make(e.kind);enemies.Add(e.id,a);}a.EnemyPose(e,animClock,step);}
            for(int i=defeated.Count-1;i>=0;i--)if(defeated[i].TickDeath(visualDt)){DeleteActor(defeated[i]);defeated.RemoveAt(i);}
            RenderDangers(s);RenderBolts(s,step);RenderBlade(s,phase,visualDt);TickVisuals(visualDt);TickAtmosphere(visualDt);
            if(walking>.5f&&s.jump<=0&&s.dodgeTime<=0){footClock+=step;if(footClock>.24f){footClock=0;Dust(P(s.pos,.06f),.32f,2);}}
            cameraVelocity+=(-cameraOffset*95-cameraVelocity*16)*visualDt;cameraOffset+=cameraVelocity*visualDt;cameraPunch=Mathf.MoveTowards(cameraPunch,0,visualDt*.45f);
            cam.transform.position=cameraHome+new Vector3(s.pos.x*.085f,0,s.pos.y*.045f)+cameraOffset;cam.orthographicSize=6.25f-cameraPunch;
        }
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
        public void Play(string name,int weapon=-1,bool strong=false)
        {
            if(!fx||name=="enter"||name=="warning"||name=="wave")return;
            string key=name+":"+weapon;AudioClip clip;if(!clips.TryGetValue(key,out clip)){var data=CraftSound(name,weapon);clip=AudioClip.Create(key,data.Length,1,22050,false);clip.SetData(data,0);clips[key]=clip;}
            fx.pitch=1+Mathf.Sin(Time.unscaledTime*31)*.035f;fx.PlayOneShot(clip,name=="windup"?.23f:strong?.52f:.38f);
        }
        public static float[] CraftSound(string name,int weapon)
        {
            const int rate=22050;float duration=name=="clear"||name=="won"?.68f:name=="windup"?.19f:name=="kill"?.32f:.26f;
            var data=new float[(int)(rate*duration)];var random=new System.Random(1749+Mathf.Max(0,weapon)*37);float low=0,previousNoise=0;
            bool impact=name=="hit"||name=="reveal"||name=="kill"||name=="hurt"||name=="break"||name=="block";
            for(int i=0;i<data.Length;i++){
                float t=i/(float)rate,u=t/duration;float noise=(float)random.NextDouble()*2-1;low=Mathf.Lerp(low,noise,.16f);float high=noise-previousNoise;previousNoise=noise;
                float v;
                if(name=="windup")v=(low*.6f+high*.07f)*Mathf.Pow(Mathf.Sin(u*Mathf.PI),1.4f)*(weapon==1?.45f:.65f);
                else if(impact){float bass=weapon==1?72:weapon==2?210:115;float thump=Mathf.Sin(6.283185f*(bass*t-55*t*t))*Mathf.Exp(-t*38);float attack=(low*.65f+high*.09f)*Mathf.Exp(-t*70);float metal=(Mathf.Sin(6.283185f*t*(weapon==1?477:1037))*.16f+Mathf.Sin(6.283185f*t*1511)*.07f)*Mathf.Exp(-t*(weapon==1?11:24));v=thump*.48f+attack+metal;}
                else if(name=="attack"||name=="skill")v=weapon==2?(Mathf.Sin(6.283185f*(610*t-600*t*t))*.3f+low*.12f)*Mathf.Exp(-t*17):(low*.4f+high*.035f)*Mathf.Sin(u*Mathf.PI)*Mathf.Exp(-t*7);
                else if(name=="parry")v=(Mathf.Sin(t*6.283185f*880)*.3f+Mathf.Sin(t*6.283185f*1763)*.13f)*Mathf.Exp(-t*12)+high*.06f*Mathf.Exp(-t*90);
                else if(name=="dodge"||name=="jump")v=low*.35f*Mathf.Sin(u*Mathf.PI)*Mathf.Exp(-t*9);
                else {float frequency=name=="heal"?660:name=="clear"||name=="won"?440:330;v=(Mathf.Sin(t*6.283185f*frequency)*.2f+Mathf.Sin(t*6.283185f*frequency*1.5f)*.09f)*Mathf.Exp(-t*6)*Mathf.Sin(Mathf.Min(1,t*80)*Mathf.PI*.5f);}
                float edge=Mathf.Min(1,t*1000)*Mathf.Min(1,(duration-t)*800);data[i]=Mathf.Clamp(v*edge,-.92f,.92f);
            }
            return data;
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
        public static void RelicIcon(Transform p,int id,float x,float y,float size)
        {
            var tex=Resources.Load<Texture2D>("Art/Relics-v3");if(!tex)return;
            float[] cols={0,.267f,.502f,.756f,1},rows={0,.36f,.66f,1};int col=id%4,row=id/4;
            var uv=new Rect(cols[col],1-rows[row+1],cols[col+1]-cols[col],rows[row+1]-rows[row]);
            float width=size*uv.width/uv.height;var image=Rect("Relic illustration",p,x+(size-width)*.5f,y,width,size).gameObject.AddComponent<RawImage>();image.texture=tex;image.uvRect=uv;image.raycastTarget=false;
        }
        public static Button Button(Transform p,string n,string label,float x,float y,float w,float h,System.Action action,bool primary=false,bool enabled=true)
        {
            var im=Panel(p,n,x,y,w,h,primary?Gold:Models.Hex("254351"));Panel(im.transform,"Button rule",0,h-2,w,2,new Color(.69f,.57f,.35f,primary?.9f:.4f));var b=im.gameObject.AddComponent<Button>();b.targetGraphic=im;b.interactable=enabled;
            var colors=b.colors;colors.normalColor=Color.white;colors.highlightedColor=new Color(1.16f,1.16f,1.16f);colors.pressedColor=new Color(.8f,.8f,.8f);colors.disabledColor=new Color(.45f,.45f,.45f,.6f);b.colors=colors;
            Text(im.transform,"Label",label,12,6,w-24,h-12,22,primary?Ink:Cream,true,TextAlignmentOptions.Midline);
            b.onClick.AddListener(()=>{if(action!=null)action();});return b;
        }
    }
}
