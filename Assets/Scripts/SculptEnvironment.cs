using System;
using UnityEngine;
namespace Mist
{
    public static partial class Sculpt
    {
        static void StoneBlock(Transform p,Vector3 pos,Vector3 size,int index,Color color)
        {
            float hue=.9f+(index%7)*.022f;var b=Block("Dressed masonry block",p,pos,size,color*hue,Finish.Stone);b.transform.localEulerAngles=V(0,(index%3-1)*1.8f,(index%5-2)*.45f);
            if(index%5==0&&size.y>.3f)Curve("Stone fracture",p,color*.48f,Finish.Stone,.012f,pos+V(-size.x*.21f,size.y*.4f,-size.z*.506f),pos+V(-size.x*.12f,size.y*.12f,-size.z*.511f),pos+V(-size.x*.19f,-size.y*.1f,-size.z*.512f));
        }
        static void Column(Transform p,Vector3 pos,float height,Color color,int index)
        {
            var q=N("Carved stone pier",p,pos);
            StoneBlock(q,V(0,.11f),V(.85f,.22f,.88f),index,color);
            StoneBlock(q,V(0,.29f),V(.69f,.16f,.72f),index+1,color);
            for(int j=0;j<5;j++)StoneBlock(q,V(0,.39f+(height-.55f)/5*(j+.5f)),V(.5f,(height-.55f)/5-.018f,.56f),index+j,color);
            StoneBlock(q,V(0,height-.12f),V(.7f,.17f,.75f),index+1,color);StoneBlock(q,V(0,height),V(.81f,.11f,.86f),index+4,color);
            Panel("Recessed pier panel",q,new[]{P(-.14f,.56f),P(.14f,.56f),P(.14f,height-.39f),P(-.14f,height-.39f)},-.29f,.025f,.08f,color*.7f,Finish.Stone);
        }
        static void StoneArch(Transform p,Vector3 pos,float width,float height,Color color,int seed)
        {
            var q=N("Load bearing arch",p,pos);float radius=width*.5f,spring=height-radius;
            for(int side=-1;side<=1;side+=2)for(int i=0;i<5;i++)StoneBlock(q,V(side*(radius+.17f),spring/5*(i+.5f)),V(.47f,spring/5-.025f,.8f),i+seed,color);
            const int stones=13;
            for(int i=0;i<stones;i++){
                float a=i*Mathf.PI/stones+.014f,b=(i+1)*Mathf.PI/stones-.014f;
                var pts=new[]{P(Mathf.Cos(a)*radius,spring+Mathf.Sin(a)*radius),P(Mathf.Cos(a)*(radius+.4f),spring+Mathf.Sin(a)*(radius+.4f)),P(Mathf.Cos(b)*(radius+.4f),spring+Mathf.Sin(b)*(radius+.4f)),P(Mathf.Cos(b)*radius,spring+Mathf.Sin(b)*radius)};
                Panel("Individual bevelled voussoir",q,pts,0,.82f,.055f,color*(.9f+(i%4)*.035f),Finish.Stone);
            }
            var stone=Block("Projecting arch keystone",q,V(0,height+.18f,-.07f),V(.4f,.56f,1.04f),color*1.1f,Finish.Stone);
            Panel("Keystone ward carving",stone.transform,new[]{P(-.12f,-.12f),P(0,.14f),P(.12f,-.12f)},-.53f,.025f,.05f,bronze,Finish.Brass);
        }
        static void Fence(Transform p,float x,float z,float length,Color color)
        {
            var q=N("Wrought iron balustrade",p,V(x,0,z));
            for(int i=0;i<7;i++){float xx=(i/6f-.5f)*length;Curve("Forged baluster",q,color,Finish.Iron,.026f,V(xx,.48f),V(xx,1.32f));
                Sweep("Lance finial",q,new[]{V(xx,1.3f),V(xx,1.45f),V(xx,1.59f)},new[]{P(.025f,.025f),P(.06f,.045f),P(.001f,.001f)},bronze,Finish.Iron,8,1);
            }
            Curve("Top rail",q,color,Finish.Iron,.04f,V(-length*.5f,1.24f),V(length*.5f,1.24f));Curve("Bottom rail",q,color,Finish.Iron,.026f,V(-length*.5f,.56f),V(length*.5f,.56f));
            for(int s=-1;s<=1;s+=2)Curve("Forged scroll",q,color,Finish.Iron,.025f,V(0,.62f),V(s*.4f,.79f),V(s*.58f,1.04f),V(s*.4f,1.15f),V(s*.28f,1.02f),V(s*.36f,.94f));
        }
        static void Cask(Transform p,Vector3 pos,int seed)
        {
            var q=N("Coopered individual stave cask",p,pos);q.localEulerAngles=V(0,seed*29,seed%2==0?3:-2);
            for(int i=0;i<14;i++){
                float a=i*Mathf.PI/7,x=Mathf.Cos(a),z=Mathf.Sin(a);var plank=Sweep("Curved oak stave",q,new[]{V(x*.31f,.04f,z*.31f),V(x*.38f,.44f,z*.38f),V(x*.32f,.87f,z*.32f)},new[]{P(.073f,.045f),P(.089f,.05f),P(.075f,.045f)},C(i%3==0?"786346":"65543F"),Finish.Wood,8,6);}
            for(int j=0;j<2;j++){float y=j==0?.17f:.74f;Ring("Riveted iron hoop",q,V(0,y),.354f,.035f,steel,Finish.Iron,V(90,0,0));for(int i=0;i<8;i++){float a=i*Mathf.PI/4;Stud(q,V(Mathf.Cos(a)*.38f,y,Mathf.Sin(a)*.38f),.021f);}}
            for(int i=0;i<5;i++)Block("Inset lid board",q,V((i-2)*.115f,.866f,0),V(.105f,.03f,.53f-Mathf.Abs(i-2)*.06f),C("6D5D44"),Finish.Wood);
        }
        static void RuinTree(Transform p,Vector3 pos,float height,int seed)
        {
            var q=N("Old contorted tree",p,pos);Color bark=C("354B48");
            Branch(q,"Sculpted tapering trunk",new[]{V(0,0),V(.2f,height*.3f),V(-.15f,height*.6f,.2f),V(.4f,height*.85f,.1f),V(.6f,height,.3f)},.48f,bark);
            for(int s=-1;s<=1;s+=2){Branch(q,"Exposed tree root",new[]{V(0,.5f),V(s*.55f,.1f,-.1f),V(s*1.05f,.02f,-.3f)},.22f,bark);for(int j=0;j<3;j++){
                var tip=V(s*(1.4f+j*.55f),height*(.6f+j*.12f),.15f+j*.4f);Branch(q,"Forked mature bough",new[]{V(0,height*(.38f+j*.14f)),V(s*.7f,height*(.57f+j*.12f),.12f),tip,tip+V(s*.3f,.5f)},.18f,bark);
                for(int k=0;k<5;k++)Leaf(q,tip+V((k-2)*.27f,(k%2)*.25f,(k%3)*.16f),.9f+k*.12f,seed*31+k*53,C(k%2==0?"3E6858":"52745A"));}}
        }
        static void CityFacade(Transform p,Vector3 pos,float width,float height,int index)
        {
            var q=N("Three dimensional old city facade",p,pos);Color stone=C(index%2==0?"3C535B":"354B55");
            for(int row=0;row<7;row++)for(int col=0;col<4;col++)StoneBlock(q,V((col-1.5f)*width/4,(row+.5f)*height/7),V(width/4-.035f,height/7-.025f,1),index+row*4+col,stone);
            for(int side=-1;side<=1;side+=2){Column(q,V(side*width*.5f,0,-.1f),height*.85f,stone*1.15f,index);}
            for(int row=0;row<2;row++)for(int col=0;col<2;col++){
                var w=N("Deep arched window",q,V((col-.5f)*width*.42f,height*(.3f+row*.36f),-.55f));
                Panel("Window recess",w,new[]{P(-.37f,-.72f),P(.37f,-.72f),P(.37f,.45f),P(0,.77f),P(-.37f,.45f)},0,.04f,.03f,C("1A303B"),Finish.Stone);
                Curve("Carved window surround",w,stone*1.4f,Finish.Stone,.07f,V(-.44f,-.75f),V(-.44f,.41f),V(0,.87f),V(.44f,.41f),V(.44f,-.75f));
                Curve("Window mullion",w,stone*1.1f,Finish.Stone,.034f,V(0,-.7f,-.03f),V(0,.74f,-.03f));
                if((row+col+index)%3==0){for(int s=-1;s<=1;s+=2)Panel("Recessed amber glazing",w,new[]{P(s*.07f,-.58f),P(s*.29f,-.58f),P(s*.29f,.35f),P(s*.07f,.57f)},-.035f,.01f,.02f,C("9D8451"),Finish.Glow);}
                StoneBlock(w,V(0,-.78f),V(1.02f,.13f,.42f),index,stone*1.2f);
            }
            StoneBlock(q,V(0,height),V(width+.45f,.22f,1.45f),index,stone*1.2f);
            Panel("Gabled roof solid",q,new[]{P(-width*.57f,height+.05f),P(0,height+width*.55f),P(width*.57f,height+.05f)},.65f,2.1f,.035f,C("253F4B"),Finish.Stone);
            for(int j=0;j<5;j++)Curve("Raised roof tile course",q,C("385663"),Finish.Stone,.028f,V(-width*.5f*(1-j/5f),height+.14f+j*width*.09f,-.44f),V(width*.5f*(1-j/5f),height+.14f+j*width*.09f,-.44f));
        }
        public static GameObject Stage(int area)
        {
            var root=Models.Root("Sculpted environment "+Catalog.Areas[area]);var p=root.transform;var random=new System.Random(821+area);Color stone=area==0?C("50666A"):area==1?C("40564D"):C("4E555F");
            Block("Solid arena foundation",p,V(0,-.38f,-2),V(44,.65f,21),stone*.55f,Finish.Stone);
            // Large low-relief slabs keep the combat area readable; their bevels cast real shadows.
            for(int row=0;row<17;row++)for(int col=0;col<19;col++){
                float x=(col-9)*1.65f+(row%2)*.72f,z=(row-9)*1.14f;float h=.065f+(float)random.NextDouble()*.018f;
                var slab=Block("Bevelled paving slab",p,V(x,-h*.5f,z),V(1.61f,h,1.1f),stone*(.69f+random.Next(7)*.02f),Finish.Stone);
                slab.transform.localEulerAngles=V(0,(float)random.NextDouble()*1.2f-.6f,0);
                if((row*19+col)%23==0)Curve("Paving fissure",p,stone*.36f,Finish.Stone,.008f,V(x-.5f,.002f,z-.3f),V(x-.2f,.004f,z+.05f),V(x-.3f,.003f,z+.39f));
            }
            for(int i=0;i<9;i++){
                float x=(i-4)*3.2f,z=5.2f+(i%2)*.12f;
                if(area!=1){Column(p,V(x,0,z),i%2==0?1.95f:1.08f,stone,i);if(i%2==0)Lantern(p,V(x,1.98f,z),.9f);if(i<8){StoneBlock(p,V(x+1.6f,.22f,z),V(2.47f,.44f,.54f),i,stone);Fence(p,x+1.6f,z,2.47f,C("263E43"));}}
                else {if(i%2==0)Column(p,V(x,0,z),.85f+(i%3)*.3f,stone,i);for(int j=0;j<5;j++)Leaf(p,V(x+j*.25f,.02f,z-.2f),.65f+j*.04f,i*31+j*52,C("547251"));}
                if(i==1||i==6){Cask(p,V(x,.025f,4.2f),i);Cask(p,V(x+.62f,.025f,4.6f),i+1);}
            }
            if(area==0){
                CityFacade(p,V(-16,-2,12),4.3f,7.8f,1);CityFacade(p,V(16,-2,13),4.3f,8.4f,3);
                StoneArch(p,V(-10.6f,0,7),3.4f,4.6f,stone,3);StoneArch(p,V(10.7f,0,7.7f),3.4f,4.8f,stone,7);
                for(int s=-1;s<=1;s+=2){var banner=N("Hanging civic banner",p,V(s*10.5f,0,7.2f));Drape("Folded civic cloth",banner,4.15f,2.3f,.42f,.43f,0,teal,6,.05f);Ring("Embroidered ward medallion",banner,V(0,3.35f,-.04f),.22f,.025f,bronze);}
            }else if(area==1){
                for(int i=0;i<4;i++)RuinTree(p,V(i<2?-12-i*2:12+(i-2)*2,0,5.5f+(i%2)*1.2f),5.4f+(i%3),i);
                StoneArch(p,V(-7.8f,0,7),3.3f,4.1f,stone,5);StoneArch(p,V(7.9f,0,8),3.3f,3.9f,stone,2);
                for(int i=0;i<6;i++){var rootPlant=N("Rooted garden border",p,V((i-2.5f)*4.1f,0,5.7f));Branch(rootPlant,"Snaking exposed root",new[]{V(-1.3f,.03f),V(-.6f,.14f,-.1f),V(0,.1f,.12f),V(.7f,.05f,-.08f),V(1.3f,.025f)},.13f,C("585B40"));}
            }else{
                for(int i=0;i<4;i++){float x=i<2?-11-i*3:11+(i-2)*3;Column(p,V(x,-3,10),11.5f,stone*.78f,i);Gear(p,V(x+1.5f,4.1f+(i%3)*1.2f,10.3f),1.7f+(i%2)*.4f,22,bronze*.65f);}
                for(int i=0;i<4;i++){float x=i<2?-10-i*2.5f:10+(i-2)*2.5f;Curve("Riveted copper supply pipe",p,C("6C6350"),Finish.Brass,.16f,V(x,-2,8),V(x,4.3f,8),V(x+.5f,4.8f,8),V(x+1.5f,4.8f,8));for(int j=0;j<5;j++)Ring("Pipe coupling",p,V(x,.6f+j*.7f,8),.18f,.027f,steel,Finish.Iron,V(90,0,0));}
            }
            // Irregular debris and vegetation join the level edge to the deeper environment.
            for(int i=0;i<34;i++){float x=(float)random.NextDouble()*28-14,z=4.9f+(float)random.NextDouble()*.8f;float scale=.25f+(float)random.NextDouble()*.45f;
                var rubble=Block("Fallen masonry",p,V(x,scale*.18f,z),V(scale,scale*.36f,scale*.65f),stone*(.63f+(i%4)*.08f),Finish.Stone);rubble.transform.localEulerAngles=V(i%3*7,i*41,i%4*4);
                if(i%2==0)for(int j=0;j<3;j++)Leaf(p,V(x+j*.14f,.02f,z-.2f),.4f+j*.13f,i*33+j*60,C("4F6851"));
            }
            for(int side=-1;side<=1;side+=2){
                Column(p,V(side*12.2f,0,-4),2.1f,stone*.55f,5);StoneBlock(p,V(side*10.8f,.36f,-5.3f),V(2.3f,.72f,1),5,stone*.55f);StoneBlock(p,V(side*11.4f,.86f,-5.3f),V(1.05f,.36f,.9f),7,stone*.62f);
                for(int j=0;j<8;j++)Leaf(p,V(side*(9.6f+j*.32f),.02f,-5.1f+(j%2)*.2f),.7f+j*.07f,j*37,C("29493F"));
            }
            return root;
        }
    }
}
