using System;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace Mist
{
    public sealed partial class World
    {
        sealed class VisualParticle
        {
            public GameObject obj;public Renderer renderer;public Vector3 velocity,scale;public Color color;public float age,life,gravity,spin,growth;public bool billboard;
        }
        sealed class DangerMarker {public GameObject root,fill,border,timer;}
        struct BladePoint {public Vector3 root,tip;public float time;}
        sealed class NumberBatch {public Vector2 pos;public float amount;public bool strong;public string kind;}
        readonly List<VisualParticle> activeVisuals=new List<VisualParticle>();
        readonly Stack<VisualParticle> freeVisuals=new Stack<VisualParticle>();
        readonly Dictionary<int,NumberBatch> numbers=new Dictionary<int,NumberBatch>();
        readonly Dictionary<int,float> lastCallout=new Dictionary<int,float>();
        readonly List<BladePoint> bladePoints=new List<BladePoint>();
        readonly List<Signal> visualSignals=new List<Signal>();
        readonly System.Random visualRandom=new System.Random(86721);
        GameObject bladeObject;Mesh bladeMesh,flareMesh,streakMesh;Vector3[] bladeVertices;Color[] bladeColors;
        float boltDustClock,bladeClock;
        readonly List<GameObject> motes=new List<GameObject>();
        readonly List<Vector3> moteOrigins=new List<Vector3>();
        static readonly Color Warm=new Color(1,.71f,.28f,.85f),Cream=new Color(1,.96f,.78f,.95f),Magic=new Color(.45f,.9f,.87f,.82f);
        public int ActiveVisualCount {get{return activeVisuals.Count;}}
        public int DefeatedVisualCount {get{return defeated.Count;}}
        float Random(float a,float b){return Mathf.Lerp(a,b,(float)visualRandom.NextDouble());}
        static Mesh ShardMesh()
        {
            var m=new Mesh{name="Solid impact shard"};m.vertices=new[]{new Vector3(0,0,.5f),new Vector3(0,0,-.5f),new Vector3(-.15f,0,0),new Vector3(.15f,0,0),new Vector3(0,.15f,0),new Vector3(0,-.15f,0)};m.triangles=new[]{0,2,4,0,4,3,0,3,5,0,5,2,1,4,2,1,3,4,1,5,3,1,2,5};var c=new Color[6];for(int i=0;i<6;i++)c[i]=Color.white;m.colors=c;m.RecalculateNormals();return m;
        }
        static Mesh StarMesh(int points,float inner)
        {
            var v=new Vector3[points*2+1];var c=new Color[v.Length];var t=new int[points*6];v[0]=Vector3.zero;c[0]=Color.white;
            for(int i=0;i<points*2;i++){float a=i*Mathf.PI/points,r=i%2==0?1:inner;v[i+1]=new Vector3(Mathf.Cos(a)*r,Mathf.Sin(a)*r,0);c[i+1]=new Color(1,1,1,i%2==0?0:.9f);t[i*3]=0;t[i*3+1]=i+1;t[i*3+2]=(i+1)%(points*2)+1;}
            var m=new Mesh{name="Hand shaped contact flare"};m.vertices=v;m.colors=c;m.triangles=t;m.RecalculateNormals();return m;
        }
        void SetupVisuals()
        {
            flareMesh=StarMesh(6,.18f);streakMesh=StarMesh(2,.16f);
            bladeVertices=new Vector3[20];bladeColors=new Color[20];var indices=new int[54];for(int i=0;i<9;i++){int n=i*2,q=i*6;indices[q]=n;indices[q+1]=n+1;indices[q+2]=n+2;indices[q+3]=n+2;indices[q+4]=n+1;indices[q+5]=n+3;}
            bladeMesh=new Mesh{name="Actual blade motion ribbon"};bladeMesh.MarkDynamic();bladeMesh.vertices=bladeVertices;bladeMesh.colors=bladeColors;bladeMesh.triangles=indices;bladeObject=Effect("Cleaver motion ribbon",bladeMesh,Color.white);bladeObject.SetActive(false);
        }
        VisualParticle Particle(Mesh mesh,Vector3 position,Vector3 scale,Color color,float life,Vector3 velocity,bool billboard=false,float gravity=0,float growth=0)
        {
            if(activeVisuals.Count>=192)return null;
            VisualParticle p;if(freeVisuals.Count>0)p=freeVisuals.Pop();else{var g=Effect("Pooled combat accent",mesh,color);p=new VisualParticle{obj=g,renderer=g.GetComponent<Renderer>()};}
            p.obj.GetComponent<MeshFilter>().sharedMesh=mesh;p.obj.SetActive(true);p.obj.transform.position=position;p.obj.transform.localScale=scale;p.obj.transform.rotation=billboard?Quaternion.LookRotation(cam.transform.forward):Quaternion.identity;
            p.age=0;p.life=life;p.scale=scale;p.color=color;p.velocity=velocity;p.billboard=billboard;p.gravity=gravity;p.spin=Random(-190,190);p.growth=growth;TintFX(p.renderer,color);activeVisuals.Add(p);return p;
        }
        void Dust(Vector3 at,float radius,int count)
        {
            Color c=area==1?new Color(.3f,.4f,.27f,.16f):new Color(.56f,.52f,.43f,.16f);
            for(int i=0;i<count;i++){float a=Random(0,Mathf.PI*2);var p=Particle(disc,at+new Vector3(Mathf.Cos(a)*radius*.3f,0,Mathf.Sin(a)*radius*.2f),Vector3.one*Random(.12f,.24f),c,Random(.28f,.52f),new Vector3(Mathf.Cos(a)*radius,.16f,Mathf.Sin(a)*radius*.4f),false,0,2.2f);if(p!=null)p.obj.transform.rotation=Quaternion.FromToRotation(Vector3.up,-cam.transform.forward);}
        }
        void Pulse(Vector3 at,float radius,Color color,float duration)
        {
            var p=Particle(ring,at,Vector3.one*radius,color,duration,Vector3.zero,false,0,.65f);if(p!=null)p.spin=0;
        }
        void Contact(Signal e)
        {
            bool blocked=e.kind=="block";Color color=blocked?new Color(.56f,.72f,.8f,.8f):e.weapon==2?Magic:Warm;var at=P(e.pos,e.actorKind>=4?1.35f:1.15f);
            var core=Particle(flareMesh,at,new Vector3(e.strong?.57f:.34f,e.strong?.41f:.28f,1),Cream,e.strong?.11f:.085f,Vector3.zero,true);if(core!=null){core.obj.transform.Rotate(0,0,Random(-40,40));core.growth=-.5f;}
            int count=blocked?4:e.strong?11:7;for(int i=0;i<count;i++){
                float a=Random(-.85f,.85f);Vector3 dir=P(e.direction);dir=Quaternion.Euler(0,a*60,0)*dir;dir.y=Random(.8f,2.1f);
                var p=Particle(i%3==0?diamond:streakMesh,at,new Vector3(Random(.15f,.3f),Random(.03f,.065f),i%3==0?.17f:1),i%3==0?Cream:color,Random(.13f,.3f),dir*Random(1.1f,2.9f),i%3!=0,i%3==0?6:0);
                if(p!=null)p.obj.transform.rotation=Quaternion.LookRotation(cam.transform.forward)*Quaternion.Euler(0,0,Random(-70,70));
            }
            if(e.strong){Dust(P(e.pos,.055f),.6f,4);Pulse(P(e.pos,.06f),.55f,new Color(color.r,color.g,color.b,.3f),.23f);}
            cameraVelocity+=P(e.direction)*(blocked?.12f:e.strong?.7f:.3f);if(e.strong)cameraPunch=Mathf.Max(cameraPunch,.045f);
            CombatActor actor;if(e.targetId!=0&&enemies.TryGetValue(e.targetId,out actor))actor.reaction=e.strong?.24f:.17f;
        }
        void ClearVisuals()
        {
            foreach(var p in activeVisuals){p.obj.SetActive(false);freeVisuals.Push(p);}activeVisuals.Clear();numbers.Clear();lastCallout.Clear();visualSignals.Clear();ResetBlade();
            foreach(var n in fx.GetComponentsInChildren<FloatingNumber>()){n.gameObject.SetActive(false);Release(n.gameObject);}
        }
        void TickVisuals(float dt)
        {
            for(int i=activeVisuals.Count-1;i>=0;i--){var p=activeVisuals[i];p.age+=dt;if(p.age>=p.life){p.obj.SetActive(false);activeVisuals.RemoveAt(i);freeVisuals.Push(p);continue;}
                float t=p.age/p.life;p.obj.transform.position+=p.velocity*dt;p.velocity+=Vector3.down*p.gravity*dt;p.obj.transform.Rotate(0,0,p.spin*dt,Space.Self);
                p.obj.transform.localScale=p.scale*Mathf.Max(.01f,1+p.growth*t)*(p.growth>0?1:1-t*.4f);var c=p.color;c.a*=Mathf.Pow(1-t,1.5f);TintFX(p.renderer,c);
            }
            foreach(var pair in numbers){var n=pair.Value;SpawnNumber(n.pos,Mathf.RoundToInt(n.amount).ToString(),n.kind=="hurt"?new Color(1,.42f,.32f):n.kind=="block"?new Color(.64f,.77f,.82f):new Color(1,.9f,.63f),n.strong,false);}numbers.Clear();
        }
        void SpawnNumber(Vector2 at,string text,Color color,bool strong,bool callout)
        {
            if(fx.GetComponentsInChildren<FloatingNumber>().Length>42)return;
            var g=new GameObject("Impact value");g.transform.SetParent(fx);g.transform.position=P(at,callout?2.55f:2.0f)+cam.transform.right*Random(-.15f,.15f);g.transform.rotation=cam.transform.rotation;g.transform.localScale=Vector3.one*.105f;
            var t=g.AddComponent<TextMeshPro>();t.font=UI.Font;t.text=text;t.fontSize=callout?20:strong?27:22;t.fontStyle=FontStyles.Bold;t.alignment=TextAlignmentOptions.Center;t.color=color;t.rectTransform.sizeDelta=new Vector2(22,5);g.AddComponent<FloatingNumber>().Configure(t,cam.transform.right*Random(-.3f,.3f),strong);
        }
        void Callout(Signal e,string text,Color c)
        {
            float last;if(lastCallout.TryGetValue(e.targetId,out last)&&visualClock-last<.24f)return;lastCallout[e.targetId]=visualClock;SpawnNumber(e.pos,text,c,true,true);
        }
        public void Signal(Signal e)
        {visualSignals.Add(e);}
        void FlushSignals(){foreach(var e in visualSignals)ProcessSignal(e);visualSignals.Clear();}
        void ProcessSignal(Signal e)
        {
            if(e.kind=="hit"||e.kind=="reveal"||e.kind=="block")Contact(e);
            if((e.kind=="hit"||e.kind=="reveal"||e.kind=="block"||e.kind=="hurt")&&e.amount>0){int key=e.kind=="hurt"?-1:e.targetId;NumberBatch n;if(!numbers.TryGetValue(key,out n)){n=new NumberBatch{pos=e.pos,kind=e.kind};numbers[key]=n;}n.amount+=e.amount;n.strong|=e.strong;}
            if(e.kind=="kill"){
                CombatActor actor;if(enemies.TryGetValue(e.targetId,out actor)){actor.StartDeath(e.direction);defeated.Add(actor);enemies.Remove(e.targetId);}
                Color c=e.actorKind==1||e.actorKind==4?Warm:new Color(.63f,.83f,.78f,.68f);for(int i=0;i<8;i++)Particle(diamond,P(e.pos,1.1f),Vector3.one*Random(.08f,.16f),c,Random(.35f,.65f),P(e.direction)*Random(.8f,2)+new Vector3(Random(-1,1),Random(1,3),Random(-1,1)),false,8);
            }
            if(e.kind=="attack"||e.kind=="skill"){
                if(e.weapon==1){var at=P(e.pos+e.direction*1.0f,.07f);Pulse(at,e.kind=="skill"?1.4f:.65f,new Color(1,.71f,.32f,.45f),.3f);Dust(at,1,7);}
                if(e.weapon==2&&hero!=null&&hero.weapon){var at=hero.weapon.TransformPoint(new Vector3(0,1.62f,0));Particle(flareMesh,at,new Vector3(.4f,.4f,1),Magic,.18f,Vector3.zero,true,0,.4f);}
            }
            if(e.kind=="hurt"){cameraVelocity+=new Vector3(-.55f,.25f,0);if(hero!=null)hero.reaction=.24f;}
            if(e.kind=="parry"){Contact(new Signal("hit",e.pos){direction=Vector2.right,strong=true,weapon=2});Pulse(P(e.pos,1.15f),.68f,Magic,.3f);Callout(e,"精准格挡",new Color(.63f,1,.94f));}
            if(e.kind=="break")Callout(e,"破势",new Color(1,.82f,.43f));
            if(e.kind=="impact"){
                Color c=e.actorKind==5?new Color(.55f,.78f,.45f,.62f):e.actorKind==6?new Color(.82f,.57f,.28f,.65f):new Color(1,.49f,.26f,.55f);
                float r=Mathf.Clamp(e.amount*.45f,.4f,1.8f);Pulse(P(e.pos,.06f),r,c,.3f);Dust(P(e.pos,.07f),r,6);
                for(int i=0;i<6;i++){float a=i*Mathf.PI/3;Particle(diamond,P(e.pos,.1f)+new Vector3(Mathf.Cos(a)*r*.55f,0,Mathf.Sin(a)*r*.55f),new Vector3(.10f,e.actorKind==5?.8f:.25f,.14f),c,.35f,new Vector3(0,1,0),false,3);}
            }
            if(e.kind=="dodge"||e.kind=="jump")Dust(P(e.pos,.06f),.6f,e.kind=="dodge"?6:4);
            if(e.kind=="heal"){SpawnNumber(e.pos,"+"+Mathf.RoundToInt(e.amount),new Color(.63f,1,.64f),false,false);Pulse(P(e.pos,.06f),.7f,new Color(.55f,.92f,.61f,.42f),.5f);}
            if(e.kind=="place"||e.kind=="recall"||e.kind=="toggle"||e.kind=="burst")Pulse(P(e.pos,.05f),e.kind=="burst"?2.4f:.65f,new Color(1,.79f,.43f,.4f),.35f);
        }
        void ResetBlade(){bladePoints.Clear();if(bladeObject)bladeObject.SetActive(false);}
        void RenderBlade(Run s,float phase,float dt)
        {
            bladeClock+=dt;if(!bladeObject||hero==null||!hero.weapon)return;
            if(s.weapon==0&&phase>=.32f&&phase<.70f){
                var a=hero.weapon.TransformPoint(new Vector3(.08f,.26f,0));var b=hero.weapon.TransformPoint(new Vector3(.22f,1.02f,0));
                if(bladePoints.Count==0||(b-bladePoints[bladePoints.Count-1].tip).sqrMagnitude>.0001f){bladePoints.Add(new BladePoint{root=a,tip=b,time=bladeClock});if(bladePoints.Count>10)bladePoints.RemoveAt(0);}
            }
            while(bladePoints.Count>0&&bladeClock-bladePoints[0].time>.13f)bladePoints.RemoveAt(0);
            bladeObject.SetActive(bladePoints.Count>=2);if(bladePoints.Count<2)return;
            for(int i=0;i<10;i++){var p=bladePoints[Mathf.Min(i,bladePoints.Count-1)];bladeVertices[i*2]=p.root;bladeVertices[i*2+1]=p.tip;float a=i>=bladePoints.Count?0:Mathf.Clamp01(1-(bladeClock-p.time)/.13f)*.8f;bladeColors[i*2]=new Color(1,.65f,.24f,a*.12f);bladeColors[i*2+1]=new Color(1,.94f,.73f,a);}
            bladeMesh.vertices=bladeVertices;bladeMesh.colors=bladeColors;bladeMesh.RecalculateBounds();
        }
        void RenderDangers(Run s)
        {
            for(int i=0;i<s.dangers.Count;i++){
                if(i>=dangerMarkers.Count){var root=Models.Root("Timed attack boundary",fx);dangerMarkers.Add(new DangerMarker{root=root,fill=Effect("Quiet danger fill",disc,Color.white,root.transform),border=Effect("Exact hit boundary",ring,Color.white,root.transform),timer=Effect("Closing warning timer",ring,Color.white,root.transform)});}
                var d=dangerMarkers[i];var h=s.dangers[i];d.root.SetActive(true);d.root.transform.position=P(h.pos,.055f);d.root.transform.localScale=h.kind==0?new Vector3(h.radius,1,h.radius):new Vector3(h.width,1,h.depth);
                d.fill.GetComponent<MeshFilter>().sharedMesh=h.kind==0?disc:rectangle;d.border.GetComponent<MeshFilter>().sharedMesh=h.kind==0?ring:rectangleBorder;d.timer.GetComponent<MeshFilter>().sharedMesh=h.kind==0?ring:rectangleBorder;
                float progress=Mathf.Clamp01(h.age/h.wait);float fade=h.hit?Mathf.Clamp01(1-(h.age-h.wait)/.4f):1;
                TintFX(d.fill.GetComponent<Renderer>(),h.hit?new Color(1,.68f,.35f,.28f*fade):new Color(.9f,.24f,.14f,.05f+progress*.1f));TintFX(d.border.GetComponent<Renderer>(),new Color(1,.36f,.18f,.7f*fade));
                d.border.transform.localPosition=Vector3.up*.004f;d.timer.transform.localPosition=Vector3.up*.008f;d.timer.transform.localScale=Vector3.one*Mathf.Lerp(.12f,1,progress);TintFX(d.timer.GetComponent<Renderer>(),new Color(1,.76f,.41f,(h.hit?0:.35f+progress*.45f)));
            }
            for(int i=s.dangers.Count;i<dangerMarkers.Count;i++)dangerMarkers[i].root.SetActive(false);
        }
        void RenderBolts(Run s,float dt)
        {
            boltDustClock+=dt;bool tail=boltDustClock>.035f;if(tail)boltDustClock=0;
            for(int i=0;i<s.bolts.Count;i++){
                if(i>=boltModels.Count)boltModels.Add(Effect("Pointed star projectile",diamond,Cream));var b=s.bolts[i];var o=boltModels[i];o.SetActive(true);o.transform.position=P(b.pos,1.15f);
                o.transform.rotation=Quaternion.LookRotation(P(b.velocity))*Quaternion.Euler(0,0,visualClock*170);o.transform.localScale=new Vector3(b.empowered?.27f:.17f,b.empowered?.27f:.17f,b.empowered?.74f:.48f);TintFX(o.GetComponent<Renderer>(),b.empowered?Magic:Cream);
                if(tail&&i<8)Particle(flareMesh,P(b.pos,1.15f),Vector3.one*(b.empowered?.13f:.08f),new Color(.5f,.92f,.88f,.5f),.14f,Vector3.zero,true);
            }
            for(int i=s.bolts.Count;i<boltModels.Count;i++)boltModels[i].SetActive(false);
        }
        void SetupAtmosphere(int next)
        {
            foreach(var m in motes)Release(m);motes.Clear();moteOrigins.Clear();
            for(int i=0;i<26;i++){var at=new Vector3(Random(-14,14),Random(.3f,5.5f),Random(4,9));var color=next==1?new Color(.71f,.86f,.46f,.35f):next==0?new Color(.66f,.8f,.84f,.09f):new Color(.85f,.71f,.45f,.12f);var g=Effect("Ambient drifting mote",next==0?streakMesh:flareMesh,color);g.transform.position=at;g.transform.rotation=Quaternion.LookRotation(cam.transform.forward)*Quaternion.Euler(0,0,next==0?72:0);g.transform.localScale=next==0?new Vector3(.16f,.012f,1):Vector3.one*Random(.022f,.047f);motes.Add(g);moteOrigins.Add(at);}
        }
        void TickAtmosphere(float dt)
        {
            for(int i=0;i<motes.Count;i++){var at=moteOrigins[i];float t=visualClock*(area==0?1.8f:.35f);at.x+=Mathf.Sin(t+i)*.35f;at.y=area==0?Mathf.Repeat(at.y-t,5.5f):at.y+Mathf.Sin(t+i*1.7f)*.22f;motes[i].transform.position=at;}
        }
    }
    public sealed class FloatingNumber:MonoBehaviour
    {
        TextMeshPro text;Color color;Vector3 velocity;float life=.62f,age;bool strong;
        public void Configure(TextMeshPro label,Vector3 drift,bool emphasis){text=label;color=label.color;velocity=drift;strong=emphasis;}
        void Update(){if(Game.Instance&&Game.Instance.Paused)return;float dt=Time.unscaledDeltaTime;age+=dt;if(age>life){Destroy(gameObject);return;}transform.position+=(Vector3.up*(.9f-age*.6f)+velocity)*dt;float pop=1+(strong?.42f:.22f)*Mathf.Sin(Mathf.Clamp01(age/.18f)*Mathf.PI);transform.localScale=Vector3.one*.105f*pop;if(text){var c=color;c.a*=1-Mathf.InverseLerp(.37f,life,age);text.color=c;}}
    }
}
