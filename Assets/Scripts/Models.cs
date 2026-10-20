using System.Collections.Generic;
using UnityEngine;

namespace Mist
{
    // Original, reproducible model construction. The editor exports every mesh and material.
    public static class Models
    {
        public static readonly Color Teal=Hex("397A81"), Gold=Hex("C79A51"), Ember=Hex("FFD293"), Red=Hex("AD5449"), Dark=Hex("142937"), Bone=Hex("D8D4BB");
        static readonly Dictionary<string,Material> materials=new Dictionary<string,Material>();
        static Mesh bevel;
        static Mesh effectSphere,effectCylinder;
        public static Color Hex(string s){Color c;ColorUtility.TryParseHtmlString("#"+s,out c);return c;}
        public static Material Mat(Color color,bool glow=false)
        {
            string key=ColorUtility.ToHtmlStringRGBA(color)+(glow?"g":"");
            Material m;
            if(materials.TryGetValue(key,out m)&&m)return m;
            m=new Material(Shader.Find("Standard")){name="Mist_"+key,color=color};
            m.SetFloat("_Glossiness",.16f);m.SetFloat("_Metallic",0);
            if(glow){m.EnableKeyword("_EMISSION");m.SetColor("_EmissionColor",color*1.4f);}
            materials[key]=m;return m;
        }
        public static GameObject Root(string name,Transform parent=null,Vector3 pos=default(Vector3))
        {var g=new GameObject(name);g.transform.SetParent(parent,false);g.transform.localPosition=pos;return g;}
        public static GameObject MeshPart(string name,Transform parent,Mesh mesh,Vector3 pos,Vector3 scale,Color col,bool glow=false)
        {
            var o=Root(name,parent,pos);o.transform.localScale=scale;
            o.AddComponent<MeshFilter>().sharedMesh=mesh;o.AddComponent<MeshRenderer>().sharedMaterial=Mat(col,glow);return o;
        }
        public static GameObject Primitive(string name,Transform parent,PrimitiveType type,Vector3 pos,Vector3 scale,Color col,bool glow=false)
        {
            if(Application.isPlaying)
            {
                // Visual effects never require Physics components. CreatePrimitive would ask
                // IL2CPP for unused collider types that can legitimately be stripped.
                Mesh mesh;
                if(type==PrimitiveType.Sphere)
                {
                    if(!effectSphere)
                    {
                        var rings=new Vector2[13];
                        for(int i=0;i<13;i++){float a=i*Mathf.PI/12;rings[i]=new Vector2(Mathf.Sin(a)*.5f,-Mathf.Cos(a)*.5f);}
                        effectSphere=Lathe(rings,16);
                    }
                    mesh=effectSphere;
                }
                else if(type==PrimitiveType.Cylinder)
                {if(!effectCylinder)effectCylinder=Lathe(new[]{new Vector2(.5f,-1),new Vector2(.5f,1)},32);mesh=effectCylinder;}
                else mesh=BevelCube();
                return MeshPart(name,parent,mesh,pos,scale,col,glow);
            }
            var g=GameObject.CreatePrimitive(type);g.name=name;g.transform.SetParent(parent,false);g.transform.localPosition=pos;g.transform.localScale=scale;
            var c=g.GetComponent<Collider>();if(Application.isPlaying)Object.Destroy(c);else Object.DestroyImmediate(c);
            g.GetComponent<Renderer>().sharedMaterial=Mat(col,glow);return g;
        }
        public static GameObject Orb(string n,Transform p,Vector3 v,Vector3 s,Color c,bool glow=false){return Primitive(n,p,PrimitiveType.Sphere,v,s,c,glow);}
        public static GameObject Box(string n,Transform p,Vector3 v,Vector3 s,Color c){return MeshPart(n,p,BevelCube(),v,s,c);}
        public static GameObject Ring(string n,Transform p,Vector3 v,float radius,float thickness,Color c,bool glow=false)
        {return MeshPart(n,p,Torus(radius,thickness),v,Vector3.one,c,glow);}
        public static GameObject Tube(string n,Transform p,Vector3 a,Vector3 b,float r,Color c)
        {
            var o=MeshPart(n,p,Lathe(new[]{new Vector2(r,0),new Vector2(r,(b-a).magnitude)},10),a,Vector3.one,c);
            o.transform.localRotation=Quaternion.FromToRotation(Vector3.up,b-a);return o;
        }
        public static Mesh Lathe(Vector2[] rings,int sides=16)
        {
            var verts=new List<Vector3>();var tris=new List<int>();
            for(int j=0;j<rings.Length;j++)for(int i=0;i<sides;i++)
            {float a=i*Mathf.PI*2/sides;verts.Add(new Vector3(Mathf.Cos(a)*rings[j].x,rings[j].y,Mathf.Sin(a)*rings[j].x));}
            for(int j=0;j<rings.Length-1;j++)for(int i=0;i<sides;i++)
            {int a=j*sides+i,b=j*sides+(i+1)%sides,c=a+sides,d=b+sides;tris.AddRange(new[]{a,c,b,b,c,d});}
            int low=verts.Count;verts.Add(new Vector3(0,rings[0].y,0));int high=verts.Count;verts.Add(new Vector3(0,rings[rings.Length-1].y,0));
            for(int i=0;i<sides;i++){tris.AddRange(new[]{low,i,(i+1)%sides,high,(rings.Length-1)*sides+(i+1)%sides,(rings.Length-1)*sides+i});}
            return Finish("Lathed sculpt",verts,tris);
        }
        public static Mesh Torus(float r,float thick,int sides=32)
        {
            var v=new List<Vector3>();var t=new List<int>();
            for(int i=0;i<sides;i++)for(int j=0;j<6;j++)
            {float a=i*2*Mathf.PI/sides,b=j*2*Mathf.PI/6;v.Add(new Vector3((r+thick*Mathf.Cos(b))*Mathf.Cos(a),thick*Mathf.Sin(b),(r+thick*Mathf.Cos(b))*Mathf.Sin(a)));}
            for(int i=0;i<sides;i++)for(int j=0;j<6;j++)
            {int a=i*6+j,b=((i+1)%sides)*6+j,c=i*6+(j+1)%6,d=((i+1)%sides)*6+(j+1)%6;t.AddRange(new[]{a,b,c,b,d,c});}
            return Finish("Copper ring",v,t);
        }
        public static Mesh BevelCube()
        {
            if(bevel)return bevel;
            // Six faces inset at each corner; bevelled edges provide a light-catching silhouette.
            var v=new List<Vector3>();var t=new List<int>();float b=.055f;
            for(int axis=0;axis<3;axis++)for(int sign=-1;sign<=1;sign+=2)
            {
                int start=v.Count;Vector3 n=Vector3.zero;n[axis]=sign*.5f;
                int u=(axis+1)%3,w=(axis+2)%3;
                foreach(var xy in new[]{new Vector2(-1,-1),new Vector2(1,-1),new Vector2(1,1),new Vector2(-1,1)})
                {var p=n;p[u]=xy.x*(.5f-b);p[w]=xy.y*(.5f-b);v.Add(p);}
                if(sign==1)t.AddRange(new[]{start,start+1,start+2,start,start+2,start+3});
                else t.AddRange(new[]{start,start+2,start+1,start,start+3,start+2});
            }
            // Fill the bevel strips and triangular corner facets from their actual face points.
            for(int a=0;a<3;a++)for(int c=a+1;c<3;c++)for(int sa=-1;sa<=1;sa+=2)for(int sc=-1;sc<=1;sc+=2)
            {
                int other=3-a-c,start=v.Count;
                for(int end=-1;end<=1;end+=2)
                {Vector3 p=Vector3.zero;p[a]=sa*.5f;p[c]=sc*(.5f-b);p[other]=end*(.5f-b);v.Add(p);p[a]=sa*(.5f-b);p[c]=sc*.5f;v.Add(p);}
                AddOutward(t,v,start,start+1,start+2);AddOutward(t,v,start+1,start+3,start+2);
            }
            for(int x=-1;x<=1;x+=2)for(int y=-1;y<=1;y+=2)for(int z=-1;z<=1;z+=2)
            {int q=v.Count;v.Add(new Vector3(x*.5f,y*(.5f-b),z*(.5f-b)));v.Add(new Vector3(x*(.5f-b),y*.5f,z*(.5f-b)));v.Add(new Vector3(x*(.5f-b),y*(.5f-b),z*.5f));AddOutward(t,v,q,q+1,q+2);}
            bevel=Finish("Bevelled block",v,t);return bevel;
        }
        static void AddOutward(List<int> t,List<Vector3> v,int a,int b,int c)
        {if(Vector3.Dot(Vector3.Cross(v[b]-v[a],v[c]-v[a]),v[a]+v[b]+v[c])<0)t.AddRange(new[]{a,c,b});else t.AddRange(new[]{a,b,c});}
        static Mesh Finish(string name,List<Vector3> v,List<int> t)
        {
            // Split normals by face for crafted, faceted surfaces.
            var flat=new Vector3[t.Count];var indices=new int[t.Count];
            for(int i=0;i<t.Count;i++){flat[i]=v[t[i]];indices[i]=i;}
            var m=new Mesh{name=name};m.vertices=flat;m.triangles=indices;m.RecalculateNormals();m.RecalculateBounds();return m;
        }
        public static GameObject Weapon(Transform parent,int type)
        {
            var r=Root("Weapon",parent);
            Tube("Wrapped grip",r.transform,Vector3.zero,new Vector3(0,.42f,0),.07f,Dark);
            Ring("Pommel",r.transform,Vector3.zero,.1f,.03f,Gold);
            if(type==0)
            {
                Box("Crossguard",r.transform,new Vector3(0,.38f,0),new Vector3(.48f,.1f,.13f),Gold);
                var blade=MeshPart("Curved blade",r.transform,Lathe(new[]{new Vector2(.11f,0),new Vector2(.15f,.35f),new Vector2(.11f,.8f),new Vector2(0,1.12f)},4),new Vector3(0,.43f,0),new Vector3(.6f,1,.25f),Bone);
                blade.transform.localEulerAngles=new Vector3(0,0,-9);
                Tube("Blade spine",r.transform,new Vector3(-.03f,.45f,0),new Vector3(.1f,1.33f,0),.022f,Ember);
            }
            else if(type==1)
            {
                Tube("Hammer haft",r.transform,new Vector3(0,.3f,0),new Vector3(0,1.3f,0),.085f,Gold);
                Box("Cast bell head",r.transform,new Vector3(0,1.2f,0),new Vector3(.82f,.62f,.58f),Gold);
                Box("Hammer inlay",r.transform,new Vector3(0,1.21f,-.3f),new Vector3(.43f,.38f,.06f),Teal);
                Orb("Hammer gem",r.transform,new Vector3(0,1.21f,-.35f),new Vector3(.18f,.18f,.1f),Ember,true);
            }
            else
            {
                Tube("Staff",r.transform,Vector3.zero,new Vector3(0,1.75f,0),.065f,Gold);
                var ring=Ring("Orbit",r.transform,new Vector3(0,1.45f,0),.3f,.045f,Gold);ring.transform.localEulerAngles=new Vector3(90,0,0);
                Orb("Star",r.transform,new Vector3(0,1.45f,0),Vector3.one*.26f,Ember,true);
                Orb("Orbit gem",r.transform,new Vector3(.3f,1.6f,0),Vector3.one*.1f,Teal,true);
            }
            return r;
        }
        public static GameObject Lantern(Transform parent,Vector3 pos,float scale=1)
        {
            var r=Root("Lantern",parent,pos);r.transform.localScale=Vector3.one*scale;
            MeshPart("Roof",r.transform,Lathe(new[]{new Vector2(.27f,.45f),new Vector2(.1f,.62f),new Vector2(.05f,.7f)},6),Vector3.zero,Vector3.one,Gold);
            MeshPart("Base",r.transform,Lathe(new[]{new Vector2(.1f,0),new Vector2(.25f,.08f),new Vector2(.24f,.14f)},6),Vector3.zero,Vector3.one,Gold);
            Orb("Flame",r.transform,new Vector3(0,.3f,0),new Vector3(.23f,.38f,.23f),Ember,true);
            for(int i=0;i<6;i++){float a=i*Mathf.PI/3;Tube("Cage",r.transform,new Vector3(Mathf.Cos(a)*.2f,.13f,Mathf.Sin(a)*.2f),new Vector3(Mathf.Cos(a)*.2f,.46f,Mathf.Sin(a)*.2f),.022f,Gold);}
            var ring=Ring("Handle",r.transform,new Vector3(0,.78f,0),.1f,.022f,Gold);ring.transform.localEulerAngles=new Vector3(90,0,0);
            return r;
        }
        public static GameObject Character(int kind=-1,int weapon=0)
        {
            var r=Root(kind<0?"Hunter":Catalog.EnemyNames[Mathf.Min(kind,6)]);
            var rig=Root("Rig",r.transform).transform;
            Color cloth=kind<0?Teal:kind==0?Hex("546A87"):kind==1?Gold:kind==2?Hex("645980"):kind==3?Hex("576475"):kind==5?Hex("44736B"):Hex("657C88");
            float size=kind>=4?1.8f:kind==3?.8f:1;rig.localScale=Vector3.one*size;
            if(kind==3)
            {
                Orb("Hound body",rig,new Vector3(0,.7f,.2f),new Vector3(.8f,.7f,1.4f),cloth);
                Orb("Hound head",rig,new Vector3(0,1,-.6f),new Vector3(.8f,.75f,.8f),Bone);
                for(int i=0;i<4;i++){var leg=Root("Leg"+i,rig,new Vector3(i%2==0?-.28f:.28f,.6f,i<2?-.4f:.5f));Tube("Limb",leg.transform,Vector3.zero,new Vector3(0,-.5f,0),.12f,cloth);}
                for(int i=-1;i<=1;i+=2){Orb("Eye",rig,new Vector3(i*.22f,1.1f,-.94f),new Vector3(.12f,.14f,.08f),Ember,true);MeshPart("Horn",rig,Lathe(new[]{new Vector2(.15f,0),new Vector2(0,.55f)},8),new Vector3(i*.3f,1.2f,-.45f),Vector3.one,Gold);}
                return r;
            }
            MeshPart("Tailored cloak",rig,Lathe(new[]{new Vector2(.58f,.55f),new Vector2(.49f,.8f),new Vector2(.35f,1.4f),new Vector2(.2f,1.55f)},12),Vector3.zero,new Vector3(1,1,.78f),cloth);
            for(int i=-1;i<=1;i+=2)
            {
                var leg=Root(i<0?"LegL":"LegR",rig,new Vector3(i*.21f,.6f,0)).transform;
                Tube("Shin",leg,Vector3.zero,new Vector3(0,-.34f,0),.13f,Dark);
                Box("Leather boot",leg,new Vector3(0,-.42f,-.08f),new Vector3(.33f,.25f,.5f),Hex("514645"));
                Box("Boot clasp",leg,new Vector3(0,-.29f,-.22f),new Vector3(.15f,.07f,.05f),Gold);
                var arm=Root(i<0?"ArmL":"ArmR",rig,new Vector3(i*.45f,1.25f,0)).transform;
                Orb("Sleeve",arm,new Vector3(0,-.2f,0),new Vector3(.35f,.55f,.37f),cloth);
                Orb("Glove",arm,new Vector3(0,-.52f,-.08f),new Vector3(.25f,.28f,.27f),Hex("564B48"));
                Ring("Cuff",arm,new Vector3(0,-.4f,0),.14f,.04f,Gold);
            }
            var belt=Ring("Belt",rig,new Vector3(0,.98f,0),.38f,.07f,Hex("51403A"));belt.transform.localScale=new Vector3(1,1,.8f);
            Box("Buckle",rig,new Vector3(0,.98f,-.33f),new Vector3(.19f,.16f,.08f),Gold);
            var head=Root("Head",rig,new Vector3(0,1.7f,0)).transform;
            MeshPart("Pointed hood",head,Lathe(new[]{new Vector2(.42f,-.28f),new Vector2(.64f,0),new Vector2(.62f,.35f),new Vector2(.42f,.62f),new Vector2(.04f,.89f)},16),Vector3.zero,new Vector3(1,1,.85f),cloth);
            Orb("Face recess",head,new Vector3(0,.1f,-.42f),new Vector3(.85f,.87f,.27f),Dark);
            if(kind>=0)
            {
                Orb("Porcelain mask",head,new Vector3(0,.12f,-.52f),new Vector3(.72f,.82f,.17f),Bone);
                for(int i=-1;i<=1;i+=2)
                {
                    Orb("Eye socket",head,new Vector3(i*.19f,.2f,-.61f),new Vector3(.23f,.25f,.08f),Dark);
                    Orb("Mist eye",head,new Vector3(i*.19f,.2f,-.66f),new Vector3(.095f,.12f,.05f),kind>=4?Ember:Teal,true);
                    var horn=MeshPart("Antler",head,Lathe(new[]{new Vector2(.12f,0),new Vector2(.1f,.25f),new Vector2(.025f,.6f)},8),new Vector3(i*.44f,.5f,.05f),Vector3.one,Gold);
                    horn.transform.localEulerAngles=new Vector3(-15,0,-i*24);
                }
                Tube("Mask crack",head,new Vector3(.07f,-.12f,-.62f),new Vector3(.16f,-.28f,-.6f),.014f,Dark);
            }
            else
            {
                for(int i=-1;i<=1;i+=2)Orb("Amber eye",head,new Vector3(i*.17f,.11f,-.56f),new Vector3(.09f,.13f,.045f),Ember,true);
                var scarf=Ring("Scarf",rig,new Vector3(0,1.36f,0),.44f,.12f,Red);scarf.transform.localScale=new Vector3(1,.8f,.85f);
                var tail=Box("ScarfTail",rig,new Vector3(.44f,1.05f,-.32f),new Vector3(.28f,.78f,.07f),Red);tail.transform.localEulerAngles=new Vector3(-20,0,28);
                for(int i=0;i<5;i++)Orb("Hood stitch",head,new Vector3(.45f-i*.025f,.3f-i*.1f,-.39f-i*.015f),Vector3.one*.035f,Gold);
            }
            var right=rig.Find("ArmR");var left=rig.Find("ArmL");
            var w=Weapon(right,kind==1||kind==4?1:kind==2||kind==5||kind==6?2:weapon);
            w.transform.localPosition=new Vector3(0,-.5f,-.12f);w.transform.localEulerAngles=new Vector3(-12,0,-28);
            if(kind<0)Lantern(left,new Vector3(0,-1.18f,-.12f),.7f);
            else if(kind==1)
            {
                Box("Shield",left,new Vector3(0,-.4f,-.3f),new Vector3(.63f,.85f,.16f),Gold);
                Box("Shield enamel",left,new Vector3(0,-.4f,-.4f),new Vector3(.42f,.6f,.05f),Teal);
            }
            if(kind>=4)
            {
                var halo=Ring("Boss halo",rig,new Vector3(0,2.5f,.3f),.85f,.07f,Gold);halo.transform.localEulerAngles=new Vector3(80,0,0);
                for(int i=0;i<12;i++){float a=i*Mathf.PI/6;var tooth=Box("Crown tooth",rig,new Vector3(Mathf.Cos(a)*.83f,2.5f+Mathf.Sin(a)*.83f,.32f),new Vector3(.13f,.24f,.16f),Gold);tooth.transform.localEulerAngles=new Vector3(0,0,i*30);}
            }
            if(kind==4)
            {
                Box("Cast breastplate",rig,new Vector3(0,1.15f,-.38f),new Vector3(.72f,.64f,.18f),Gold);
                for(int i=-1;i<=1;i+=2){Orb("Bell pauldron",rig,new Vector3(i*.57f,1.42f,0),new Vector3(.65f,.6f,.67f),Gold);Box("Helmet crest",head,new Vector3(i*.3f,.45f,-.4f),new Vector3(.18f,.55f,.18f),Gold);}
                Orb("Breastplate core",rig,new Vector3(0,1.2f,-.5f),Vector3.one*.23f,Ember,true);
            }
            if(kind==5)
            {
                for(int i=0;i<8;i++)
                {float a=i*Mathf.PI/4;Vector3 tip=new Vector3(Mathf.Cos(a)*.95f,2.7f+Mathf.Abs(Mathf.Sin(a))*.5f,Mathf.Sin(a)*.5f);Tube("Living crown",rig,new Vector3(0,2.2f,0),tip,.055f,Gold);Orb("Crown leaf",rig,tip,new Vector3(.35f,.55f,.17f),i%2==0?Teal:Hex("8C9B63"));}
                for(int i=0;i<5;i++)Tube("Root hem",rig,new Vector3((i-2)*.2f,.65f,0),new Vector3((i-2)*.3f,.05f,-.3f),.065f,Gold);
            }
            if(kind==6)
            {
                var clock=Root("Chest clock",rig,new Vector3(0,1.1f,-.48f)).transform;
                var rim=Ring("Dial",clock,Vector3.zero,.4f,.06f,Ember,true);rim.transform.localEulerAngles=new Vector3(90,0,0);
                Tube("Hand",clock,new Vector3(0,0,-.05f),new Vector3(.22f,.21f,-.05f),.035f,Gold);
                Tube("Hand",clock,new Vector3(0,0,-.06f),new Vector3(-.12f,.13f,-.06f),.04f,Gold);
                for(int i=0;i<4;i++)Box("Floating clock shard",rig,new Vector3((i%2==0?-1:1)*.85f,.8f+i*.42f,.1f),new Vector3(.2f,.4f,.15f),Gold);
            }
            return r;
        }
        public static void Arch(Transform parent,Vector3 pos,float width,float height,Color col)
        {
            var r=Root("Stone arch",parent,pos).transform;float radius=width*.5f;
            for(int side=-1;side<=1;side+=2)
            {
                Box("Pillar",r,new Vector3(side*radius,height*.36f,0),new Vector3(.65f,height*.72f,.9f),col);
                Box("Foot",r,new Vector3(side*radius,.15f,0),new Vector3(.9f,.3f,1.15f),col);
                Box("Capital",r,new Vector3(side*radius,height*.72f,0),new Vector3(.9f,.3f,1.05f),col);
            }
            for(int i=0;i<11;i++)
            {
                float a=i*Mathf.PI/10;var block=Box("Arch voussoir",r,new Vector3(Mathf.Cos(a)*radius,height*.72f+Mathf.Sin(a)*radius*.7f,0),new Vector3(width*.18f,.6f,.9f),col);
                block.transform.localEulerAngles=new Vector3(0,0,a*Mathf.Rad2Deg+90);
            }
        }
        public static GameObject Stage(int area)
        {
            var root=Root("Environment");var r=root.transform;
            Color stone=area==0?Hex("465665"):area==1?Hex("426463"):Hex("505365");
            Box("Floating foundation",r,new Vector3(0,-.52f,.4f),new Vector3(23,1,10.5f),Dark);
            var random=new System.Random(900+area);
            for(int x=-11;x<=10;x++)for(int z=-4;z<=4;z++)
            {
                float light=.82f+random.Next(4)*.06f;
                var tile=Box("Paving stone",r,new Vector3(x+.48f+(z%2==0?.17f:0),-.04f,z+.3f),new Vector3(.98f,.2f,.98f),stone*light);
                tile.transform.localEulerAngles=new Vector3(0,(float)random.NextDouble()*2-1,0);
            }
            Ring("Old ward outer ring",r,new Vector3(0,.075f,0),2.65f,.028f,Gold*.65f);
            Ring("Old ward inner ring",r,new Vector3(0,.076f,0),2.45f,.018f,Gold*.65f);
            for(int i=0;i<8;i++){float a=i*Mathf.PI/4;var mark=Box("Ward ray",r,new Vector3(Mathf.Cos(a)*2.55f,.08f,Mathf.Sin(a)*2.55f),new Vector3(.2f,.015f,.07f),Gold*.8f);mark.transform.localEulerAngles=new Vector3(0,-i*45,0);}
            for(int i=-5;i<=5;i++)
            {
                Box("Rear parapet",r,new Vector3(i*2.15f,.6f,5),new Vector3(2,.95f,.6f),stone*.8f);
                Box("Parapet cap",r,new Vector3(i*2.15f,1.13f,5),new Vector3(2.1f,.15f,.8f),stone*1.12f);
                if(i%2==0)
                {
                    Box("Lamp pedestal",r,new Vector3(i*2.15f,1.4f,5),new Vector3(.6f,2.4f,.6f),stone);
                    Lantern(r,new Vector3(i*2.15f,2.65f,5),1.2f);
                    var light=Root("Lantern glow",r,new Vector3(i*2.15f,2.9f,4)).AddComponent<Light>();light.type=LightType.Point;light.color=Ember;light.range=5;light.intensity=1.4f;light.shadows=LightShadows.None;
                }
            }
            Arch(r,new Vector3(10.5f,0,2),3.5f,3.3f,stone);
            Arch(r,new Vector3(-10.7f,0,2),3.5f,3.3f,stone*.8f);
            for(int i=0;i<15;i++)
            {
                float x=(i-7)*3.6f,z=14+(i%3)*3,height=3+(float)random.NextDouble()*5;
                Color distant=Color.Lerp(stone,Hex("203F50"),.5f);
                Box("Distant tower",r,new Vector3(x,height/2,z),new Vector3(2.8f,height,2.8f),distant*.7f);
                MeshPart("Spire",r,Lathe(new[]{new Vector2(2,0),new Vector2(0,3)},6),new Vector3(x,height,z),Vector3.one,distant*.5f);
                for(int floor=1;floor<height;floor+=2)Box("Lit window",r,new Vector3(x,height-floor,z-1.42f),new Vector3(.25f,.7f,.04f),Gold*.7f);
            }
            if(area==1)
            {
                for(int i=0;i<6;i++)
                {
                    var tree=Root("Copperleaf tree",r,new Vector3((i-2.5f)*4,0,7+(i%2))).transform;
                    Tube("Trunk",tree,Vector3.zero,new Vector3(.3f,4,0),.27f,Hex("5B5D54"));
                    for(int j=0;j<4;j++){float a=j*2.1f;Vector3 v=new Vector3(Mathf.Cos(a)*1.3f,2.8f+j*.6f,Mathf.Sin(a));Tube("Branch",tree,new Vector3(.1f,2,0),v,.11f,Gold*.7f);Orb("Canopy",tree,v,new Vector3(2.7f,1.4f,2.2f),j%2==0?Hex("466F72"):Hex("799778"));}
                }
            }
            if(area==2)
            {
                var clock=Root("Great clock",r,new Vector3(0,5.5f,7)).transform;
                var disc=Primitive("Clock face",clock,PrimitiveType.Cylinder,Vector3.zero,new Vector3(6,.12f,6),Dark);disc.transform.localEulerAngles=new Vector3(90,0,0);
                var ring=Ring("Clock rim",clock,new Vector3(0,0,-.15f),3,.16f,Gold);ring.transform.localEulerAngles=new Vector3(90,0,0);
                for(int i=0;i<12;i++){float a=i*Mathf.PI/6;var mark=Box("Hour",clock,new Vector3(Mathf.Sin(a)*2.5f,Mathf.Cos(a)*2.5f,-.3f),new Vector3(.15f,.38f,.08f),Ember);mark.transform.localEulerAngles=new Vector3(0,0,-i*30);}
                Tube("Minute hand",clock,new Vector3(0,0,-.4f),new Vector3(-1.7f,1.2f,-.4f),.06f,Gold);
                Tube("Hour hand",clock,new Vector3(0,0,-.4f),new Vector3(.5f,1.2f,-.4f),.1f,Gold);
            }
            else Arch(r,new Vector3(0,1,9),7,6,stone*.65f);
            for(int i=0;i<8;i++)
            {
                float x=(i-3.5f)*2.7f,z=4.2f;
                if(i%3==0)
                {
                    var barrel=Root("Abandoned supply barrel",r,new Vector3(x,.05f,z)).transform;
                    MeshPart("Staves",barrel,Lathe(new[]{new Vector2(.3f,0),new Vector2(.39f,.35f),new Vector2(.3f,.8f)},12),Vector3.zero,Vector3.one,Hex("685957"));
                    Ring("Barrel hoop",barrel,new Vector3(0,.16f,0),.34f,.035f,Gold*.7f);Ring("Barrel hoop",barrel,new Vector3(0,.63f,0),.34f,.035f,Gold*.7f);
                    Box("Crate",r,new Vector3(x+.63f,.3f,z),new Vector3(.58f,.6f,.6f),Hex("637272"));
                }
                else
                {
                    for(int j=0;j<4;j++){var stem=Tube("Wild reed",r,new Vector3(x+j*.12f,.05f,z),new Vector3(x+j*.12f-.12f,.45f+(j%2)*.2f,z),.02f,Teal);Orb("Seed pod",r,stem.transform.localPosition+new Vector3(-.12f,.5f,0),new Vector3(.08f,.2f,.08f),Gold*.7f);}
                }
            }
            for(int i=0;i<9;i++)
            {
                float x=(i-4)*2.5f;
                var cap=Box("Front edge block",r,new Vector3(x,-.05f,-4.9f),new Vector3(2.3f,.45f,.6f),stone*.85f);
                if(i==0||i==8){Lantern(r,new Vector3(x,.2f,-4.9f),1.1f);}
            }
            return root;
        }
    }
}
