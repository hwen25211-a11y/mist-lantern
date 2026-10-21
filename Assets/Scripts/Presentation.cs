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
        GameObject hero,placedLamp,ward,backdrop;
        readonly Dictionary<int,GameObject> enemyModels=new Dictionary<int,GameObject>();
        readonly List<GameObject> dangerModels=new List<GameObject>(),boltModels=new List<GameObject>();
        readonly List<Particle> particles=new List<Particle>();
        static Mesh disc,ring,slash,diamond;
        static readonly Dictionary<string,Material> effects=new Dictionary<string,Material>();
        int area=-1,weapon=-1;Vector2 previous;float walking,shake,animClock;Vector3 cameraHome;
        class Particle{public GameObject obj;public Vector3 velocity;public float life,total,size;public bool flat;}
        static void Release(Object o){if(!o)return;if(Application.isPlaying)Object.Destroy(o);else Object.DestroyImmediate(o);}
        public static Vector3 P(Vector2 p,float y=0){return new Vector3(p.x,y,p.y);}
        static Material FX(Color c)
        {
            string k=ColorUtility.ToHtmlStringRGBA(c);Material m;if(effects.TryGetValue(k,out m)&&m)return m;
            m=new Material(Shader.Find("Mist/SoftFX")){color=c};effects[k]=m;return m;
        }
        static Mesh Radial(float inner,float outer,float arc=6.283185f)
        {
            int n=48;var v=new Vector3[(n+1)*2];var uv=new Vector2[v.Length];var color=new Color[v.Length];var t=new int[n*6];
            for(int i=0;i<=n;i++){float a=-arc*.5f+arc*i/n;v[i*2]=new Vector3(Mathf.Cos(a)*inner,0,Mathf.Sin(a)*inner);v[i*2+1]=new Vector3(Mathf.Cos(a)*outer,0,Mathf.Sin(a)*outer);color[i*2]=new Color(1,1,1,inner==0?.12f:.3f);color[i*2+1]=new Color(1,1,1,1);if(i<n){int q=i*6,k=i*2;t[q]=k;t[q+1]=k+2;t[q+2]=k+1;t[q+3]=k+1;t[q+4]=k+2;t[q+5]=k+3;}}
            var mesh=new Mesh{name="Radial effect"};mesh.vertices=v;mesh.triangles=t;mesh.colors=color;mesh.uv=uv;mesh.RecalculateNormals();return mesh;
        }
        GameObject Effect(string name,Mesh mesh,Color color)
        {var g=Models.Root(name,fx);g.AddComponent<MeshFilter>().sharedMesh=mesh;g.AddComponent<MeshRenderer>().sharedMaterial=FX(color);return g;}
        public void Setup()
        {
            if(!disc){disc=Radial(0,1);ring=Radial(.96f,1);slash=Radial(.77f,1,3.6f);}
            cam=new GameObject("Adventure Camera").AddComponent<Camera>();cam.tag="MainCamera";cam.orthographic=true;cam.orthographicSize=6.8f;
            cameraHome=new Vector3(0,11,-23);cam.transform.position=cameraHome;cam.transform.LookAt(new Vector3(0,1.1f,1.3f));
            cam.backgroundColor=Models.Hex("233D49");cam.clearFlags=CameraClearFlags.SolidColor;cam.nearClipPlane=.1f;cam.farClipPlane=120;cam.gameObject.AddComponent<AudioListener>();
            RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Trilight;RenderSettings.ambientSkyColor=Models.Hex("586D79");RenderSettings.ambientEquatorColor=Models.Hex("2D424A");RenderSettings.ambientGroundColor=Models.Hex("222E2E");RenderSettings.fog=true;RenderSettings.fogColor=Models.Hex("243D49");RenderSettings.fogMode=FogMode.Linear;RenderSettings.fogStartDistance=33;RenderSettings.fogEndDistance=65;
            var key=new GameObject("Lantern moonlight").AddComponent<Light>();key.type=LightType.Directional;key.color=Models.Hex("FFE5B5");key.intensity=.95f;key.transform.rotation=Quaternion.Euler(45,-35,0);key.shadows=LightShadows.Soft;key.shadowStrength=.65f;
            var rim=new GameObject("Cool skylight").AddComponent<Light>();rim.type=LightType.Directional;rim.color=Models.Hex("7EBFC9");rim.intensity=.18f;rim.transform.rotation=Quaternion.Euler(25,140,0);
            actors=new GameObject("Characters").transform;fx=new GameObject("Effects").transform;
            placedLamp=Sculpt.Lantern(actors,Vector3.zero,.95f);placedLamp.SetActive(false);
            ward=Effect("Lantern's field",disc,new Color(.94f,.72f,.35f,.14f));ward.transform.localScale=new Vector3(3.4f,1,3.4f);
            var edge=Effect("Light boundary",ring,new Color(1,.77f,.4f,.3f));edge.transform.SetParent(ward.transform,false);edge.transform.localPosition=Vector3.up*.006f;
            // Only the remote skyline uses a matte painting; all playable and middle-distance structures are solid.
            backdrop=new GameObject("Distant atmospheric skyline");backdrop.transform.SetParent(cam.transform,false);backdrop.transform.localPosition=new Vector3(0,2.1f,80);
            var mesh=new Mesh{name="Distant matte"};mesh.vertices=new[]{new Vector3(-16,-5.33f,0),new Vector3(16,-5.33f,0),new Vector3(16,5.33f,0),new Vector3(-16,5.33f,0)};mesh.uv=new[]{new Vector2(0,0),new Vector2(1,0),new Vector2(1,1),new Vector2(0,1)};mesh.triangles=new[]{0,2,1,0,3,2};mesh.RecalculateNormals();
            backdrop.AddComponent<MeshFilter>().sharedMesh=mesh;var mat=new Material(Shader.Find("Mist/Backdrop"));mat.mainTexture=Resources.Load<Texture2D>("Art/Worlds-v2");backdrop.AddComponent<MeshRenderer>().sharedMaterial=mat;
            SetStage(0);
        }
        void SetStage(int next)
        {
            if(next==area)return;area=next;if(stage){stage.gameObject.SetActive(false);Release(stage.gameObject);}
            var prefab=Resources.Load<GameObject>("Stages/Area"+next);stage=(prefab?Instantiate(prefab):Models.Stage(next)).transform;
            StaticBatchingUtility.Combine(stage.gameObject);
            if(backdrop){var m=backdrop.GetComponent<Renderer>().sharedMaterial;m.mainTextureScale=new Vector2(1,.331f);m.mainTextureOffset=new Vector2(0,(2-next)/3f+.001f);}
        }
        public void ResetModels()
        {
            foreach(var g in enemyModels.Values)if(g){g.SetActive(false);Release(g);}enemyModels.Clear();
            if(hero){hero.SetActive(false);Release(hero);}weapon=-1;
            foreach(var p in particles)if(p.obj){p.obj.SetActive(false);Release(p.obj);}particles.Clear();
            foreach(var n in fx.GetComponentsInChildren<FloatingNumber>()){n.gameObject.SetActive(false);Release(n.gameObject);}
            foreach(var g in dangerModels)g.SetActive(false);foreach(var g in boltModels)g.SetActive(false);placedLamp.SetActive(false);ward.SetActive(false);
        }
        GameObject Make(int kind,int w=0)
        {
            var prefab=Resources.Load<GameObject>("Models/"+(kind<0?"Hunter"+w:"Enemy"+kind));var g=prefab?Instantiate(prefab):Models.Character(kind,w);g.transform.SetParent(actors,false);
            var shadow=Effect("Contact shadow",disc,new Color(.025f,.04f,.05f,.34f));shadow.transform.SetParent(g.transform,false);shadow.transform.localPosition=new Vector3(0,.025f,0);shadow.transform.localScale=new Vector3(kind>=4?1.2f:.7f,1,.45f);return g;
        }
        public void Render(Run s,float dt,bool live)
        {
            if(s==null){ward.SetActive(false);return;}SetStage(s.room/4);
            if(weapon!=s.weapon||!hero){if(hero)Release(hero);hero=Make(-1,s.weapon);weapon=s.weapon;}
            float step=live&&s.hitStop<=0?dt:0;animClock+=step;
            float moved=Vector2.Distance(previous,s.pos);previous=s.pos;walking=Mathf.Lerp(walking,live&&moved>.001f?1:0,dt*12);
            float progress=s.actionDuration<=0?-1:s.actionAge<s.windup?s.actionAge/Mathf.Max(.001f,s.windup)*.45f:.45f+(s.actionAge-s.windup)/Mathf.Max(.001f,s.actionDuration-s.windup)*.55f;
            Animate(hero,s.pos,s.jump,s.direction.x,walking,progress,s.weapon,s.dodgeTime>0,s.hurtFlash,s.guarding,s.charge,step);
            placedLamp.SetActive(s.lampPlaced);placedLamp.transform.position=P(s.lampPos,.08f);
            ward.SetActive(s.bright);ward.transform.position=P(s.lampPlaced?s.lampPos:s.pos,.045f);
            var lantern=hero.transform.Find("Rig/Body/ArmL/Forearm/Lantern");if(lantern)lantern.gameObject.SetActive(!s.lampPlaced);
            var current=new HashSet<int>();foreach(var e in s.enemies)current.Add(e.id);var stale=new List<int>();foreach(var pair in enemyModels)if(!current.Contains(pair.Key)){Release(pair.Value);stale.Add(pair.Key);}foreach(int id in stale)enemyModels.Remove(id);
            foreach(var e in s.enemies)
            {
                GameObject g;if(!enemyModels.TryGetValue(e.id,out g)){g=Make(e.kind);enemyModels.Add(e.id,g);}
                if(e.hp<=0){g.SetActive(false);continue;}g.SetActive(true);
                float ep=e.action>0?(1-e.action/e.actionDuration)*.45f:e.recovery>0?.45f+Mathf.Clamp01(1-e.recovery/(e.kind>=4?.55f:.22f))*.55f:-1;
                float walk=e.stun>0||e.action>0||e.recovery>0?0:Vector2.Distance(e.pos,s.pos)>1.8f?.7f:0;
                Animate(g,e.pos,e.kind==2?.25f+Mathf.Sin(animClock*3)*.12f:0,e.facing,walk,ep,e.kind==1||e.kind==4?1:0,false,e.flash,false,0,step);
                var head=g.transform.Find("Rig/Body/Head");if(head&&e.kind==6)head.localRotation=Quaternion.Euler(0,0,Mathf.Sin(animClock)*6);
            }
            for(int i=0;i<s.dangers.Count;i++)
            {
                if(i>=dangerModels.Count){var g=Effect("Attack footprint",disc,new Color(.95f,.22f,.15f,.25f));var border=Effect("Attack boundary",ring,new Color(1,.43f,.27f,.8f));border.transform.SetParent(g.transform,false);border.transform.localPosition=Vector3.up*.015f;dangerModels.Add(g);}
                var h=s.dangers[i];var o=dangerModels[i];o.SetActive(true);o.transform.position=P(h.pos,.065f);
                o.transform.localScale=h.kind==0?new Vector3(h.radius,1,h.radius):new Vector3(h.width,.012f,h.depth);
                var mf=o.GetComponent<MeshFilter>();mf.sharedMesh=h.kind==0?disc:Models.BevelCube();o.transform.GetChild(0).gameObject.SetActive(h.kind==0);
                float p=Mathf.Clamp01(h.age/h.wait);int band=Mathf.FloorToInt(p*6);o.GetComponent<Renderer>().sharedMaterial=FX(h.hit?new Color(1,.8f,.44f,.32f):new Color(.95f,.23f,.17f,.1f+band*.025f));
            }
            for(int i=s.dangers.Count;i<dangerModels.Count;i++)dangerModels[i].SetActive(false);
            for(int i=0;i<s.bolts.Count;i++)
            {
                if(i>=boltModels.Count){var g=Models.Orb("Star bolt",fx,Vector3.zero,Vector3.one*.2f,Models.Ember,true);boltModels.Add(g);}
                var b=s.bolts[i];var o=boltModels[i];o.SetActive(true);o.transform.position=P(b.pos,1.05f);o.transform.localScale=new Vector3(b.empowered?.32f:.2f,.15f,.42f);o.transform.rotation=Quaternion.LookRotation(new Vector3(b.velocity.x,0,b.velocity.y));
                if(live&&i<8&&Time.frameCount%3==0)Spark(P(b.pos,1.05f),Models.Ember,Vector3.zero,.16f,.07f);
            }
            for(int i=s.bolts.Count;i<boltModels.Count;i++)boltModels[i].SetActive(false);
            for(int i=particles.Count-1;i>=0;i--){var p=particles[i];p.life-=live?dt:0;if(p.life<=0){Release(p.obj);particles.RemoveAt(i);continue;}p.obj.transform.position+=p.velocity*step;if(!p.flat)p.velocity+=Vector3.down*step*4;float f=p.life/p.total;p.obj.transform.localScale=Vector3.one*p.size*(p.flat?1+(1-f)*.3f:f);}
            shake=Mathf.Max(0,shake-dt);cam.transform.position=cameraHome+new Vector3(Mathf.Sin(Time.unscaledTime*85)*shake*.1f,Mathf.Cos(Time.unscaledTime*70)*shake*.06f,0);
        }
        void Animate(GameObject g,Vector2 pos,float height,float facing,float walk,float action,int w,bool dodge,float hurt,bool guard,float charge,float dt)
        {
            g.transform.position=P(pos,height);var rig=g.transform.Find("Rig");if(!rig)return;
            g.transform.localScale=new Vector3(facing>=0?1:-1,1,1);float side=1;rig.localRotation=Quaternion.Euler(dodge?-20:0,-side*20,dodge?-side*35:hurt>0?-side*hurt*32:0);
            float stride=Mathf.Sin(animClock*11)*walk;rig.localPosition=new Vector3(0,Mathf.Abs(stride)*.05f,0);
            var body=rig.Find("Body");if(body){body.localRotation=Quaternion.Euler(0,action>=0?side*Mathf.Sin(action*6.28f)*25:0,action>=0?side*Mathf.Sin(action*6.28f)*7:0);}
            foreach(string name in new[]{"LegL","LegR","Leg0","Leg1","Leg2","Leg3"}){var leg=rig.Find(name);if(leg){float swing=stride*(name.EndsWith("L")||name.EndsWith("0")||name.EndsWith("2")?1:-1);leg.localRotation=Quaternion.Euler(swing*26,0,0);var shin=leg.Find("Shin");if(shin)shin.localRotation=Quaternion.Euler(-Mathf.Max(0,swing)*38,0,0);}}
            var right=rig.Find("Body/ArmR");var left=rig.Find("Body/ArmL");
            if(right){float angle=action<0?charge>0?-120*Mathf.Clamp01(charge/.4f):stride*12-8:action<.2f?Mathf.Lerp(0,-125,action/.2f):action<.45f?Mathf.Lerp(-125,65,(action-.2f)/.25f):Mathf.Lerp(65,0,(action-.45f)/.55f);right.localRotation=Quaternion.Euler(guard?-70:angle,action>=0?side*30:0,action>=0?-side*25:guard?side*45:0);}
            if(left)left.localRotation=Quaternion.Euler(guard?-65:-stride*10+3,0,guard?-side*28:action>=0?side*15:4);
            var rightElbow=right?right.Find("Forearm"):null;var leftElbow=left?left.Find("Forearm"):null;
            if(rightElbow)rightElbow.localRotation=Quaternion.Euler(guard?-28:action>=0?-12-Mathf.Sin(action*Mathf.PI)*26:-12,0,0);
            if(leftElbow)leftElbow.localRotation=Quaternion.Euler(guard?-28:-5-Mathf.Abs(stride)*8,0,0);
            var scarf=rig.Find("Body/ScarfTail");if(scarf)scarf.localRotation=Quaternion.Euler(Mathf.Sin(animClock*5)*7,Mathf.Sin(animClock*7)*walk*14,Mathf.Sin(animClock*5)*5);
            var cape=rig.Find("Body/ScarfTail");var capeTip=cape?cape.Find("CapeTip"):null;if(capeTip){cape.localRotation=Quaternion.Euler(Mathf.Sin(animClock*3)*2+walk*3,Mathf.Sin(animClock*3.5f)*walk*5,Mathf.Sin(animClock*2.8f)*2);capeTip.localRotation=Quaternion.Euler(Mathf.Sin(animClock*3-1)*3+walk*4,0,Mathf.Sin(animClock*2.8f-1)*3);}
            for(int i=-1;i<=1;i+=2){var wing=rig.Find(i<0?"Body/WingL":"Body/WingR");if(wing)wing.localRotation=Quaternion.Euler(0,i*Mathf.Sin(animClock*7)*22,0);}
        }
        void Spark(Vector3 pos,Color c,Vector3 velocity,float life,float size)
        {
            if(particles.Count>=180)return;var g=Models.Orb("Impact chip",fx,pos,Vector3.one*size,c,true);particles.Add(new Particle{obj=g,velocity=velocity,life=life,total=life,size=size});
        }
        void Wave(Vector2 pos,float radius,Color c,float life,bool sweep=false,float angle=0)
        {
            var g=Effect(sweep?"Blade arc":"Ground pulse",sweep?slash:ring,c);g.transform.position=P(pos,sweep?1.05f:.08f);g.transform.rotation=Quaternion.Euler(sweep?-18:0,-angle,0);g.transform.localScale=Vector3.one*radius;particles.Add(new Particle{obj=g,life=life,total=life,size=radius,flat=true});
        }
        public void Signal(Signal e)
        {
            var s=Game.Instance.sim.s;
            if(e.kind=="attack"||e.kind=="skill")
            {float a=Mathf.Atan2(s.attackDirection.y,s.attackDirection.x)*Mathf.Rad2Deg;if(s.weapon!=2)Wave(e.pos,e.kind=="skill"?3.2f:s.weapon==1?2.55f:2.05f,e.kind=="skill"?new Color(.65f,1,.9f,.8f):new Color(1,.91f,.66f,.85f),.18f,true,a);if(e.kind=="skill")Wave(e.pos,1.8f,new Color(.55f,.95f,.86f,.7f),.4f);}
            if(e.kind=="hit"||e.kind=="reveal"||e.kind=="kill"||e.kind=="break"||e.kind=="parry")
            {
                shake=Mathf.Max(shake,e.kind=="kill"?.3f:e.kind=="break"?.45f:.12f);int n=e.kind=="kill"?16:7;
                for(int i=0;i<n;i++){float a=i*6.28f/n;Spark(P(e.pos,1.1f),i%2==0?Models.Ember:Models.Bone,new Vector3(Mathf.Cos(a)*3,1.5f+Mathf.Sin(a),Mathf.Sin(a)*1.4f),.22f+i*.018f,i%2==0?.1f:.065f);}
                if(e.kind=="parry"||e.kind=="break")Wave(e.pos,1.15f,new Color(.65f,.95f,1,.8f),.28f);
            }
            if(e.kind=="hurt")shake=.6f;
            if(e.kind=="impact"){Wave(e.pos,Mathf.Max(.5f,e.amount),new Color(1,.56f,.25f,.7f),.28f);for(int i=0;i<6;i++)Spark(P(e.pos,.2f),Models.Gold,new Vector3(Mathf.Sin(i)*2,1,Mathf.Cos(i)*2),.32f,.09f);}
            if(e.kind=="place"||e.kind=="recall"||e.kind=="toggle"||e.kind=="burst")Wave(e.pos,e.kind=="burst"?2.6f:1.3f,new Color(1,.8f,.45f,.7f),.4f);
            if(e.kind=="hit"||e.kind=="reveal"||e.kind=="hurt"||e.kind=="heal"||e.kind=="parry"||e.kind=="break")
            {
                var g=new GameObject("Combat number");g.transform.SetParent(fx);g.transform.position=P(e.pos,2.1f);g.transform.rotation=cam.transform.rotation;g.transform.localScale=Vector3.one*.11f;
                var t=g.AddComponent<TextMeshPro>();t.font=UI.Font;t.fontSize=e.kind=="hurt"?23:19;t.fontStyle=FontStyles.Bold;t.alignment=TextAlignmentOptions.Center;
                t.text=e.kind=="parry"?"精准格挡":e.kind=="break"?"破势":(e.kind=="heal"?"+":"")+Mathf.RoundToInt(e.amount);t.color=e.kind=="hurt"?Models.Hex("FF917E"):e.kind=="parry"?Models.Hex("A9EAE4"):Models.Ember;t.rectTransform.sizeDelta=new Vector2(24,7);g.AddComponent<FloatingNumber>();
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
