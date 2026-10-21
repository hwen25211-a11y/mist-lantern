using System;
using System.Collections.Generic;
using UnityEngine;
using Object=UnityEngine.Object;

namespace Mist
{
    // Authored silhouettes, lofted cloth, carved stone and articulated creatures.
    // Every mesh and material is exported by BuildGame; no imported game assets.
    public static class Art
    {
        public static void Optimize(GameObject root)
        {
            // Keep the authoring parts, but render each rigid joint with one coloured mesh.
            var anchors=new HashSet<Transform>();
            foreach(var t in root.GetComponentsInChildren<Transform>())
                if(t.name=="Rig"||t.name=="Body"||t.name=="Head"||t.name.StartsWith("Arm")||t.name.StartsWith("Leg")||t.name.StartsWith("Wing")||t.name=="ScarfTail"||t.name=="Lantern")anchors.Add(t);
            var groups=new Dictionary<Transform,List<MeshFilter>>();
            foreach(var mf in root.GetComponentsInChildren<MeshFilter>())
            {
                var renderer=mf.GetComponent<MeshRenderer>();if(!renderer||renderer.sharedMaterial.GetColor("_EmissionColor").maxColorComponent>.01f)continue;
                var anchor=mf.transform.parent;while(anchor&&!anchors.Contains(anchor))anchor=anchor.parent;if(!anchor)continue;
                if(!groups.ContainsKey(anchor))groups[anchor]=new List<MeshFilter>();groups[anchor].Add(mf);
            }
            foreach(var entry in groups)
            {
                var vertices=new List<Vector3>();var normals=new List<Vector3>();var colors=new List<Color>();var triangles=new List<int>();
                foreach(var mf in entry.Value)
                {
                    int offset=vertices.Count;var matrix=entry.Key.worldToLocalMatrix*mf.transform.localToWorldMatrix;var mesh=mf.sharedMesh;var vv=mesh.vertices;var nn=mesh.normals;
                    Color color=mf.GetComponent<Renderer>().sharedMaterial.color;
                    for(int i=0;i<vv.Length;i++){vertices.Add(matrix.MultiplyPoint3x4(vv[i]));normals.Add(matrix.inverse.transpose.MultiplyVector(nn[i]).normalized);colors.Add(color);}
                    foreach(int i in mesh.triangles)triangles.Add(offset+i);mf.GetComponent<Renderer>().enabled=false;
                }
                var merged=new Mesh{name=entry.Key.name+" coloured sculpture"};merged.SetVertices(vertices);merged.SetNormals(normals);merged.SetColors(colors);merged.SetTriangles(triangles,0);merged.RecalculateBounds();
                Models.MeshPart("Joint render",entry.Key,merged,Vector3.zero,Vector3.one,Color.white);
            }
        }
        static Color C(string c){return Models.Hex(c);}
        static readonly Color ink=C("172531"),copper=C("B58450"),edge=C("E1BA77"),leather=C("60463D"),cloth=C("2F7776"),pale=C("D7D4B4"),skin=C("D4A47F");
        static Transform Root(string n,Transform p,Vector3 v){return Models.Root(n,p,v).transform;}
        static GameObject B(string n,Transform p,Vector3 v,Vector3 s,Color c){return Models.Box(n,p,v,s,c);}
        static GameObject O(string n,Transform p,Vector3 v,Vector3 s,Color c){return Models.Orb(n,p,v,s,c);}
        static GameObject T(string n,Transform p,Vector3 a,Vector3 b,float r,Color c){return Models.Tube(n,p,a,b,r,c);}
        static Mesh Finish(string n,List<Vector3> v,List<int> t,bool smooth=true)
        {
            var mesh=new Mesh{name=n};mesh.SetVertices(v);mesh.SetTriangles(t,0);mesh.RecalculateNormals();mesh.RecalculateBounds();
            var uv=new Vector2[v.Count];for(int i=0;i<v.Count;i++)uv[i]=new Vector2(v[i].x*.5f+.5f,v[i].y*.5f);mesh.uv=uv;return mesh;
        }
        // rings: horizontal radius, elevation, depth radius, horizontal offset.
        public static Mesh Loft(string name,Vector4[] rings,int sides=16)
        {
            var v=new List<Vector3>();var t=new List<int>();
            for(int j=0;j<rings.Length;j++)for(int i=0;i<sides;i++)
            {float a=i*Mathf.PI*2/sides;var r=rings[j];float fold=1+.025f*Mathf.Cos(a*6);v.Add(new Vector3(r.w+Mathf.Cos(a)*r.x*fold,r.y,Mathf.Sin(a)*r.z*fold));}
            for(int j=0;j<rings.Length-1;j++)for(int i=0;i<sides;i++){int a=j*sides+i,b=j*sides+(i+1)%sides,c=a+sides,d=b+sides;t.AddRange(new[]{a,c,b,b,c,d});}
            for(int end=0;end<2;end++){int q=v.Count,j=end==0?0:rings.Length-1;v.Add(new Vector3(rings[j].w,rings[j].y,0));for(int i=0;i<sides;i++){int a=j*sides+i,b=j*sides+(i+1)%sides;if(end==0)t.AddRange(new[]{q,a,b});else t.AddRange(new[]{q,b,a});}}
            return Finish(name,v,t);
        }
        static GameObject L(string n,Transform p,Vector3 v,Color c,params Vector4[] r){return Models.MeshPart(n,p,Loft(n,r),v,Vector3.one,c);}
        static Vector4 R(float x,float y,float z,float off=0){return new Vector4(x,y,z,off);}
        static GameObject Plate(string n,Transform p,Vector3[] points,float depth,Color c)
        {
            var v=new List<Vector3>();var t=new List<int>();
            foreach(var x in points)v.Add(x+Vector3.back*depth*.5f);foreach(var x in points)v.Add(x+Vector3.forward*depth*.5f);
            int m=points.Length;for(int i=1;i<m-1;i++){
                bool front=Vector3.Cross(points[i]-points[0],points[i+1]-points[0]).z<0;
                if(front)t.AddRange(new[]{0,i,i+1,m,m+i+1,m+i});else t.AddRange(new[]{0,i+1,i,m,m+i,m+i+1});}
            for(int i=0;i<m;i++){int j=(i+1)%m;t.AddRange(new[]{i,j,m+i,j,m+j,m+i});}
            return Models.MeshPart(n,p,Finish(n,v,t),Vector3.zero,Vector3.one,c);
        }
        static void Band(Transform p,string n,float y,float radius,Color c,float depth=1)
        {var o=Models.Ring(n,p,new Vector3(0,y,0),radius,.035f,c);o.transform.localScale=new Vector3(1,1,depth);}
        static void Eye(Transform p,float x,float y,float z,Color iris,float size=1)
        {
            O("Eye socket",p,new Vector3(x,y,z),new Vector3(.2f,.22f,.09f)*size,ink);
            O("Iris",p,new Vector3(x+.015f,y-.01f,z-.05f),new Vector3(.1f,.13f,.03f)*size,iris);
            O("Catchlight",p,new Vector3(x+.03f,y+.03f,z-.068f),Vector3.one*.035f*size,pale);
        }
        public static GameObject Character(int kind,int weapon)
        {
            var root=Models.Root(kind<0?"The last lantern keeper":Catalog.EnemyNames[kind]);var rig=Root("Rig",root.transform,Vector3.zero);
            rig.localScale=Vector3.one*(kind<0?.82f:kind>=4?1.15f:.82f);
            if(kind==3){Hound(rig);return root;}
            if(kind==5){Gardener(rig);return root;}
            if(kind==6){ClockLord(rig);return root;}
            bool hero=kind<0,shield=kind==1||kind==4,moth=kind==2,boss=kind==4;
            Color coat=hero?cloth:shield?C("667B7C"):moth?C("575277"):C("798D92");
            if(boss)rig.localScale=Vector3.one*1.2f;
            var body=Root("Body",rig,Vector3.zero);
            L("Tailored coat",body,Vector3.zero,coat,R(.53f,.65f,.32f),R(.48f,.85f,.3f),R(.36f,1.15f,.27f),R(.48f,1.5f,.32f),R(.23f,1.65f,.22f));
            for(int i=-1;i<=1;i+=2)
            {
                var leg=Root(i<0?"LegL":"LegR",rig,new Vector3(i*.22f,.69f,0));
                L("Trouser folds",leg,Vector3.zero,C("273C48"),R(.14f,-.4f,.13f),R(.17f,-.15f,.16f),R(.16f,0,.16f));
                L("Turned leather boot",leg,new Vector3(0,-.56f,-.08f),leather,R(.18f,0,.28f),R(.19f,.1f,.3f),R(.14f,.25f,.16f));
                B("Boot sole",leg,new Vector3(0,-.55f,-.08f),new Vector3(.38f,.07f,.58f),ink);
                B("Boot buckle",leg,new Vector3(0,-.29f,-.19f),new Vector3(.16f,.08f,.05f),edge);
                var arm=Root(i<0?"ArmL":"ArmR",body,new Vector3(i*.48f,1.45f,0));
                L("Sleeve",arm,Vector3.zero,coat,R(.15f,-.42f,.15f),R(.2f,-.18f,.2f),R(.2f,0,.2f));
                Band(arm,"Leather cuff",-.4f,.16f,copper);
                O("Glove palm",arm,new Vector3(0,-.57f,-.06f),new Vector3(.25f,.28f,.28f),leather);
                for(int f=0;f<3;f++)B("Gloved finger",arm,new Vector3(-.085f+f*.08f,-.64f,-.19f),new Vector3(.065f,.12f,.075f),C("86614A"));
                if(hero||shield)
                {
                    O("Forged shoulder plate",arm,new Vector3(i*.03f,-.015f,0),new Vector3(.49f,.3f,.47f),i>0?copper:coat*.85f);
                    for(int rivet=0;rivet<3;rivet++)O("Shoulder rivet",arm,new Vector3(-.15f+rivet*.15f,-.055f,-.22f),Vector3.one*.055f,edge);
                }
            }
            Band(body,"Waist belt",.98f,.38f,leather,.79f);B("Belt clasp",body,new Vector3(.02f,1,-.32f),new Vector3(.2f,.17f,.07f),edge);
            B("Buckle inset",body,new Vector3(.02f,1,-.365f),new Vector3(.11f,.09f,.025f),ink);
            for(int i=0;i<4;i++){float y=1.1f+i*.11f;O("Coat button",body,new Vector3(.08f,y,-.29f),Vector3.one*.045f,edge);}
            for(int i=-1;i<=1;i+=2)T("Coat piping",body,new Vector3(i*.17f,1.6f,-.22f),new Vector3(i*.46f,.7f,-.23f),.022f,edge*.85f);
            var pouch=L("Satchel",body,new Vector3(-.4f,.72f,.06f),leather,R(.16f,0,.12f),R(.2f,.15f,.16f),R(.18f,.32f,.13f));pouch.transform.localEulerAngles=new Vector3(0,0,-12);
            T("Satchel strap",body,new Vector3(-.3f,1.6f,-.27f),new Vector3(.35f,.95f,-.29f),.047f,leather);
            var head=Root("Head",body,new Vector3(0,1.77f,-.015f));
            if(hero)
            {
                L("Sculpted face",head,new Vector3(0,0,-.14f),skin,R(.18f,-.25f,.15f),R(.33f,-.12f,.25f),R(.39f,.2f,.28f),R(.29f,.42f,.23f),R(.1f,.5f,.1f));
                O("Nose",head,new Vector3(0,.02f,-.44f),new Vector3(.13f,.15f,.14f),C("C18C6B"));
                Eye(head,-.16f,.14f,-.385f,C("DDB862"));Eye(head,.16f,.14f,-.385f,C("DDB862"));
                T("Left eyebrow",head,new Vector3(-.28f,.31f,-.37f),new Vector3(-.08f,.27f,-.415f),.037f,ink);
                T("Right eyebrow",head,new Vector3(.08f,.27f,-.415f),new Vector3(.28f,.31f,-.37f),.037f,ink);
                T("Mouth",head,new Vector3(-.08f,-.14f,-.37f),new Vector3(.07f,-.15f,-.38f),.018f,C("785349"));
                L("Asymmetric traveller hood",head,new Vector3(0,.24f,.08f),cloth,R(.46f,-.26f,.34f),R(.51f,.1f,.37f),R(.4f,.43f,.3f,-.04f),R(.22f,.63f,.17f,-.2f),R(.02f,.63f,.03f,-.48f));
                Plate("Hood brow",head,new[]{new Vector3(-.43f,.3f,-.33f),new Vector3(-.23f,.43f,-.43f),new Vector3(.15f,.43f,-.43f),new Vector3(.43f,.29f,-.33f),new Vector3(.31f,.24f,-.39f),new Vector3(-.3f,.28f,-.42f)},.06f,C("408D87"));
                for(int i=0;i<4;i++)T("Stitch",head,new Vector3(-.38f+i*.04f,.38f+i*.045f,-.27f),new Vector3(-.32f+i*.04f,.36f+i*.045f,-.28f),.013f,edge);
                Band(body,"Red scarf collar",1.61f,.3f,C("B34F40"),1.1f);
                var scarf=Root("ScarfTail",body,new Vector3(-.33f,1.55f,.12f));
                Plate("Wind torn scarf",scarf,new[]{new Vector3(0,0,0),new Vector3(-.45f,-.05f,.08f),new Vector3(-.7f,-.31f,.14f),new Vector3(-.38f,-.24f,.1f),new Vector3(-.48f,-.43f,.08f),new Vector3(.05f,-.2f,0)},.05f,C("BC5846"));
                Models.Lantern(body.Find("ArmL"),new Vector3(0,-1.15f,-.07f),.58f);
            }
            else if(shield)
            {
                L("Closed bell helmet",head,Vector3.zero,copper,R(.38f,-.25f,.32f),R(.48f,.05f,.36f),R(.43f,.47f,.33f),R(.16f,.65f,.15f));
                B("Visor slit",head,new Vector3(0,.08f,-.36f),new Vector3(.6f,.115f,.055f),ink);
                for(int i=-1;i<=1;i+=2)B("Amber eye",head,new Vector3(i*.16f,.08f,-.397f),new Vector3(.13f,.048f,.022f),Models.Ember);
                T("Helmet ridge",head,new Vector3(0,-.22f,-.38f),new Vector3(0,.5f,-.31f),.038f,edge);
                Plate("Helmet crest",head,new[]{new Vector3(-.05f,.45f,0),new Vector3(-.12f,1.05f,0),new Vector3(.1f,1.2f,0),new Vector3(.18f,.5f,0)},.2f,C("855253"));
                var sh=Root("Shield",body.Find("ArmL"),new Vector3(-.07f,-.4f,-.27f));
                Plate("Kite shield",sh,new[]{new Vector3(-.45f,.42f,0),new Vector3(0,.6f,0),new Vector3(.42f,.4f,0),new Vector3(.37f,-.28f,0),new Vector3(0,-.65f,0),new Vector3(-.36f,-.28f,0)},.15f,copper);
                Plate("Shield enamel",sh,new[]{new Vector3(-.33f,.34f,-.1f),new Vector3(0,.48f,-.1f),new Vector3(.3f,.32f,-.1f),new Vector3(.25f,-.23f,-.1f),new Vector3(0,-.49f,-.1f),new Vector3(-.24f,-.23f,-.1f)},.03f,C("294D59"));
                O("Shield boss",sh,new Vector3(0,0,-.14f),new Vector3(.31f,.34f,.16f),edge);
                L("Breastplate",body,new Vector3(0,1,-.27f),copper,R(.31f,0,.1f),R(.43f,.35f,.15f),R(.35f,.55f,.1f));
                if(boss){body.localScale=new Vector3(1.3f,1.25f,1.1f);for(int i=-1;i<=1;i+=2)T("Banner spear",body,new Vector3(i*.35f,1,.35f),new Vector3(i*.85f,2.9f,.35f),.055f,copper);}
            }
            else
            {
                L("Cracked porcelain face",head,new Vector3(0,.1f,-.17f),pale,R(.14f,-.38f,.12f),R(.32f,-.13f,.25f),R(.42f,.2f,.3f),R(.31f,.43f,.22f),R(.1f,.5f,.1f));
                Eye(head,-.18f,.18f,-.43f,C("74D3CA"));Eye(head,.18f,.18f,-.43f,C("74D3CA"));
                T("Mask fracture",head,new Vector3(.02f,.4f,-.38f),new Vector3(.1f,.03f,-.45f),.014f,ink);
                T("Mask fracture",head,new Vector3(.1f,.03f,-.45f),new Vector3(-.02f,-.22f,-.36f),.014f,ink);
                if(moth)
                {
                    for(int i=-1;i<=1;i+=2){T("Antenna",head,new Vector3(i*.2f,.4f,0),new Vector3(i*.6f,.95f,0),.035f,copper);
                    var wing=Root(i<0?"WingL":"WingR",body,new Vector3(i*.25f,1.5f,.2f));
                    Plate("Moth wing",wing,new[]{new Vector3(0,0,0),new Vector3(i*.9f,.8f,0),new Vector3(i*1.25f,.4f,0),new Vector3(i*.8f,-.1f,0),new Vector3(i*.95f,-.65f,0),new Vector3(i*.35f,-.4f,0)},.07f,C("7D668E"));
                    O("Wing eye",wing,new Vector3(i*.75f,.3f,-.05f),new Vector3(.35f,.43f,.06f),copper);O("Wing pupil",wing,new Vector3(i*.75f,.3f,-.09f),new Vector3(.19f,.27f,.04f),ink);}
                }
                else for(int i=-1;i<=1;i+=2)T("Broken antler",head,new Vector3(i*.3f,.33f,0),new Vector3(i*.53f,.8f,.06f),.065f,C("656D64"));
            }
            var w=Weapon(body.Find("ArmR"),hero?weapon:shield?1:moth?2:0);w.transform.localPosition=new Vector3(0,-.57f,-.12f);w.transform.localEulerAngles=new Vector3(-12,0,-32);
            return root;
        }
        public static GameObject Weapon(Transform p,int type)
        {
            var r=Models.Root("Weapon",p);var t=r.transform;
            T("Wrapped grip",t,new Vector3(0,-.15f,0),new Vector3(0,.25f,0),.065f,leather);
            for(int i=0;i<5;i++)Band(t,"Grip binding",-.1f+i*.065f,.07f,copper);
            if(type==0)
            {
                Plate("Forged curved steel",t,new[]{new Vector3(-.1f,.25f,0),new Vector3(-.13f,1.05f,0),new Vector3(.08f,1.6f,0),new Vector3(.34f,1.89f,0),new Vector3(.23f,1.35f,0),new Vector3(.18f,.8f,0),new Vector3(.12f,.25f,0)},.07f,C("A4C3C0"));
                Plate("Honed edge",t,new[]{new Vector3(.14f,.3f,-.045f),new Vector3(.19f,1.2f,-.045f),new Vector3(.34f,1.89f,-.045f),new Vector3(.05f,1.5f,-.045f),new Vector3(.08f,.3f,-.045f)},.016f,C("E7E9C9"));
                B("Crossguard",t,new Vector3(0,.25f,0),new Vector3(.52f,.12f,.19f),copper);
                O("Pommel",t,new Vector3(0,-.19f,0),Vector3.one*.18f,edge);
            }
            else if(type==1)
            {
                T("Hammer shaft",t,Vector3.zero,new Vector3(0,1.2f,0),.075f,leather);
                var head=Root("Bell head",t,new Vector3(0,1.24f,0));head.localEulerAngles=new Vector3(0,0,90);
                L("Cast flared bell",head,Vector3.zero,copper,R(.43f,-.48f,.37f),R(.44f,-.35f,.38f),R(.34f,-.2f,.3f),R(.28f,.22f,.25f),R(.13f,.35f,.12f));
                Band(head,"Bell lip",-.42f,.44f,edge,.85f);Band(head,"Band engraving",-.18f,.34f,C("446B6E"),.85f);
                O("Clapper",head,new Vector3(0,-.5f,0),Vector3.one*.26f,ink);
                for(int i=0;i<6;i++){float a=i*Mathf.PI/3;O("Hammer rivet",head,new Vector3(Mathf.Cos(a)*.36f,-.22f,Mathf.Sin(a)*.31f),Vector3.one*.07f,edge);}
            }
            else
            {
                T("Carved staff",t,new Vector3(0,-.3f,0),new Vector3(0,1.55f,0),.065f,C("695344"));
                for(int i=0;i<4;i++)Band(t,"Staff bindings",.8f+i*.12f,.082f,copper);
                var ring=Models.Ring("Astrolabe",t,new Vector3(0,1.54f,0),.36f,.045f,copper);ring.transform.localEulerAngles=new Vector3(90,0,0);
                var ring2=Models.Ring("Orbit",t,new Vector3(0,1.54f,0),.25f,.022f,edge);ring2.transform.localEulerAngles=new Vector3(30,0,30);
                L("Lantern crystal",t,new Vector3(0,1.3f,0),C("95E6D7"),R(0,0,0),R(.16f,.18f,.13f),R(.1f,.37f,.09f),R(0,.5f,0));
            }
            return r;
        }
        static void Hound(Transform p)
        {
            var b=Root("Body",p,new Vector3(0,.72f,0));
            O("Lean body",b,Vector3.zero,new Vector3(1.65f,.68f,.65f),C("3D5964"));
            for(int i=0;i<4;i++){var leg=Root("Leg"+i,p,new Vector3(i<2?-.5f:.5f,.65f,i%2==0?-.21f:.23f));T("Hock",leg,Vector3.zero,new Vector3(-.12f,-.3f,0),.085f,ink);T("Paw",leg,new Vector3(-.12f,-.3f,0),new Vector3(.05f,-.55f,-.03f),.07f,C("798E8B"));}
            var head=Root("Head",b,new Vector3(.75f,.2f,-.05f));
            O("Porcelain wolf skull",head,Vector3.zero,new Vector3(.65f,.58f,.5f),pale);O("Muzzle",head,new Vector3(.25f,-.12f,-.1f),new Vector3(.43f,.28f,.4f),pale);
            Eye(head,.06f,.1f,-.25f,C("EAC678"),.8f);
            for(int i=-1;i<=1;i+=2)L("Pointed ear",head,new Vector3(-.18f,.2f,i*.16f),C("597F81"),R(.12f,0,.1f),R(.08f,.3f,.06f),R(0,.5f,0,-.08f));
            T("Tail",b,new Vector3(-.7f,0,.08f),new Vector3(-1.1f,.5f,.12f),.09f,C("46636D"));
            for(int i=0;i<6;i++)L("Spinal porcelain",b,new Vector3(-.55f+i*.2f,.25f,0),pale,R(.09f,0,.08f),R(0,.26f,0,-.06f));
        }
        static void Gardener(Transform p)
        {
            var b=Root("Body",p,Vector3.zero);
            L("Ancient hollow trunk",b,Vector3.zero,C("71634C"),R(.9f,.1f,.65f),R(.67f,.7f,.5f),R(.8f,1.6f,.55f),R(.5f,2.4f,.35f),R(.25f,2.9f,.2f));
            for(int i=0;i<7;i++){float a=i*6.28f/7;T("Gnarled root",b,new Vector3(Mathf.Cos(a)*.4f,.7f,Mathf.Sin(a)*.35f),new Vector3(Mathf.Cos(a)*1.3f,.03f,Mathf.Sin(a)*1),.16f,C("665843"));}
            var h=Root("Head",b,new Vector3(0,2,-.5f));
            Plate("Split porcelain visage",h,new[]{new Vector3(-.45f,.4f,0),new Vector3(.25f,.55f,0),new Vector3(.48f,.1f,-.07f),new Vector3(.14f,-.5f,0),new Vector3(-.34f,-.29f,0)},.18f,pale);
            Eye(h,-.18f,.13f,-.15f,C("F1BD68"));Eye(h,.19f,.09f,-.16f,C("F1BD68"));
            for(int side=-1;side<=1;side+=2)
            {
                var arm=Root(side<0?"ArmL":"ArmR",b,new Vector3(side*.65f,1.7f,0));
                T("Crooked bough",arm,Vector3.zero,new Vector3(side*.6f,-.25f,-.05f),.19f,C("71634C"));T("Long forebranch",arm,new Vector3(side*.6f,-.25f,-.05f),new Vector3(side*1.05f,-.7f,-.1f),.13f,C("887556"));
                for(int j=0;j<3;j++)T("Root claw",arm,new Vector3(side*1.05f,-.7f,-.1f),new Vector3(side*(1.08f+j*.09f),-1.2f,-.25f+j*.15f),.06f,copper);
            }
            for(int i=0;i<9;i++){float a=i*2.4f;Vector3 v=new Vector3(Mathf.Cos(a)*(.8f+i*.035f),2.8f+Mathf.Sin(i)*.4f,Mathf.Sin(a)*.55f);T("Crown branch",b,new Vector3(0,2.3f,0),v,.075f,C("77664B"));Leaf(b,v,new Vector3(.4f,.7f,.16f),i%2==0?C("4C8978"):C("92A266"),i*41);}
        }
        static void ClockLord(Transform p)
        {
            var b=Root("Body",p,new Vector3(0,.55f,0));
            var face=Root("Head",b,new Vector3(0,1.35f,-.1f));
            var dial=Models.Primitive("Clock body",face,PrimitiveType.Cylinder,Vector3.zero,new Vector3(1.7f,.2f,1.7f),ink);dial.transform.localEulerAngles=new Vector3(90,0,0);
            Gear(face,Vector3.zero,1.05f,16,copper);
            var ring=Models.Ring("Ivory chapter ring",face,new Vector3(0,0,-.24f),.73f,.07f,pale);ring.transform.localEulerAngles=new Vector3(90,0,0);
            for(int i=0;i<12;i++){float a=i*Mathf.PI/6;var mark=B("Hour",face,new Vector3(Mathf.Sin(a)*.65f,Mathf.Cos(a)*.65f,-.26f),new Vector3(.045f,.13f,.025f),edge);mark.transform.localEulerAngles=new Vector3(0,0,-i*30);}
            T("Clock hand",face,new Vector3(0,0,-.29f),new Vector3(.42f,.35f,-.29f),.035f,Models.Ember);
            T("Clock hand",face,new Vector3(0,0,-.3f),new Vector3(-.27f,.18f,-.3f),.045f,Models.Ember);
            O("Soul",face,new Vector3(0,0,-.34f),Vector3.one*.19f,C("B1EEE1"));
            for(int i=-1;i<=1;i+=2){var arm=Root(i<0?"ArmL":"ArmR",b,new Vector3(i*.85f,1.5f,0));T("Articulated arm",arm,Vector3.zero,new Vector3(i*.6f,-.25f,0),.1f,copper);O("Joint",arm,new Vector3(i*.6f,-.25f,0),Vector3.one*.28f,edge);T("Pendulum forearm",arm,new Vector3(i*.6f,-.25f,0),new Vector3(i*.4f,-1.1f,-.12f),.085f,copper);Gear(arm,new Vector3(i*.4f,-1.1f,-.12f),.38f,9,edge);}
            for(int i=0;i<5;i++){float x=(i-2)*.25f;T("Hanging chain",b,new Vector3(x,.8f,.1f),new Vector3(x,.03f,.1f),.027f,copper);L("Pendulum weight",b,new Vector3(x,-.1f,.1f),edge,R(.09f,0,.08f),R(.12f,.16f,.1f),R(.05f,.29f,.05f));}
        }
        static void Leaf(Transform p,Vector3 pos,Vector3 scale,Color c,float angle)
        {
            var g=Plate("Carved leaf",p,new[]{new Vector3(0,0,0),new Vector3(-.22f,.18f,-.04f),new Vector3(-.36f,.45f,-.1f),new Vector3(-.25f,.7f,-.06f),new Vector3(0,1,0),new Vector3(.24f,.69f,-.05f),new Vector3(.34f,.4f,-.09f),new Vector3(.19f,.15f,-.03f)},.03f,c);g.transform.localPosition=pos;g.transform.localScale=scale;g.transform.localEulerAngles=new Vector3(0,angle,angle*.2f);
        }
        public static void Gear(Transform p,Vector3 pos,float radius,int teeth,Color color)
        {
            var root=Root("Gear",p,pos);var ring=Models.Ring("Gear rim",root,Vector3.zero,radius*.78f,radius*.13f,color);ring.transform.localEulerAngles=new Vector3(90,0,0);
            for(int i=0;i<teeth;i++){float a=i*Mathf.PI*2/teeth;var tooth=B("Gear tooth",root,new Vector3(Mathf.Sin(a)*radius*.91f,Mathf.Cos(a)*radius*.91f,0),new Vector3(radius*.24f,radius*.28f,radius*.18f),color);tooth.transform.localEulerAngles=new Vector3(0,0,-a*Mathf.Rad2Deg);}
            for(int i=0;i<4;i++){float a=i*Mathf.PI*.5f;T("Spoke",root,Vector3.zero,new Vector3(Mathf.Sin(a)*radius*.75f,Mathf.Cos(a)*radius*.75f,0),radius*.07f,color);}
        }
        static void Rock(Transform p,Vector3 pos,Vector3 scale,Color color,int seed)
        {
            var rng=new System.Random(seed);var v=new List<Vector3>();var t=new List<int>();int sides=7;
            for(int ring=0;ring<3;ring++)for(int i=0;i<sides;i++){float a=i*Mathf.PI*2/sides;float r=(ring==1?.55f:.42f)*(1+(float)rng.NextDouble()*.15f);v.Add(new Vector3(Mathf.Cos(a)*r,ring==0?0:ring==1?.2f:.38f+(float)rng.NextDouble()*.08f,Mathf.Sin(a)*r));}
            for(int j=0;j<2;j++)for(int i=0;i<sides;i++){int a=j*sides+i,b=j*sides+(i+1)%sides;t.AddRange(new[]{a,a+sides,b,b,a+sides,b+sides});}
            for(int i=1;i<sides-1;i++)t.AddRange(new[]{14,14+i+1,14+i});
            Models.MeshPart("Chipped stone",p,Finish("Hand-shaped stone",v,t),pos,scale,color);
        }
        public static GameObject Stage(int area)
        {
            var root=Models.Root("Environment "+Catalog.Areas[area]);var p=root.transform;var rng=new System.Random(730+area);
            Color ground=area==0?C("53686A"):area==1?C("425E51"):C("555465");
            B("Earth below the path",p,new Vector3(0,-.55f,-8),new Vector3(32,.7f,28),ground*.65f);
            var floor=B("Painted masonry",p,new Vector3(0,-.06f,-8),new Vector3(32,.12f,28),ground);
            var material=new Material(Shader.Find("Mist/Ground")){name="Ground "+area};material.mainTexture=Resources.Load<Texture2D>("Art/Ground-v2");material.SetFloat("_Area",area);floor.GetComponent<Renderer>().sharedMaterial=material;
            for(int i=0;i<44;i++){float x=(float)rng.NextDouble()*25-12.5f,z=(float)rng.NextDouble()*9-4.5f;float size=.15f+(float)rng.NextDouble()*.3f;if(i%3==0)Rock(p,new Vector3(x,.025f,z),new Vector3(size,.16f,size*.7f),ground*.7f,i+111);}
            for(int i=0;i<14;i++)
            {
                float x=-13+i*2,back=5.3f+(float)rng.NextDouble()*.8f;
                if(area!=1)
                {
                    float h=.45f+(float)rng.NextDouble()*.45f;
                    B("Broken parapet",p,new Vector3(x,h*.5f,back),new Vector3(1.8f,h,.6f),ground*.8f);
                    Rock(p,new Vector3(x,h,back),new Vector3(2,.3f,.85f),ground*1.12f,100+i);
                    if(i%3==0){B("Masonry pillar",p,new Vector3(x,1.3f,back),new Vector3(.64f,2.6f,.7f),ground);Models.Lantern(p,new Vector3(x,2.6f,back),1.05f);}
                }
                else if(i==1||i==12)Tree(p,new Vector3(x,0,back+.5f),4.8f,i);
                if(i%3==1)Barrel(p,new Vector3(x,.02f,4.3f));
                for(int j=0;j<5;j++)Leaf(p,new Vector3(x+j*.17f,.06f,4.55f+(j%2)*.35f),new Vector3(.3f,.35f+j*.055f,.2f),area==2?C("696D70"):C("657F56"),j*53+i*27);
            }
            if(area==0){Models.Arch(p,new Vector3(-12,0,4),3.2f,4.2f,ground);Models.Arch(p,new Vector3(12,0,4),3.2f,4.2f,ground);}
            if(area==1){Models.Arch(p,new Vector3(-10,0,7),4,4.4f,ground*.9f);Models.Arch(p,new Vector3(9,0,7),4,4,ground*.9f);}
            if(area==2){Gear(p,new Vector3(-10,4,6.5f),2.7f,16,copper*.65f);Gear(p,new Vector3(10,4.6f,7),3.2f,18,copper*.8f);for(int i=0;i<8;i++)T("Long hanging chain",p,new Vector3((i-3.5f)*3.5f,2,8),new Vector3((i-3.5f)*3.5f,13,8),.04f,copper*.6f);}
            // Deliberate foreground clusters frame the play space instead of a rectangular plinth.
            for(int side=-1;side<=1;side+=2)
            {
                Rock(p,new Vector3(side*11,-.05f,-5.2f),new Vector3(3,2.4f,2.4f),ground*.58f,40+side);
                for(int j=0;j<9;j++)Leaf(p,new Vector3(side*(8.8f+j*.36f),0,-5.1f),new Vector3(.6f,.85f+j*.07f,.2f),C("253E40"),j*31);
                if(area==1)Tree(p,new Vector3(side*14,0,0),7,side+4);
            }
            return root;
        }
        static void Barrel(Transform p,Vector3 pos)
        {
            var t=Root("Coopered barrel",p,pos);L("Wooden staves",t,Vector3.zero,C("76604D"),R(.33f,0,.33f),R(.43f,.4f,.43f),R(.35f,.9f,.35f));
            Band(t,"Iron hoop",.16f,.38f,C("455157"));Band(t,"Iron hoop",.72f,.4f,C("455157"));
            for(int i=0;i<10;i++){float a=i*6.28f/10;T("Stave seam",t,new Vector3(Mathf.Cos(a)*.35f,.08f,Mathf.Sin(a)*.35f),new Vector3(Mathf.Cos(a)*.39f,.8f,Mathf.Sin(a)*.39f),.012f,C("433A33"));}
        }
        static void Tree(Transform p,Vector3 pos,float height,int seed)
        {
            var t=Root("Twisted tree",p,pos);Color bark=C("384A49");
            L("Bent trunk",t,Vector3.zero,bark,R(.5f,0,.38f),R(.25f,height*.35f,.22f,.2f),R(.2f,height*.65f,.18f,-.08f),R(.05f,height,.06f,.35f));
            for(int i=0;i<4;i++){float sign=i%2==0?-1:1;Vector3 v=new Vector3(sign*(.8f+i*.2f),height*(.55f+i*.12f),.2f);T("Forked branch",t,new Vector3(0,height*.4f,0),v,.12f,bark);for(int j=0;j<4;j++)Leaf(t,v+new Vector3((j-1.5f)*.3f,0,0),new Vector3(1.3f,1.3f,.3f),C(i%2==0?"365D54":"416D5B"),j*53+seed*21);}
        }
    }
}
