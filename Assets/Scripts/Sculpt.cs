using System;
using System.Collections.Generic;
using UnityEngine;

namespace Mist
{
    // Solid authored parts, shared by the game and its editable model inspection scene.
    // Coordinates are in character space: front is -Z, up is +Y.
    public static partial class Sculpt
    {
        public enum Finish { Cloth, Leather, Iron, Brass, Ceramic, Wood, Stone, Glow, Skin, Hair }
        static readonly Dictionary<string,Material> materials=new Dictionary<string,Material>();
        static Color C(string hex){return Models.Hex(hex);}
        static readonly Color teal=C("285C61"),lightTeal=C("447C78"),ink=C("1A292E"),bronze=C("9E7546"),gold=C("D1AC70"),steel=C("697F83"),bone=C("C4C7AF"),red=C("803F38"),leather=C("443A33");
        static Vector3 V(float x,float y,float z=0){return new Vector3(x,y,z);}
        static Transform N(string name,Transform parent,Vector3 position=default(Vector3)) {return Models.Root(name,parent,position).transform;}
        public static Material Material(Color c,Finish f)
        {
            string key=ColorUtility.ToHtmlStringRGBA(c)+"_"+f;Material m;
            if(materials.TryGetValue(key,out m)&&m)return m;
            m=new Material(Shader.Find("Mist/Sculpt")){name="Sculpt_"+key,color=c};
            m.SetFloat("_Metallic",f==Finish.Brass?.85f:f==Finish.Iron?.7f:0);
            m.SetFloat("_Smoothness",f==Finish.Brass?.52f:f==Finish.Iron?.38f:f==Finish.Ceramic?.48f:f==Finish.Leather?.22f:f==Finish.Skin?.12f:.06f);
            m.SetFloat("_OutlineWidth",f==Finish.Glow?0:f==Finish.Skin?.0025f:f==Finish.Hair?.0015f:.004f);
            m.SetFloat("_SurfaceType",f==Finish.Stone?1:f==Finish.Wood?2:f==Finish.Cloth?3:f==Finish.Leather?4:f==Finish.Brass?5:f==Finish.Iron?6:f==Finish.Skin?7:0);
            if(f==Finish.Stone)m.SetTexture("_DetailTex",Resources.Load<Texture2D>("Art/Ground-v2"));
            m.SetTexture("_CraftTex",Resources.Load<Texture2D>("Art/CraftSurfaces-v1"));
            m.SetColor("_EmissionColor",f==Finish.Glow?c:Color.black);materials[key]=m;return m;
        }
        static Mesh Mesh(string name,List<Vector3> v,List<int> t)
        {
            var m=new Mesh{name=name};if(v.Count>65000)m.indexFormat=UnityEngine.Rendering.IndexFormat.UInt32;
            m.SetVertices(v);m.SetTriangles(t,0);m.RecalculateNormals();
            // At acute folded edges an averaged normal can point through the adjacent face.
            // Split those corners, as a hard-normal seam, while preserving all geometry.
            for(int pass=0;pass<6;pass++){
                var normals=m.normals;bool split=false;
                for(int i=0;i<t.Count;i+=3){Vector3 normal=Vector3.Cross(v[t[i+1]]-v[t[i]],v[t[i+2]]-v[t[i]]);if(normal.sqrMagnitude<1e-12f)continue;
                    Vector3 average=normals[t[i]]+normals[t[i+1]]+normals[t[i+2]];
                    if(Vector3.Dot(normal,average)>=0)continue;
                    for(int j=0;j<3;j++){var point=v[t[i+j]];t[i+j]=v.Count;v.Add(point);}split=true;
                }
                if(!split)break;m.SetVertices(v);m.SetTriangles(t,0);m.RecalculateNormals();
            }
            m.RecalculateBounds();
            var colors=new Color[v.Count];for(int i=0;i<colors.Length;i++)colors[i]=Color.white;m.colors=colors;return m;
        }
        static GameObject Part(string name,Transform p,Mesh mesh,Color color,Finish finish=Finish.Cloth)
        {
            var g=Models.Root(name,p);g.AddComponent<MeshFilter>().sharedMesh=mesh;g.AddComponent<MeshRenderer>().sharedMaterial=Material(color,finish);return g;
        }
        static void Quad(List<int> t,int a,int b,int c,int d){t.AddRange(new[]{a,b,c,a,c,d});}
        static void HardQuad(List<Vector3> v,List<int> t,int a,int b,int c,int d)
        {int start=v.Count;v.Add(v[a]);v.Add(v[b]);v.Add(v[c]);v.Add(v[d]);Quad(t,start,start+1,start+2,start+3);}
        // Smoothly interpolated swept sections. Tangents define an actual 3D tube, not a billboard.
        public static GameObject Sweep(string name,Transform parent,Vector3[] path,Vector2[] radii,Color col,Finish finish=Finish.Cloth,int sides=12,int steps=5,float folds=0)
        {
            var v=new List<Vector3>();var t=new List<int>();int count=(path.Length-1)*steps+1;bool closed=Vector3.Distance(path[0],path[path.Length-1])<.00001f;Vector3 transported=Vector3.zero;
            for(int k=0;k<count;k++){
                float u=k/(float)(count-1)*(path.Length-1);int a=Mathf.Min((int)u,path.Length-2);float q=u-a;
                Vector3 p0=path[a==0?(closed?path.Length-2:0):a-1],p1=path[a],p2=path[a+1],p3=path[a+2>=path.Length?(closed?1:path.Length-1):a+2];
                Vector3 point=.5f*((2*p1)+(-p0+p2)*q+(2*p0-5*p1+4*p2-p3)*q*q+(-p0+3*p1-3*p2+p3)*q*q*q);
                Vector3 tangent=(.5f*((-p0+p2)+2*(2*p0-5*p1+4*p2-p3)*q+3*(-p0+3*p1-3*p2+p3)*q*q)).normalized;if(tangent.sqrMagnitude<.01f)tangent=(p2-p1).normalized;Vector3 right=k==0?Vector3.Cross(tangent,Mathf.Abs(tangent.z)<.85f?Vector3.forward:Vector3.up).normalized:(transported-tangent*Vector3.Dot(transported,tangent)).normalized;if(right.sqrMagnitude<.01f)right=Vector3.Cross(tangent,Vector3.up).normalized;transported=right;
                Vector3 forward=Vector3.Cross(right,tangent).normalized;Vector2 r=Vector2.Lerp(radii[a],radii[a+1],Mathf.SmoothStep(0,1,q));
                for(int j=0;j<sides;j++){float ang=j*Mathf.PI*2/sides;float fold=1+folds*Mathf.Sin(ang*7+k*.13f);v.Add(point+right*Mathf.Cos(ang)*r.x*fold+forward*Mathf.Sin(ang)*r.y*fold);}
            }
            for(int k=0;k<count-1;k++)for(int j=0;j<sides;j++){int a=k*sides+j,b=k*sides+(j+1)%sides;Quad(t,a,a+sides,b+sides,b);}
            if(!closed)for(int e=0;e<2;e++){int c=v.Count;v.Add(path[e==0?0:path.Length-1]);int source=e==0?0:(count-1)*sides,start=v.Count;for(int j=0;j<sides;j++)v.Add(v[source+j]);for(int j=0;j<sides;j++)if(e==0)t.AddRange(new[]{c,start+j,start+(j+1)%sides});else t.AddRange(new[]{c,start+(j+1)%sides,start+j});}
            return Part(name,parent,Mesh(name,v,t),col,finish);
        }
        static GameObject Curve(string name,Transform p,Color color,Finish f,float radius,params Vector3[] points)
        {var rr=new Vector2[points.Length];for(int i=0;i<rr.Length;i++)rr[i]=Vector2.one*radius;return Sweep(name,p,points,rr,color,f,8,4);}
        static GameObject Form(string name,Transform p,Vector3 center,Vector3 scale,Color col,Finish f=Finish.Cloth,float irregular=0)
        {
            var v=new List<Vector3>();var t=new List<int>();const int rows=16,sides=24;
            for(int k=0;k<=rows;k++)for(int j=0;j<sides;j++){
                float a=j*Mathf.PI*2/sides,b=k*Mathf.PI/rows;float r=1+irregular*Mathf.Sin(a*3+b*5)*Mathf.Sin(b);
                v.Add(center+Vector3.Scale(V(Mathf.Sin(b)*Mathf.Cos(a)*r,Mathf.Cos(b),Mathf.Sin(b)*Mathf.Sin(a)*r),scale));}
            for(int k=0;k<rows;k++)for(int j=0;j<sides;j++){int a=k*sides+j,b=k*sides+(j+1)%sides;Quad(t,a,b,b+sides,a+sides);}
            return Part(name,p,Mesh(name,v,t),col,f);
        }
        // Raised, bevelled solid panel, including the back and perimeter.
        static GameObject Panel(string name,Transform p,Vector2[] outline,float z,float depth,float bevel,Color col,Finish f=Finish.Cloth)
        {
            var v=new List<Vector3>();var t=new List<int>();int n=outline.Length;Vector2 center=Vector2.zero;foreach(var x in outline)center+=x;center/=n;
            for(int row=0;row<4;row++)foreach(var x in outline){var q=(row==0||row==3)?Vector2.Lerp(x,center,bevel):x;v.Add(V(q.x,q.y,z+(row==0?-depth*.5f:row==1?-depth*.5f+depth*.2f:row==2?depth*.5f-depth*.2f:depth*.5f)));}
            for(int row=0;row<3;row++)for(int j=0;j<n;j++)Quad(t,row*n+j,row*n+(j+1)%n,(row+1)*n+(j+1)%n,(row+1)*n+j);
            var polygon=new List<int>();for(int i=0;i<n;i++)polygon.Add(i);
            float signed=0;for(int i=0;i<n;i++)signed+=outline[i].x*outline[(i+1)%n].y-outline[(i+1)%n].x*outline[i].y;
            int safety=n*n;while(polygon.Count>2&&safety-->0){bool found=false;for(int k=0;k<polygon.Count;k++){
                int a=polygon[(k+polygon.Count-1)%polygon.Count],b=polygon[k],c=polygon[(k+1)%polygon.Count];
                Vector2 ab=outline[b]-outline[a],ac=outline[c]-outline[a];float cross=ab.x*ac.y-ab.y*ac.x;if(cross*signed<=0)continue;
                bool contains=false;foreach(int j in polygon){if(j==a||j==b||j==c)continue;Vector2 pt=outline[j];
                    float d1=(outline[b].x-outline[a].x)*(pt.y-outline[a].y)-(outline[b].y-outline[a].y)*(pt.x-outline[a].x);
                    float d2=(outline[c].x-outline[b].x)*(pt.y-outline[b].y)-(outline[c].y-outline[b].y)*(pt.x-outline[b].x);
                    float d3=(outline[a].x-outline[c].x)*(pt.y-outline[c].y)-(outline[a].y-outline[c].y)*(pt.x-outline[c].x);
                    if((d1>=0&&d2>=0&&d3>=0)||(d1<=0&&d2<=0&&d3<=0)){contains=true;break;}}
                if(contains)continue;int front=v.Count;v.Add(v[a]);v.Add(v[c]);v.Add(v[b]);t.AddRange(new[]{front,front+1,front+2});int back=v.Count;v.Add(v[3*n+a]);v.Add(v[3*n+b]);v.Add(v[3*n+c]);t.AddRange(new[]{back,back+1,back+2});polygon.RemoveAt(k);found=true;break;
            }if(!found)break;}
            // Accept either contour winding; front faces always point towards -Z.
            if(signed<0)for(int i=0;i<t.Count;i+=3){int q=t[i+1];t[i+1]=t[i+2];t[i+2]=q;}
            return Part(name,p,Mesh(name,v,t),col,f);
        }
        static Vector2 P(float x,float y){return new Vector2(x,y);}
        static GameObject Block(string name,Transform p,Vector3 center,Vector3 size,Color color,Finish f=Finish.Iron)
        {var g=Part(name,p,Models.BevelCube(),color,f);g.transform.localPosition=center;g.transform.localScale=size;return g;}
        static GameObject Ring(string name,Transform p,Vector3 center,float radius,float wire,Color color,Finish f=Finish.Brass,Vector3 rotation=default(Vector3))
        {
            var points=new Vector3[33];var rr=new Vector2[33];for(int i=0;i<33;i++){float a=i*Mathf.PI/16;points[i]=V(Mathf.Cos(a)*radius,Mathf.Sin(a)*radius,0);rr[i]=Vector2.one*wire;}
            var g=Sweep(name,p,points,rr,color,f,8,1);g.transform.localPosition=center;g.transform.localEulerAngles=rotation;return g;
        }
        static void Stud(Transform p,Vector3 pos,float radius=.022f){Form("Peened rivet",p,pos,V(radius,radius,radius*.6f),gold,Finish.Brass);}
        static void Trim(Transform p,Color c,float r,params Vector3[] pts){Curve("Raised bound edge",p,c,Finish.Leather,r,pts);}
        static void Stitch(Transform p,Vector3 a,Vector3 b,int count,Color c)
        {for(int i=0;i<count;i++){var at=Vector3.Lerp(a,b,(i+.5f)/count);Curve("Saddle stitch",p,c,Finish.Cloth,.006f,at+V(-.012f,-.012f,-.006f),at+V(.012f,.012f,-.006f));}}
        static void Strap(Transform p,Vector3[] path,float width,Color color)
        {
            var radii=new Vector2[path.Length];for(int i=0;i<radii.Length;i++)radii[i]=P(width,.022f);
            Sweep("Leather harness strap",p,path,radii,color,Finish.Leather,8,5);
        }
        static void Buckle(Transform p,Vector3 center,float scale=1)
        {
            var q=N("Forged rectangular buckle",p,center);q.localScale=Vector3.one*scale;
            Curve("Buckle frame",q,gold,Finish.Brass,.018f,V(-.1f,-.075f),V(.1f,-.075f),V(.1f,.075f),V(-.1f,.075f),V(-.1f,-.075f));
            Curve("Buckle pin",q,bronze,Finish.Brass,.012f,V(0,-.075f,-.006f),V(0,.045f,-.006f));
        }
        // An open, thick cloth shell with curved folds; the lining is independent geometry.
        static void Drape(string name,Transform p,float top,float bottom,float topWidth,float hemWidth,float z,Color col,int folds=7,float tear=.04f)
        {
            const int columns=32,rows=16;var v=new List<Vector3>();var t=new List<int>();
            for(int layer=0;layer<2;layer++)for(int j=0;j<=rows;j++)for(int i=0;i<=columns;i++){
                float u=i/(float)columns,w=j/(float)rows,x=(u-.5f)*2*Mathf.Lerp(topWidth,hemWidth,w);
                float wave=Mathf.Sin(u*folds*Mathf.PI*2+.5f)*(.015f+w*.06f);
                float y=Mathf.Lerp(top,bottom,w)+w*w*w*(Mathf.Cos(u*17)*tear);
                float zz=z+wave+Mathf.Sin(u*Mathf.PI)*.1f+w*w*.12f+layer*.035f;
                v.Add(V(x,y,zz));}
            int stride=columns+1,layerN=stride*(rows+1);
            for(int layer=0;layer<2;layer++)for(int j=0;j<rows;j++)for(int i=0;i<columns;i++){int a=layer*layerN+j*stride+i;if(layer==0)Quad(t,a,a+stride,a+stride+1,a+1);else Quad(t,a,a+1,a+stride+1,a+stride);}
            for(int i=0;i<columns;i++){int a=rows*stride+i;HardQuad(v,t,a,a+1,a+layerN+1,a+layerN);HardQuad(v,t,i,i+layerN,i+layerN+1,i+1);}
            for(int j=0;j<rows;j++){int a=j*stride;HardQuad(v,t,a,a+layerN,a+stride+layerN,a+stride);a+=columns;HardQuad(v,t,a,a+stride,a+stride+layerN,a+layerN);}
            for(int i=0;i<t.Count;i+=3){int swap=t[i+1];t[i+1]=t[i+2];t[i+2]=swap;}
            Part(name,p,Mesh(name,v,t),col,Finish.Cloth);
        }
        static void YouthFace(Transform head,bool porcelain=false)
        {
            Color complexion=porcelain?C("C7CDB9"):C("CDA184");
            var vertices=new List<Vector3>();var triangles=new List<int>();var pigment=new List<Color>();const int rows=24,sides=40;
            for(int row=0;row<=rows;row++)for(int j=0;j<sides;j++){
                float b=row*Mathf.PI/rows,a=j*Mathf.PI*2/sides,y=.06f+Mathf.Cos(b)*.38f;
                float jaw=1-.17f*Mathf.Clamp01((-.03f-y)/.27f);
                float x=Mathf.Sin(b)*Mathf.Cos(a)*.325f*jaw,z=Mathf.Sin(b)*Mathf.Sin(a)*.27f-.035f;
                if(Mathf.Sin(a)<0){float front=-Mathf.Sin(a);z-=front*.014f*Mathf.Exp(-x*x/.015f-(y-.035f)*(y-.035f)/.025f);}
                vertices.Add(V(x,y,z));
                float cheek=Mathf.Exp(-Mathf.Pow((Mathf.Abs(x)-.2f)/.08f,2)-Mathf.Pow((y+.045f)/.07f,2))*Mathf.Max(0,-Mathf.Sin(a));
                pigment.Add(porcelain?Color.white:Color.Lerp(Color.white,new Color(1,.88f,.83f,1),cheek*.38f));
            }
            for(int row=0;row<rows;row++)for(int j=0;j<sides;j++){int a=row*sides+j,b=row*sides+(j+1)%sides;Quad(triangles,a,b,b+sides,a+sides);}
            var faceMesh=Mesh("Continuous rounded face and jaw",vertices,triangles);if(faceMesh.vertexCount==pigment.Count)faceMesh.SetColors(pigment);
            Part("Unified face volume",head,faceMesh,complexion,porcelain?Finish.Ceramic:Finish.Skin);
            for(int side=-1;side<=1;side+=2){
                float x=side*.136f;
                // Open eye volumes, with fine eyelids instead of dark rings or lowered brows.
                Form("Open eye white",head,V(x,.14f,-.283f),V(.078f,.062f,.018f),C("E6E3D4"),Finish.Ceramic);
                Form("Round iris",head,V(x+.006f,.139f,-.3f),V(.031f,.04f,.005f),porcelain?C("4D958A"):C("655746"),Finish.Ceramic);
                Form("Round pupil",head,V(x+.006f,.14f,-.306f),V(.017f,.028f,.003f),C("202D31"),Finish.Ceramic);
                Form("Eye catchlight",head,V(x-.006f,.158f,-.31f),V(.008f,.011f,.003f),C("FCF5DE"),Finish.Ceramic);
                Curve("Fine upper eyelid",head,porcelain?C("6A8580"):C("695044"),Finish.Cloth,.007f,V(x-.074f,.14f,-.282f),V(x-.043f,.188f,-.293f),V(x+.012f,.202f,-.299f),V(x+.074f,.15f,-.284f));
                Curve("Relaxed arched eyebrow",head,porcelain?C("9BAC9E"):C("564335"),Finish.Cloth,.013f,V(side*.072f,.245f,-.273f),V(side*.145f,.263f,-.268f),V(side*.22f,.244f,-.245f));
                if(!porcelain){Form("Ear with rounded helix",head,V(side*.312f,.055f,-.015f),V(.058f,.097f,.055f),complexion,Finish.Skin);Form("Ear inner fold",head,V(side*.326f,.05f,-.064f),V(.022f,.052f,.009f),C("A97965"),Finish.Skin);}
            }
            Form("Small rounded nose",head,V(0,.015f,-.309f),V(.045f,.057f,.045f),porcelain?C("B6C4B1"):C("CFA184"),porcelain?Finish.Ceramic:Finish.Skin);
            if(!porcelain){
                Curve("Natural mouth line",head,C("805447"),Finish.Cloth,.007f,V(-.049f,-.135f,-.269f),V(0,-.144f,-.28f),V(.049f,-.135f,-.269f));
                Form("Subtle lower lip",head,V(0,-.157f,-.265f),V(.027f,.009f,.009f),C("B98772"),Finish.Skin);
                for(int i=0;i<5;i++)Sweep("Swept soft hair lock",head,new[]{V(-.24f+i*.105f,.373f,-.17f),V(-.25f+i*.1f,.33f,-.244f),V(-.29f+i*.096f, .305f+(i%2)*.018f,-.27f)},new[]{P(.059f,.035f),P(.047f,.026f),P(.003f,.003f)},C(i%2==0?"5D4735":"73563D"),Finish.Hair,16,5);
            }else{
                Curve("Fine porcelain fracture",head,C("687F76"),Finish.Ceramic,.006f,V(.12f,.37f,-.19f),V(.075f,.3f,-.25f),V(.1f,.25f,-.276f));
                Curve("Carved quiet mouth",head,C("82948A"),Finish.Ceramic,.006f,V(-.045f,-.14f,-.271f),V(.045f,-.14f,-.271f));
            }
        }
        static void Hood(Transform head)
        {
            var v=new List<Vector3>();var t=new List<int>();const int sides=48,rows=14;
            // An open cowl with a visible inner lining. The face remains a separate solid volume.
            for(int lining=0;lining<2;lining++)for(int k=0;k<=rows;k++)for(int j=0;j<sides;j++){
                float q=k/(float)rows,a=j*Mathf.PI*2/sides;
                float radius=Mathf.Sqrt(1-q*q)*(.445f-lining*.045f);
                float x=Mathf.Cos(a)*radius-.18f*q*q;
                float y=.11f+Mathf.Sin(a)*radius*1.15f+q*.32f;
                float z=-.265f+q*.72f+Mathf.Sin(a*5+q*5)*.018f*(1-q);
                v.Add(V(x,y,z));}
            int layer=(rows+1)*sides;
            for(int l=0;l<2;l++)for(int k=0;k<rows;k++)for(int j=0;j<sides;j++){int a=l*layer+k*sides+j,b=l*layer+k*sides+(j+1)%sides;if(l==0)Quad(t,a,b,b+sides,a+sides);else Quad(t,a,a+sides,b+sides,b);}
            for(int j=0;j<sides;j++)Quad(t,j,layer+j,layer+(j+1)%sides,(j+1)%sides);
            Part("Open folded hood with inner lining",head,Mesh("Cowl shell",v,t),teal);
            var edgePoints=new Vector3[49];for(int j=0;j<=48;j++){float a=j*Mathf.PI/24;edgePoints[j]=V(Mathf.Cos(a)*.447f,.11f+Mathf.Sin(a)*.512f,-.284f+Mathf.Sin(a*5)*.018f);}
            Curve("Bound hood opening",head,lightTeal,Finish.Cloth,.024f,edgePoints);
            Curve("Cowl crown seam",head,C("1C454B"),Finish.Cloth,.015f,V(0,.65f,-.27f),V(-.025f,.65f,0),V(-.06f,.5f,.36f));
            Stitch(head,V(-.29f,.51f,-.265f),V(-.15f,.61f,-.27f),5,C("A29B6E"));
        }
        static void Boot(Transform leg,int side,bool armored)
        {
            Sweep("Tapered trouser with knee folds",leg,new[]{V(0,0),V(.015f,-.19f),V(-.015f,-.38f)},new[]{P(.16f,.17f),P(.145f,.145f),P(.12f,.13f)},C("293C43"),Finish.Cloth,20,5,.055f);
            Sweep("Boot shaft",leg,new[]{V(0,-.32f),V(0,-.51f),V(0,-.7f,-.055f)},new[]{P(.15f,.15f),P(.125f,.14f),P(.15f,.18f)},leather,Finish.Leather,20,4,.018f);
            Form("Lasted boot toe",leg,V(0,-.7f,-.14f),V(.18f,.12f,.27f),C("54463A"),Finish.Leather);
            Sweep("Layered leather sole",leg,new[]{V(0,-.81f,-.1f),V(0,-.755f,-.1f)},new[]{P(.187f,.32f),P(.193f,.32f)},ink,Finish.Leather,24,1);
            Curve("Toe cap seam",leg,C("A08A63"),Finish.Leather,.009f,V(-.163f,-.7f,-.22f),V(-.1f,-.63f,-.29f),V(0,-.61f,-.31f),V(.1f,-.63f,-.29f),V(.163f,-.7f,-.22f));
            for(int i=0;i<2;i++){float y=-.37f-i*.13f;Strap(leg,new[]{V(-.13f,y,.02f),V(-.13f,y,-.13f),V(0,y,-.16f),V(.13f,y,-.13f),V(.13f,y,.02f)},.035f,C("6B5540"));Buckle(leg,V(side*.12f,y,-.135f),.48f);}
            if(armored){Panel("Greave with raised central rib",leg,new[]{P(-.12f,-.3f),P(0,-.24f),P(.12f,-.3f),P(.09f,-.67f),P(0,-.72f),P(-.1f,-.65f)},-.183f,.075f,.15f,steel,Finish.Iron);Curve("Greave midrib",leg,gold,Finish.Brass,.014f,V(0,-.29f,-.228f),V(0,-.63f,-.228f));}
        }
        static void BindLowerLeg(Transform leg)
        {
            var parts=new List<Transform>();foreach(Transform t in leg)if(t.name!="Tapered trouser with knee folds")parts.Add(t);
            var shin=N("Shin",leg,V(0,-.3f));foreach(var t in parts)t.SetParent(shin,true);
        }
        static void Arm(Transform arm,int side,bool armor,bool boss=false)
        {
            Sweep("Shaped sleeve",arm,new[]{V(0,.015f),V(side*.06f,-.19f,-.015f),V(side*.025f,-.39f,-.055f)},new[]{P(.18f,.19f),P(.16f,.16f),P(.12f,.12f)},armor?C("374B50"):teal,Finish.Cloth,20,5,.05f);
            var fore=N("Forearm",arm,V(side*.025f,-.33f,-.04f));
            Sweep("Leather gauntlet cuff",fore,new[]{V(0,.02f),V(0,-.13f,-.015f),V(0,-.27f,-.04f)},new[]{P(.155f,.145f),P(.13f,.13f),P(.1f,.1f)},leather,Finish.Leather,20,4);
            for(int j=0;j<3;j++){
                float y=-.02f-j*.075f;
                Panel("Overlapping vambrace plate",fore,new[]{P(-.13f,y+.04f),P(.13f,y+.04f),P(.105f,y-.065f),P(-.105f,y-.065f)},-.142f-j*.004f,.038f,.12f,armor?steel:bronze,Finish.Iron);
                if(j==0||j==2){Stud(fore,V(-.085f,y,-.169f));Stud(fore,V(.085f,y,-.169f));}
            }
            Form("Gloved palm",fore,V(0,-.32f,-.08f),V(.112f,.13f,.105f),leather,Finish.Leather);
            for(int j=0;j<4;j++){float x=-.081f+j*.052f;Sweep("Bent individual glove finger",fore,new[]{V(x,-.32f,-.154f),V(x,-.397f,-.164f),V(x,-.41f,-.09f)},new[]{P(.025f,.025f),P(.023f,.023f),P(.018f,.018f)},C("78604A"),Finish.Leather,8,3);}
            Sweep("Opposed glove thumb",fore,new[]{V(-side*.08f,-.285f,-.08f),V(-side*.125f,-.31f,-.11f),V(-side*.12f,-.35f,-.14f),V(-side*.085f,-.365f,-.17f)},new[]{P(.03f,.03f),P(.028f,.028f),P(.024f,.024f),P(.015f,.015f)},leather,Finish.Leather,10,3);
            if(armor||side==1){
                var shoulder=N("Three overlapping shoulder lames",arm,V(side*.015f,.04f));
                for(int j=0;j<3;j++){
                    var q=N("Shoulder lame "+j,shoulder,V(side*j*.025f,-j*.09f,j*.008f));
                    q.localEulerAngles=V(0,0,-side*(10+j*8));
                    Sweep("Forged shoulder shell",q,new[]{V(0,-.08f),V(0,0),V(0,.1f)},new[]{P(.245f-j*.025f,.25f-j*.015f),P(.24f-j*.025f,.24f-j*.015f),P(.12f,.13f)},j==0?bronze:steel,Finish.Iron,24,4);
                    Curve("Shoulder rolled edge",q,gold,Finish.Brass,.014f,V(-.21f+j*.02f,-.075f,-.12f),V(0,-.075f,-.25f+j*.015f),V(.21f-j*.02f,-.075f,-.12f));
                }
                for(int i=0;i<3;i++)Stud(shoulder,V(-.14f+i*.14f,.02f,-.24f));
            }
        }
        static void Torso(Transform body,bool armor,bool boss=false)
        {
            Sweep("Fitted gambeson",body,new[]{V(0,.81f),V(0,1.05f),V(0,1.4f),V(0,1.67f)},new[]{P(.34f,.25f),P(.32f,.24f),P(.43f,.28f),P(.23f,.2f)},armor?C("34484A"):teal,Finish.Cloth,32,5,.025f);
            for(int side=-1;side<=1;side+=2){
                var flap=N("Split tailored coat panel",body,V(side*.22f,0,-.18f));flap.localEulerAngles=V(0,-side*12,-side*4);
                Drape("Weighted coat skirt",flap,1.03f,.49f,.15f,.23f,0,armor?C("445650"):teal,3,.025f);
                Curve("Gold bound coat edge",flap,C("8E946F"),Finish.Cloth,.013f,V(-.15f,1.01f,-.01f),V(-.19f,.75f,.055f),V(-.23f,.51f,.12f),V(0,.49f,.14f),V(.23f,.51f,.12f));
            }
            for(int side=-1;side<=1;side+=2){Panel("Layered coat lapel",body,new[]{P(side*.08f,1.68f),P(side*.3f,1.6f),P(side*.24f,1.31f),P(side*.07f,1.18f)},-.27f,.07f,.1f,lightTeal);}
            if(armor){
                Sweep("Hammered cuirass volume",body,new[]{V(0,1.03f,-.1f),V(0,1.3f,-.07f),V(0,1.53f,-.035f),V(0,1.62f)},new[]{P(.34f,.21f),P(.45f,.27f),P(.4f,.26f),P(.25f,.18f)},steel,Finish.Iron,28,4);
                for(int j=0;j<3;j++)Panel("Articulated fauld",body,new[]{P(-.34f-j*.015f,1.1f-j*.09f),P(.34f+j*.015f,1.1f-j*.09f),P(.38f+j*.01f,1.02f-j*.09f),P(-.38f-j*.01f,1.02f-j*.09f)},-.3f,.06f,.12f,j%2==0?bronze:steel,Finish.Iron);
                Panel("Recessed breast sigil",body,new[]{P(-.18f,1.47f),P(0,1.55f),P(.18f,1.47f),P(.14f,1.25f),P(0,1.16f),P(-.14f,1.25f)},-.36f,.035f,.12f,teal,Finish.Iron);
                Curve("Lantern ward emblem",body,gold,Finish.Brass,.018f,V(-.07f,1.3f,-.39f),V(-.07f,1.44f,-.39f),V(0,1.49f,-.39f),V(.07f,1.44f,-.39f),V(.07f,1.3f,-.39f),V(-.07f,1.3f,-.39f));
            }else{
                Strap(body,new[]{V(-.29f,1.6f,-.205f),V(-.12f,1.42f,-.31f),V(.11f,1.18f,-.295f),V(.3f,.99f,-.21f)},.062f,leather);Buckle(body,V(.02f,1.31f,-.34f),.7f);
                Stitch(body,V(-.1f,1.4f,-.34f),V(.2f,1.08f,-.3f),11,C("A3926D"));
            }
            Strap(body,new[]{V(-.34f,1.03f,.09f),V(-.34f,1.03f,-.17f),V(0,1.03f,-.29f),V(.34f,1.03f,-.17f),V(.34f,1.03f,.09f)},.05f,leather);Buckle(body,V(.04f,1.03f,-.31f));
            var bag=N("Seamed travel satchel",body,V(-.38f,.83f,.035f));bag.localEulerAngles=V(0,0,-10);
            Sweep("Soft leather bag",bag,new[]{V(0,-.17f),V(0,0),V(0,.17f)},new[]{P(.135f,.1f),P(.18f,.14f),P(.15f,.1f)},C("655039"),Finish.Leather,20,4);
            Panel("Overlapping satchel flap",bag,new[]{P(-.16f,.15f),P(.16f,.15f),P(.13f,-.03f),P(0,-.09f),P(-.13f,-.03f)},-.142f,.035f,.1f,C("7A6043"),Finish.Leather);Stud(bag,V(0,-.02f,-.17f));
            Stitch(bag,V(-.13f,.11f,-.169f),V(.13f,.11f,-.169f),8,C("B3A075"));
        }
        static void Collar(Transform body)
        {
            for(int j=0;j<3;j++)Sweep("Folded red scarf collar",body,new[]{V(0,1.57f+j*.065f,.01f),V(0,1.62f+j*.065f,.01f)},new[]{P(.27f-j*.015f,.245f),P(.27f-j*.015f,.245f)},j==1?C("A25244"):red,Finish.Cloth,32,1,.025f);
            var cape=N("ScarfTail",body,V(0,0,.23f));cape.localEulerAngles=V(5,0,-5);
            Drape("Folded shoulder mantle",cape,1.65f,.65f,.28f,.55f,.01f,red,7,.08f);
            Curve("Mantle hem stitching",cape,C("A17453"),Finish.Cloth,.012f,V(-.53f,.67f,.16f),V(-.27f,.68f,.18f),V(0,.62f,.22f),V(.28f,.68f,.18f),V(.5f,.71f,.16f));
        }
        public static GameObject Character(int kind,int weapon)
        {
            var root=Models.Root(kind<0?"Lantern warden — sculpted adventurer":Catalog.EnemyNames[kind]);var rig=N("Rig",root.transform);rig.localScale=Vector3.one*(kind>=4?1.2f:.88f);
            if(kind==3){Hound(rig);return root;}if(kind==5){Gardener(rig);return root;}if(kind==6){ClockLord(rig);return root;}
            bool armor=kind==1||kind==4,boss=kind==4;var body=N("Body",rig);
            Torso(body,armor,boss);
            for(int s=-1;s<=1;s+=2){var leg=N(s<0?"LegL":"LegR",rig,V(s*.22f,.82f));Boot(leg,s,armor);BindLowerLeg(leg);var arm=N(s<0?"ArmL":"ArmR",body,V(s*.43f,1.52f));Arm(arm,s,armor,boss);}
            var head=N("Head",body,V(0,1.94f,-.02f));
            if(kind<0){YouthFace(head);Hood(head);Collar(body);Lantern(body.Find("ArmL"),V(-.015f,-1.12f,-.1f),.6f);}
            else if(armor){Helmet(head,boss);Shield(body.Find("ArmL"),boss);if(boss){body.localScale=V(1.35f,1.25f,1.2f);Collar(body);Crown(head);Regalia(body);}}
            else {YouthFace(head,true);if(kind==2)Moth(body,head);else Revenant(body,head);}
            var w=Weapon(body.Find("ArmR"),kind<0?weapon:armor?1:kind==2?2:0);w.transform.localPosition=V(.02f,-.65f,-.16f);w.transform.localEulerAngles=V(-8,0,-26);
            var forearm=body.Find("ArmR/Forearm");if(forearm)w.transform.SetParent(forearm,true);
            var lamp=body.Find("ArmL/Lantern");var leftForearm=body.Find("ArmL/Forearm");if(lamp&&leftForearm)lamp.SetParent(leftForearm,true);
            return root;
        }
        static void Helmet(Transform h,bool boss)
        {
            Sweep("Forged bell helmet",h,new[]{V(0,-.29f),V(0,-.12f),V(0,.18f),V(0,.43f),V(0,.5f)},new[]{P(.32f,.29f),P(.39f,.32f),P(.38f,.33f),P(.26f,.25f),P(.08f,.09f)},steel,Finish.Iron,32,5);
            Panel("Deep recessed visor",h,new[]{P(-.32f,.12f),P(.32f,.12f),P(.27f,-.005f),P(-.27f,-.005f)},-.33f,.05f,.09f,ink,Finish.Iron);
            for(int s=-1;s<=1;s+=2){Curve("Narrow amber eyes",h,C("D8BE7A"),Finish.Glow,.018f,V(s*.075f,.055f,-.37f),V(s*.235f,.055f,-.354f));
                Panel("Bevelled cheek guard",h,new[]{P(s*.09f,-.025f),P(s*.3f,.015f),P(s*.34f,-.21f),P(s*.12f,-.31f)},-.305f,.07f,.13f,bronze,Finish.Brass);
                for(int i=0;i<3;i++)Block("Breather aperture",h,V(s*(.15f+i*.052f),-.14f,-.346f),V(.014f,.062f,.008f),ink,Finish.Iron);
                Ring("Visor hinge",h,V(s*.37f,.085f,-.035f),.065f,.017f,bronze,Finish.Brass,V(0,90,0));
            }
            Panel("Raised brow and nasal",h,new[]{P(-.31f,.16f),P(-.04f,.18f),P(-.035f,.42f),P(.035f,.42f),P(.04f,.18f),P(.31f,.16f),P(.3f,.12f),P(.04f,.1f),P(.045f,-.22f),P(-.045f,-.22f),P(-.04f,.1f),P(-.3f,.12f)},-.37f,.05f,.08f,gold,Finish.Brass);
            Curve("Rolled gorget edge",h,bronze,Finish.Brass,.025f,V(-.29f,-.26f,-.13f),V(-.18f,-.31f,-.29f),V(0,-.32f,-.32f),V(.18f,-.31f,-.29f),V(.29f,-.26f,-.13f));
            if(!boss){Sweep("Swept plume",h,new[]{V(0,.45f),V(.025f,.68f,.04f),V(.05f,.75f,.25f),V(.08f,.58f,.49f)},new[]{P(.055f,.07f),P(.075f,.1f),P(.065f,.09f),P(.008f,.015f)},red,Finish.Cloth,12,5,.08f);}
        }
        static void Regalia(Transform body)
        {
            // The bridge warden carries a great bell frame and a ceremonial chain, unlike the foot soldier.
            for(int side=-1;side<=1;side+=2){
                var q=N("Warden fluted shoulder crest",body.Find(side<0?"ArmL":"ArmR"),V(side*.05f,.09f));
                for(int j=0;j<3;j++)Panel("Cast shoulder fin",q,new[]{P(side*(-.08f+j*.08f),.04f),P(side*(.05f+j*.08f),.36f+j*.04f),P(side*(.18f+j*.07f),.26f+j*.03f),P(side*(.22f+j*.07f),-.08f)},.02f+j*.055f,.06f,.1f,j%2==0?bronze:steel,Finish.Iron);
                for(int j=0;j<5;j++){float x=side*(.07f+j*.058f),y=1.48f+Mathf.Pow(j/4f,2)*.12f;Ring("Ceremonial interlocked chain",body,V(x,y,-.34f),.04f,.011f,gold,Finish.Brass,V(0,j%2*65,0));}
                var skirt=N("Warden heavy tabard",body,V(side*.18f,0,-.37f));Drape("Faded brocade tabard",skirt,1.08f,.38f,.13f,.18f,0,C("663E36"),3,.06f);
                Curve("Tabard woven border",skirt,bronze,Finish.Cloth,.012f,V(-.115f,1.05f,-.01f),V(-.15f,.7f,.08f),V(-.17f,.41f,.13f));
            }
            var frame=N("Back-mounted guardian bell",body,V(0,1.35f,.5f));
            Curve("Bell carrying frame",frame,steel,Finish.Iron,.065f,V(-.35f,-.3f),V(-.35f,.8f),V(0,1.02f),V(.35f,.8f),V(.35f,-.3f));
            Sweep("Guardian warning bell",frame,new[]{V(0,.13f),V(0,.21f),V(0,.53f),V(0,.66f)},new[]{P(.28f,.24f),P(.26f,.23f),P(.17f,.15f),P(.06f,.06f)},bronze,Finish.Brass,24,4);
            Ring("Guardian bell lip",frame,V(0,.14f),.27f,.025f,gold,Finish.Brass,V(90,0,0));
        }
        static void Crown(Transform h)
        {
            Ring("Crown base",h,V(0,.38f),.3f,.035f,bronze,Finish.Brass,V(90,0,0));
            for(int i=0;i<7;i++){float a=i*2*Mathf.PI/7;float x=Mathf.Cos(a),z=Mathf.Sin(a);Sweep("Cast crown fin",h,new[]{V(x*.26f,.36f,z*.26f),V(x*.38f,.62f,z*.38f),V(x*.43f,.84f,z*.43f)},new[]{P(.07f,.045f),P(.05f,.035f),P(.005f,.008f)},gold,Finish.Brass,8,4);}
        }
        static void Shield(Transform arm,bool boss)
        {
            var q=N("Layered heraldic kite shield",arm,V(-.11f,-.39f,-.3f));q.localEulerAngles=V(-8,-8,5);q.localScale=Vector3.one*(boss?1.14f:1);
            var points=new[]{P(-.38f,.36f),P(0,.56f),P(.38f,.36f),P(.32f,-.19f),P(0,-.56f),P(-.32f,-.19f)};
            Panel("Oak shield backing",q,points,0,.15f,.12f,C("4A3E32"),Finish.Wood);
            Panel("Rolled bronze shield rim",q,points,-.09f,.08f,.1f,bronze,Finish.Brass);
            Panel("Inset blue enamel field",q,new[]{P(-.31f,.31f),P(0,.47f),P(.31f,.31f),P(.26f,-.17f),P(0,-.45f),P(-.26f,-.17f)},-.144f,.055f,.15f,teal,Finish.Iron);
            Panel("Central forged keel",q,new[]{P(-.035f,.44f),P(.035f,.44f),P(.038f,-.34f),P(0,-.44f),P(-.038f,-.34f)},-.183f,.035f,.2f,gold,Finish.Brass);
            Ring("Fluted shield boss ring",q,V(0,.09f,-.21f),.14f,.023f,gold);Form("Conical shield boss",q,V(0,.09f,-.222f),V(.108f,.108f,.095f),bronze,Finish.Brass);
            for(int i=0;i<6;i++){var v=points[i];Stud(q,V(v.x*.9f,v.y*.9f,-.152f),.025f);}
            Curve("Left heraldic branch",q,gold,Finish.Brass,.016f,V(-.09f,-.24f,-.185f),V(-.16f,-.09f,-.185f),V(-.15f,.21f,-.185f),V(-.22f,.29f,-.185f));
            Curve("Right heraldic branch",q,gold,Finish.Brass,.016f,V(.09f,-.24f,-.185f),V(.16f,-.09f,-.185f),V(.15f,.21f,-.185f),V(.22f,.29f,-.185f));
            for(int j=0;j<4;j++)Curve("Shield wear cut",q,C("6D8985"),Finish.Iron,.008f,V(-.22f+j*.11f,-.21f+j*.11f,-.18f),V(-.18f+j*.11f,-.18f+j*.11f,-.18f));
        }
        static void Revenant(Transform body,Transform head)
        {
            var c=N("ScarfTail",body,V(0,0,.22f));Drape("Ragged funeral mantle",c,1.62f,.45f,.26f,.64f,.04f,C("4A5B61"),7,.16f);
            for(int side=-1;side<=1;side+=2){Sweep("Curving broken antler",head,new[]{V(side*.29f,.24f,.03f),V(side*.44f,.46f,.09f),V(side*.5f,.72f,.12f),V(side*.39f,.9f,.14f)},new[]{P(.075f,.06f),P(.065f,.05f),P(.04f,.03f),P(.005f,.005f)},C("5A6F65"),Finish.Wood,12,5,.07f);}
            for(int i=0;i<5;i++){float x=(i-2)*.09f;Ring("Funeral chain link",body,V(x,1.54f-Mathf.Sin(i*Mathf.PI/4)*.12f,-.32f),.037f,.009f,bronze);}
        }
        static void Moth(Transform body,Transform head)
        {
            for(int s=-1;s<=1;s+=2){
                Curve("Feathered moth antenna",head,C("857A67"),Finish.Wood,.022f,V(s*.22f,.34f,.1f),V(s*.34f,.69f,.1f),V(s*.58f,.82f,.1f));
                for(int j=0;j<6;j++)Curve("Antenna barb",head,bronze,Finish.Wood,.009f,V(s*(.31f+j*.04f),.62f+j*.03f,.1f),V(s*(.29f+j*.05f),.77f+j*.025f,.12f));
                var wing=N(s<0?"WingL":"WingR",body,V(s*.26f,1.43f,.19f));
                for(int lobe=0;lobe<2;lobe++){
                    var q=N("Veined wing lobe",wing);q.localScale=V(s,1,1);
                    var contour=lobe==0?new[]{P(0,0),P(.16f,.55f),P(.65f,.98f),P(1.12f,.82f),P(1.23f,.5f),P(.97f,.13f),P(.48f,-.07f)}:new[]{P(0,0),P(.57f,-.01f),P(.9f,-.26f),P(.77f,-.69f),P(.45f,-.88f),P(.14f,-.58f)};
                    Panel("Thick folded wing membrane",q,contour,.04f,.085f,.08f,lobe==0?C("62566E"):C("414D5D"));
                    foreach(var v in contour)Curve("Wing structural vein",q,C("9A896E"),Finish.Cloth,.012f,V(.08f,-.05f,-.01f),V(v.x*.65f,v.y*.65f,-.025f),V(v.x*.94f,v.y*.94f,-.014f));
                    if(lobe==0){Form("Raised wing eye marking",q,V(.72f,.48f,-.018f),V(.22f,.26f,.025f),C("A19470"));Form("Wing eye inner marking",q,V(.72f,.48f,-.045f),V(.14f,.18f,.017f),C("2D3B43"));Form("Wing eye crescent",q,V(.75f,.51f,-.065f),V(.065f,.11f,.015f),C("8CA393"));}
                }
            }
            var cape=N("ScarfTail",body,V(0,0,.25f));Drape("Moth robe train",cape,1.58f,.28f,.21f,.35f,.04f,C("4F4662"),5,.1f);
        }
        public static GameObject Weapon(Transform parent,int type)
        {
            var root=Models.Root("Weapon",parent);var p=root.transform;
            Sweep("Octagonal leather grip",p,new[]{V(0,-.2f),V(0,.25f)},new[]{P(.058f,.06f),P(.058f,.06f)},leather,Finish.Leather,12,1);
            for(int i=0;i<7;i++)Curve("Grip spiral binding",p,C("978468"),Finish.Leather,.009f,V(-.054f,-.17f+i*.055f,-.04f),V(0,-.15f+i*.055f,-.062f),V(.054f,-.13f+i*.055f,-.04f));
            if(type==0){
                Panel("Swept forged blade",p,new[]{P(-.075f,.25f),P(-.115f,.69f),P(-.075f,1.12f),P(.05f,1.53f),P(.3f,1.79f),P(.2f,1.31f),P(.13f,.78f),P(.11f,.25f)},0,.095f,.12f,C("7D9799"),Finish.Iron);
                Panel("Honed cutting bevel",p,new[]{P(.077f,.28f),P(.097f,.8f),P(.162f,1.32f),P(.3f,1.79f),P(.205f,1.25f),P(.14f,.78f),P(.11f,.28f)},-.053f,.018f,.1f,C("CBD7C5"),Finish.Iron);
                Curve("Recessed blade fuller",p,C("3F656D"),Finish.Iron,.012f,V(-.015f,.39f,-.055f),V(-.02f,.82f,-.056f),V(.02f,1.14f,-.056f),V(.11f,1.46f,-.056f));
                for(int i=0;i<4;i++)Curve("Etched blade ward",p,C("B2BFA6"),Finish.Iron,.007f,V(-.045f,.47f+i*.12f,-.058f),V(.012f,.5f+i*.12f,-.058f),V(-.015f,.54f+i*.12f,-.058f));
                Sweep("Curved guard quillons",p,new[]{V(-.32f,.33f),V(-.22f,.25f),V(0,.23f),V(.24f,.27f),V(.3f,.37f)},new[]{P(.03f,.04f),P(.035f,.05f),P(.07f,.08f),P(.035f,.05f),P(.03f,.04f)},bronze,Finish.Brass,12,4);
                Form("Faceted pommel",p,V(0,-.24f),V(.095f,.11f,.075f),bronze,Finish.Brass);Ring("Pommel stone bezel",p,V(0,-.24f,-.072f),.047f,.014f,gold);Form("Pommel teal stone",p,V(0,-.24f,-.078f),V(.035f,.035f,.018f),teal,Finish.Ceramic);
            }else if(type==1){
                Sweep("Carved ash hammer haft",p,new[]{V(0,.19f),V(0,.67f),V(.01f,1.1f)},new[]{P(.061f,.065f),P(.073f,.07f),P(.068f,.07f)},C("66503C"),Finish.Wood,16,3,.04f);
                for(int i=0;i<3;i++)Ring("Haft reinforcement",p,V(0,.75f+i*.11f),.078f,.018f,bronze,Finish.Brass,V(90,0,0));
                var bell=N("Hollow bell hammer",p,V(0,1.17f));bell.localEulerAngles=V(0,0,90);
                Sweep("Bell cast body",bell,new[]{V(0,-.39f),V(0,-.32f),V(0,-.17f),V(0,.18f),V(0,.33f)},new[]{P(.38f,.33f),P(.39f,.34f),P(.3f,.26f),P(.23f,.21f),P(.1f,.1f)},bronze,Finish.Brass,32,4);
                Sweep("Deep bell mouth",bell,new[]{V(0,-.405f),V(0,-.391f)},new[]{P(.32f,.28f),P(.32f,.28f)},ink,Finish.Iron,32,1);
                Ring("Bell rolled mouth",bell,V(0,-.4f),.36f,.036f,gold,Finish.Brass,V(90,0,0));
                Ring("Cast inscription band",bell,V(0,-.17f),.306f,.022f,teal,Finish.Iron,V(90,0,0));
                Form("Exposed bell clapper",bell,V(0,-.43f),V(.105f,.12f,.105f),steel,Finish.Iron);
                for(int j=0;j<8;j++){float a=j*Mathf.PI/4;float x=Mathf.Cos(a),z=Mathf.Sin(a);Curve("Bell relief rib",bell,gold,Finish.Brass,.015f,V(x*.33f,-.3f,z*.29f),V(x*.29f,-.17f,z*.25f),V(x*.23f,.15f,z*.21f));Stud(bell,V(x*.265f,.05f,z*.245f));}
            }else{
                Sweep("Twisted carved staff",p,new[]{V(0,-.35f),V(-.035f,.4f),V(.03f,.95f),V(0,1.35f)},new[]{P(.045f,.05f),P(.055f,.055f),P(.07f,.06f),P(.065f,.06f)},C("65533C"),Finish.Wood,16,4,.045f);
                for(int i=0;i<4;i++)Ring("Astrolabe stem collar",p,V(0,1.05f+i*.08f),.077f,.017f,gold,Finish.Brass,V(90,0,0));
                Ring("Outer armillary sphere",p,V(0,1.62f),.36f,.026f,bronze);Ring("Inner tilted orbit",p,V(0,1.62f),.27f,.019f,gold,Finish.Brass,V(22,57,30));
                for(int i=0;i<12;i++){float a=i*Mathf.PI/6;Curve("Astrolabe division",p,gold,Finish.Brass,.011f,V(Mathf.Sin(a)*.32f,1.62f+Mathf.Cos(a)*.32f),V(Mathf.Sin(a)*.36f,1.62f+Mathf.Cos(a)*.36f));}
                Sweep("Suspended cut crystal",p,new[]{V(0,1.34f),V(0,1.56f),V(0,1.72f),V(0,1.91f)},new[]{P(.001f,.001f),P(.115f,.1f),P(.1f,.09f),P(.001f,.001f)},C("87C5BB"),Finish.Ceramic,6,1);
                Curve("Crystal suspension chain",p,bronze,Finish.Brass,.009f,V(0,1.92f),V(0,1.99f));Form("Crystal core glow",p,V(0,1.63f,-.09f),V(.035f,.06f,.02f),C("BCE0BF"),Finish.Glow);
            }
            return root;
        }
        public static GameObject Lantern(Transform p,Vector3 position,float scale)
        {
            var r=Models.Root("Lantern",p,position);r.transform.localScale=Vector3.one*scale;var q=r.transform;
            Sweep("Lantern plinth moulding",q,new[]{V(0,0),V(0,.055f),V(0,.1f),V(0,.13f)},new[]{P(.13f,.13f),P(.22f,.22f),P(.22f,.22f),P(.18f,.18f)},bronze,Finish.Brass,8,1);
            Sweep("Domed copper cap",q,new[]{V(0,.47f),V(0,.52f),V(0,.61f),V(0,.65f)},new[]{P(.23f,.23f),P(.2f,.2f),P(.09f,.09f),P(.05f,.05f)},bronze,Finish.Brass,8,2);
            Form("Inner warm glass",q,V(0,.3f),V(.14f,.19f,.14f),C("E0BC76"),Finish.Glow);
            for(int i=0;i<6;i++){float a=i*Mathf.PI/3;var at=V(Mathf.Sin(a)*.18f,0,Mathf.Cos(a)*.18f);Curve("Cast lantern rib",q,bronze,Finish.Brass,.018f,at+V(0,.11f),at+V(0,.29f),at+V(0,.48f));Stud(q,at+V(0,.49f),.018f);}
            Ring("Lantern carrying loop",q,V(0,.72f),.09f,.018f,bronze);
            return r;
        }
        static void Hound(Transform rig)
        {
            var body=N("Body",rig,V(0,.72f));
            Sweep("Arched sinewed torso",body,new[]{V(-.66f,-.05f),V(-.25f,.06f),V(.27f,.1f),V(.56f,.03f)},new[]{P(.2f,.24f),P(.27f,.3f),P(.29f,.34f),P(.23f,.25f)},C("364D55"),Finish.Leather,24,6,.035f);
            for(int i=0;i<4;i++){
                int side=i%2==0?-1:1;float x=i<2?-.48f:.46f;var leg=N("Leg"+i,rig,V(x,.74f,side*.22f));
                Sweep("Canine muscle and hock",leg,new[]{V(0,0),V(i<2?-.12f:.08f,-.22f),V(i<2?.05f:-.08f,-.48f),V(.1f,-.63f)},new[]{P(.13f,.14f),P(.1f,.11f),P(.055f,.06f),P(.07f,.08f)},C("4D6567"),Finish.Leather,16,5,.04f);
                Form("Splayed canine paw",leg,V(.14f,-.66f,-.015f),V(.15f,.075f,.12f),C("263C42"),Finish.Leather);
                for(int j=0;j<3;j++)Sweep("Individual ivory claw",leg,new[]{V(.22f,-.66f,-.08f+j*.07f),V(.32f,-.68f,-.08f+j*.07f)},new[]{P(.025f,.027f),P(.001f,.001f)},bone,Finish.Ceramic,8,3);
            }
            var h=N("Head",body,V(.69f,.15f,-.025f));h.localEulerAngles=V(0,-8,-8);
            Form("Porcelain canine cranium",h,V(0,.05f),V(.34f,.27f,.24f),bone,Finish.Ceramic,.035f);
            Sweep("Long tapered muzzle",h,new[]{V(.1f,-.035f),V(.35f,-.08f),V(.5f,-.09f)},new[]{P(.15f,.17f),P(.09f,.12f),P(.065f,.085f)},C("B3BCA7"),Finish.Ceramic,18,4);
            Sweep("Lower jaw",h,new[]{V(.05f,-.14f),V(.26f,-.23f),V(.46f,-.19f)},new[]{P(.08f,.13f),P(.05f,.09f),P(.025f,.075f)},C("8E9D8E"),Finish.Ceramic,16,4);
            Form("Black nose",h,V(.5f,-.08f),V(.07f,.07f,.09f),ink,Finish.Leather);
            for(int s=-1;s<=1;s+=2){
                Form("Deep canine eye socket",h,V(.1f,.11f,s*.216f),V(.08f,.055f,.023f),ink);Form("Narrow golden eye",h,V(.12f,.11f,s*.237f),V(.047f,.022f,.012f),C("C7B26F"),Finish.Glow);
                Sweep("Tapered wolf ear",h,new[]{V(-.17f,.2f,s*.16f),V(-.23f,.44f,s*.17f),V(-.31f,.56f,s*.2f)},new[]{P(.095f,.075f),P(.06f,.04f),P(.002f,.002f)},C("536B6A"),Finish.Leather,12,4);
                for(int i=0;i<4;i++)Sweep("Separate muzzle tooth",h,new[]{V(.15f+i*.08f,-.12f,s*(.12f-i*.012f)),V(.18f+i*.08f,-.22f,s*(.12f-i*.012f))},new[]{P(.023f,.025f),P(.001f,.001f)},bone,Finish.Ceramic,6,2);
            }
            for(int i=0;i<6;i++){float x=-.55f+i*.2f;Panel("Overlapping dorsal porcelain scute",body,new[]{P(x-.14f,.22f),P(x-.12f,.41f),P(x+.06f,.48f),P(x+.13f,.2f)},0,.28f,.15f,bone,Finish.Ceramic);}
            Sweep("Whip tail with bony tip",body,new[]{V(-.63f,.02f,.02f),V(-1.02f,.24f,.05f),V(-1.24f,.5f,.09f),V(-1.13f,.69f,.1f)},new[]{P(.09f,.1f),P(.07f,.07f),P(.04f,.04f),P(.005f,.005f)},C("455E62"),Finish.Leather,16,6);
        }
        static void Branch(Transform p,string name,Vector3[] path,float radius,Color c)
        {var rr=new Vector2[path.Length];for(int i=0;i<rr.Length;i++){float r=Mathf.Lerp(radius,.008f,i/(float)(rr.Length-1));rr[i]=P(r,r*.82f);}Sweep(name,p,path,rr,c,Finish.Wood,14,6,.11f);}
        static void Leaf(Transform p,Vector3 pos,float size,float rot,Color c)
        {
            var q=N("Sculpted leaf",p,pos);q.localScale=Vector3.one*size;q.localEulerAngles=V(-15,rot,rot*.2f);
            Panel("Curved leaf blade",q,new[]{P(0,0),P(-.15f,.15f),P(-.21f,.35f),P(-.12f,.56f),P(0,.72f),P(.13f,.53f),P(.21f,.31f),P(.13f,.13f)},0,.035f,.15f,c);
            Curve("Leaf midrib",q,C("8B966D"),Finish.Wood,.009f,V(0,0,-.023f),V(0,.3f,-.025f),V(0,.66f,-.023f));
        }
        static void Gardener(Transform rig)
        {
            var b=N("Body",rig);
            Sweep("Gnarled hollow elder trunk",b,new[]{V(0,.15f),V(-.12f,.55f),V(.07f,1.15f),V(-.03f,1.65f),V(0,2.05f),V(.08f,2.36f)},new[]{P(.76f,.48f),P(.52f,.4f),P(.49f,.36f),P(.7f,.43f),P(.45f,.33f),P(.25f,.25f)},C("554F3B"),Finish.Wood,32,6,.09f);
            for(int i=0;i<9;i++){
                float a=i*Mathf.PI*2/9,x=Mathf.Cos(a),z=Mathf.Sin(a);
                Branch(b,"Buttress root",new[]{V(x*.36f,.9f,z*.26f),V(x*.62f,.26f,z*.45f),V(x*.97f,.07f,z*.75f),V(x*1.22f,.045f,z*.89f)},.2f,C(i%2==0?"626044":"454936"));
                Branch(b,"Deep trunk bark ridge",new[]{V(x*.58f,.18f,z*.4f),V(x*.42f,.72f,z*.34f),V(x*.5f,1.28f,z*.36f),V(x*.52f,1.8f,z*.37f),V(x*.25f,2.22f,z*.22f)},.065f,C("736A4C"));
            }
            var h=N("Head",b,V(0,1.92f,-.39f));h.localScale=V(1.22f,1.22f,1.22f);YouthFace(h,true);
            for(int side=-1;side<=1;side+=2){
                var arm=N(side<0?"ArmL":"ArmR",b,V(side*.64f,1.74f,.035f));
                Branch(arm,"Twisted branch arm",new[]{V(0,0),V(side*.3f,-.15f),V(side*.49f,-.5f,-.07f),V(side*.42f,-.8f,-.16f)},.19f,C("605A40"));
                for(int f=0;f<4;f++)Branch(arm,"Jointed wooden talon",new[]{V(side*.43f,-.69f,-.12f+(f-1.5f)*.08f),V(side*(.44f+(f-1.5f)*.06f),-.94f,-.2f),V(side*(.32f+(f-1.5f)*.09f),-1.11f,-.29f),V(side*(.25f+(f-1.5f)*.09f),-1.06f,-.37f)},.048f,C("948365"));
                for(int j=0;j<3;j++){
                    Vector3 a=V(side*(.23f+j*.15f),2.1f-j*.1f,.04f+j*.13f),tip=V(side*(.45f+j*.39f),2.95f-j*.14f,.1f+j*.17f);
                    Branch(b,"Crooked crown branch",new[]{a,V(side*(.45f+j*.25f),2.51f,.08f+j*.13f),tip,tip+V(-side*.17f,.32f,.07f)},.12f,C("71694B"));
                    Branch(b,"Split twig",new[]{Vector3.Lerp(a,tip,.65f),tip+V(side*.27f,-.03f,-.08f),tip+V(side*.38f,.16f,-.04f)},.048f,C("746C4C"));
                    for(int l=0;l<3;l++)Leaf(b,tip+V((l-1)*.17f,l*.08f,0),.55f+l*.09f,side*30+l*49,C(l%2==0?"547B5B":"7E8C5D"));
                }
            }
            for(int i=0;i<5;i++){float x=(i-2)*.15f;Form("Shelf fungus",b,V(x,.85f+i*.13f,-.37f),V(.16f,.045f,.1f),C(i%2==0?"938F6D":"7C845E"),Finish.Wood);}
            Ring("Carved bark knot",b,V(-.3f,1.25f,-.38f),.13f,.025f,C("80754F"),Finish.Wood,V(0,0,-20));
        }
        public static void Gear(Transform p,Vector3 position,float radius,int teeth,Color color)
        {
            var q=N("Machined gear",p,position);var outer=new List<Vector3>();var tri=new List<int>();int count=teeth*4;
            for(int layer=0;layer<2;layer++)for(int ring=0;ring<2;ring++)for(int i=0;i<count;i++){
                float a=i*Mathf.PI*2/count;float r=ring==0?radius*((i%4==1||i%4==2)?1:.89f):radius*.67f;
                outer.Add(V(Mathf.Cos(a)*r,Mathf.Sin(a)*r,(layer==0?-.045f:.045f)*Mathf.Max(1,radius)));}
            for(int i=0;i<count;i++){int j=(i+1)%count;Quad(tri,i,count+i,count+j,j);Quad(tri,2*count+i,2*count+j,3*count+j,3*count+i);Quad(tri,i,j,2*count+j,2*count+i);Quad(tri,count+i,3*count+i,3*count+j,count+j);}
            Part("Solid gear teeth and rim",q,Mesh("Cut gear",outer,tri),color,Finish.Brass);
            Ring("Gear bevel highlight",q,V(0,0,-.05f),radius*.7f,.012f,gold);
            for(int i=0;i<5;i++){float a=i*Mathf.PI*2/5;Curve("Gear spoke",q,color,Finish.Brass,radius*.035f,Vector3.zero,V(Mathf.Cos(a)*radius*.73f,Mathf.Sin(a)*radius*.73f));}
            Ring("Bearing race",q,V(0,0,-.01f),radius*.16f,radius*.035f,steel,Finish.Iron);
        }
        static void ClockLord(Transform rig)
        {
            var b=N("Body",rig);
            var h=N("Head",b,V(0,1.91f));
            // A deep brass housing, three concentric rims, separate inner escapement and dial.
            Form("Clockwork torso housing",h,V(0,0,.19f),V(.91f,1.02f,.39f),steel,Finish.Iron);
            Ring("Outer clock bezel",h,V(0,0,-.12f),.88f,.075f,bronze);
            Ring("Recessed inner clock bezel",h,V(0,0,-.18f),.73f,.035f,gold);
            Sweep("Recessed midnight dial",h,new[]{V(0,0,-.17f),V(0,0,-.2f)},new[]{P(.7f,.7f),P(.7f,.7f)},ink,Finish.Iron,48,1);
            Ring("Ivory chapter ring",h,V(0,0,-.245f),.61f,.024f,bone,Finish.Ceramic);
            for(int i=0;i<12;i++){float a=i*Mathf.PI/6;var mark=Block("Inset hour index",h,V(Mathf.Sin(a)*.65f,Mathf.Cos(a)*.65f,-.23f),V(.025f,i%3==0?.11f:.06f,.023f),gold,Finish.Brass);mark.transform.localEulerAngles=V(0,0,-i*30);Stud(h,V(Mathf.Sin(a)*.85f,Mathf.Cos(a)*.85f,-.16f),.027f);}
            Gear(h,V(-.22f,-.15f,-.23f),.27f,14,bronze);Gear(h,V(.23f,.18f,-.215f),.21f,12,C("788D88"));Gear(h,V(.3f,-.28f,-.24f),.18f,10,bronze);
            Curve("Forged minute hand",h,bone,Finish.Ceramic,.026f,V(-.05f,-.1f,-.34f),V(0,0,-.34f),V(.36f,.42f,-.34f));
            Curve("Forged hour hand",h,gold,Finish.Brass,.035f,V(.05f,-.04f,-.35f),V(0,0,-.35f),V(-.3f,.17f,-.35f));Form("Central jewel bearing",h,V(0,0,-.38f),V(.07f,.07f,.035f),C("7AB5AA"),Finish.Ceramic);
            for(int s=-1;s<=1;s+=2){
                var arm=N(s<0?"ArmL":"ArmR",b,V(s*.86f,2.04f,.05f));
                Form("Segmented shoulder housing",arm,V(s*.06f,-.01f),V(.26f,.3f,.3f),bronze,Finish.Brass);
                Gear(arm,V(s*.09f,-.04f,-.3f),.2f,12,steel);
                Sweep("Mechanical upper arm",arm,new[]{V(s*.1f,-.16f),V(s*.34f,-.43f)},new[]{P(.13f,.14f),P(.1f,.1f)},steel,Finish.Iron,16,3);
                Ring("Elbow bearing",arm,V(s*.36f,-.47f,-.02f),.13f,.035f,gold);
                Sweep("Armoured pendulum forearm",arm,new[]{V(s*.36f,-.48f),V(s*.43f,-.72f),V(s*.31f,-1.02f)},new[]{P(.11f,.12f),P(.18f,.16f),P(.11f,.1f)},bronze,Finish.Brass,16,4);
                for(int j=0;j<3;j++)Sweep("Mechanical pincer digit",arm,new[]{V(s*.29f,-.99f,-.1f+j*.1f),V(s*(.44f+(j-1)*.08f),-1.25f,-.12f),V(s*(.22f+(j-1)*.08f),-1.4f,-.2f)},new[]{P(.055f,.055f),P(.04f,.04f),P(.009f,.009f)},steel,Finish.Iron,10,4);
                var skirt=N("Articulated skirt panel",b,V(s*.35f,0,.17f));Drape("Worn ceremonial banner",skirt,1.12f,.13f,.14f,.26f,.01f,C("574339"),4,.11f);
                Curve("Crown winding tube",h,bronze,Finish.Brass,.055f,V(s*.57f,.7f,.08f),V(s*.72f,1.02f,.08f),V(s*.52f,1.21f,.07f),V(s*.35f,1.12f,.04f));
            }
            Curve("Long pendulum shaft",b,bronze,Finish.Brass,.035f,V(0,1.16f,-.07f),V(0,.45f,-.07f));
            Gear(b,V(0,.38f,-.07f),.24f,14,gold);
            for(int i=0;i<3;i++)Ring("Rear spring coil",h,V(0,0,.5f+i*.06f),.53f-i*.07f,.025f,bronze);
        }
        public static void Optimize(GameObject root)
        {
            var groups=new Dictionary<string,List<MeshFilter>>();var anchors=new Dictionary<string,Transform>();var sourceMaterials=new Dictionary<string,Material>();
            foreach(var mf in root.GetComponentsInChildren<MeshFilter>()){
                var renderer=mf.GetComponent<MeshRenderer>();if(!renderer||!renderer.enabled)continue;
                var anchor=mf.transform.parent;
                while(anchor&&anchor!=root.transform&&anchor.name!="Rig"&&anchor.name!="Body"&&anchor.name!="Head"&&!anchor.name.StartsWith("Arm")&&!anchor.name.StartsWith("Leg")&&!anchor.name.StartsWith("Wing")&&anchor.name!="ScarfTail"&&anchor.name!="Lantern"&&anchor.name!="Forearm"&&anchor.name!="Shin")anchor=anchor.parent;
                if(!anchor)anchor=root.transform;var mat=renderer.sharedMaterial;
                string key=anchor.GetInstanceID()+"_"+mat.GetFloat("_Metallic")+"_"+mat.GetFloat("_Smoothness")+"_"+mat.GetFloat("_SurfaceType")+"_"+mat.GetColor("_EmissionColor");
                if(!groups.ContainsKey(key)){groups[key]=new List<MeshFilter>();anchors[key]=anchor;sourceMaterials[key]=mat;}groups[key].Add(mf);
            }
            foreach(var entry in groups){
                var anchor=anchors[entry.Key];var vv=new List<Vector3>();var nn=new List<Vector3>();var cc=new List<Color>();var tt=new List<int>();
                foreach(var mf in entry.Value){int offset=vv.Count;var m=anchor.worldToLocalMatrix*mf.transform.localToWorldMatrix;var n=m.inverse.transpose;var sourceMesh=mf.sharedMesh;var v=sourceMesh.vertices;var normals=sourceMesh.normals;var sourceColors=sourceMesh.colors;Color color=mf.GetComponent<Renderer>().sharedMaterial.color;
                    for(int i=0;i<v.Length;i++){vv.Add(m.MultiplyPoint3x4(v[i]));nn.Add(n.MultiplyVector(normals[i]).normalized);cc.Add(color*(sourceColors.Length==v.Length?sourceColors[i]:Color.white));}
                    var indices=sourceMesh.triangles;for(int i=0;i<indices.Length;i+=3){tt.Add(offset+indices[i]);tt.Add(offset+indices[i+(m.determinant<0?2:1)]);tt.Add(offset+indices[i+(m.determinant<0?1:2)]);}mf.GetComponent<Renderer>().enabled=false;
                }
                var mesh=new Mesh{name=anchor.name+" sculpted surfaces"};if(vv.Count>65000)mesh.indexFormat=UnityEngine.Rendering.IndexFormat.UInt32;mesh.SetVertices(vv);mesh.SetNormals(nn);mesh.SetColors(cc);mesh.SetTriangles(tt,0);mesh.RecalculateBounds();
                var g=Models.Root("Sculpt render",anchor);g.AddComponent<MeshFilter>().sharedMesh=mesh;
                var material=new Material(sourceMaterials[entry.Key]){name="Sculpt joint "+sourceMaterials[entry.Key].name,color=Color.white};g.AddComponent<MeshRenderer>().sharedMaterial=material;
            }
        }
    }
}
