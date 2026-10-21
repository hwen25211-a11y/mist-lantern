using UnityEngine;
namespace Mist
{
    public static partial class Sculpt
    {
        public static GameObject KeeperWeapon(Transform parent,int type)
        {
            if(type!=0)return Weapon(parent,type);
            var root=Models.Root("Weapon",parent);var p=root.transform;
            Sweep("Leather wrapped keeper grip",p,new[]{V(0,-.18f),V(0,.23f)},new[]{P(.063f,.063f),P(.063f,.063f)},leather,Finish.Leather,12,1);
            for(int i=0;i<5;i++)Curve("Grip lacing",p,C("8A6A47"),Finish.Leather,.009f,V(-.055f,-.14f+i*.07f,-.038f),V(.02f,-.12f+i*.07f,-.065f),V(.057f,-.10f+i*.07f,-.025f));
            Panel("Cleaver forged spine and broad face",p,new[]{P(-.105f,.24f),P(-.12f,.94f),P(-.065f,1.04f),P(.255f,1.00f),P(.30f,.86f),P(.29f,.65f),P(.24f,.40f),P(.13f,.24f)},0,.075f,.075f,C("676564"),Finish.Iron);
            Panel("Continuous bright cleaver cutting bevel",p,new[]{P(.13f,.24f),P(.24f,.40f),P(.29f,.65f),P(.30f,.86f),P(.255f,1.00f),P(.208f,.98f),P(.252f,.85f),P(.243f,.66f),P(.194f,.42f),P(.095f,.27f)},-.039f,.012f,.025f,C("C5BEA8"),Finish.Iron);
            Panel("Bronze blade collar",p,new[]{P(-.145f,.18f),P(-.15f,.32f),P(-.07f,.35f),P(.07f,.31f),P(.105f,.17f),P(.06f,.13f)},0,.13f,.16f,C("BA8749"),Finish.Brass);
            Stud(p,V(-.04f,.265f,-.074f),.021f);
            for(int side=-1;side<=1;side+=2)for(int i=0;i<3;i++)Curve("Shallow hammered blade mark",p,C("93908A"),Finish.Iron,.004f,V(-.03f+i*.055f,.54f+i*.10f,side*.039f),V(-.005f+i*.055f,.50f+i*.10f,side*.039f));
            Form("Square bronze pommel",p,V(0,-.22f),V(.085f,.075f,.07f),bronze,Finish.Brass);
            return root;
        }
        public static GameObject KeeperLantern(Transform parent,float scale)
        {
            var root=Models.Root("Lantern",parent);root.transform.localScale=Vector3.one*scale;var p=root.transform;
            Color copper=C("BD803A"),rim=C("E7B65C"),dark=C("775026");
            Sweep("Octagonal bottom foot",p,new[]{V(0,.025f),V(0,.075f),V(0,.105f),V(0,.15f)},new[]{P(.10f,.10f),P(.25f,.20f),P(.255f,.205f),P(.225f,.18f)},copper,Finish.Brass,8,1);
            Sweep("Bottom bright rim",p,new[]{V(0,.105f),V(0,.126f)},new[]{P(.257f,.207f),P(.257f,.207f)},rim,Finish.Brass,8,1);
            Sweep("Warm amber inner chamber",p,new[]{V(0,.15f),V(0,.52f)},new[]{P(.182f,.137f),P(.182f,.137f)},C("D89239"),Finish.Glow,8,1);
            Form("Lantern flame core",p,V(0,.33f,-.002f),V(.084f,.15f,.064f),C("F8D88B"),Finish.Glow);
            for(int side=-1;side<=1;side+=2){
                float z=side*.155f;
                Curve("Front and rear cross brace",p,rim,Finish.Brass,.020f,V(-.19f,.155f,z),V(.19f,.515f,z));
                Curve("Front and rear cross brace",p,copper,Finish.Brass,.020f,V(.19f,.155f,z),V(-.19f,.515f,z));
                for(int edge=-1;edge<=1;edge+=2)Curve("Straight corner cage rib",p,copper,Finish.Brass,.019f,V(edge*.19f,.14f,z),V(edge*.19f,.52f,z));
                Curve("Side cross brace",p,copper,Finish.Brass,.018f,V(side*.205f,.155f,-.12f),V(side*.205f,.515f,.12f));
                Curve("Side cross brace",p,rim,Finish.Brass,.018f,V(side*.205f,.155f,.12f),V(side*.205f,.515f,-.12f));
            }
            Sweep("Wide bevelled cap lip",p,new[]{V(0,.515f),V(0,.545f),V(0,.57f)},new[]{P(.255f,.205f),P(.265f,.215f),P(.235f,.19f)},rim,Finish.Brass,8,1);
            Sweep("Faceted copper lantern roof",p,new[]{V(0,.57f),V(0,.64f),V(0,.67f)},new[]{P(.235f,.19f),P(.105f,.085f),P(.08f,.065f)},copper,Finish.Brass,8,1);
            Sweep("Crown carrying socket",p,new[]{V(0,.665f),V(0,.70f)},new[]{P(.055f,.05f),P(.045f,.04f)},dark,Finish.Brass,8,1);
            Curve("Angular forged carrying loop",p,copper,Finish.Brass,.022f,V(-.045f,.695f),V(-.095f,.75f),V(-.09f,.83f),V(-.045f,.875f),V(.045f,.875f),V(.09f,.83f),V(.095f,.75f),V(.045f,.695f));
            for(int side=-1;side<=1;side+=2)Stud(p,V(side*.14f,.576f,-.13f),.022f);
            foreach(var r in root.GetComponentsInChildren<MeshRenderer>()){
                var material=new Material(r.sharedMaterial);material.SetFloat("_CraftStrength",.18f);material.SetFloat("_OutlineWidth",.003f);r.sharedMaterial=material;
            }
            return root;
        }
        public static void KeeperBackGear(Transform parent)
        {
            var roll=Models.Root("Bound rolled travel blanket",parent);roll.transform.position=V(-.17f,1.13f,.60f);roll.transform.localEulerAngles=V(0,-24,8);var p=roll.transform;
            Color canvas=C("A58C68"),cut=C("C7B18B"),seam=C("67523C"),strap=C("654329");
            Sweep("Rolled blanket solid body",p,new[]{V(-.23f,0),V(-.20f,0),V(.20f,0),V(.23f,0)},new[]{P(.115f,.115f),P(.132f,.132f),P(.132f,.132f),P(.115f,.115f)},canvas,Finish.Cloth,24,3,.015f);
            foreach(int side in new[]{-1,1}){
                var spiral=new Vector3[65];for(int i=0;i<spiral.Length;i++){float t=i/64f,a=t*Mathf.PI*5.7f,r=Mathf.Lerp(.012f,.112f,t);spiral[i]=V(side*.235f,Mathf.Sin(a)*r,Mathf.Cos(a)*r);}
                Curve("Visible folded cloth spiral",p,seam,Finish.Cloth,.005f,spiral);
                Ring("Rolled cloth outer bound hem",p,V(side*.232f,0),.117f,.007f,cut,Finish.Cloth,V(0,90,0));
                Sweep("Blanket leather carrying band",p,new[]{V(side*.11f-.026f,0),V(side*.11f+.026f,0)},new[]{P(.139f,.139f),P(.139f,.139f)},strap,Finish.Leather,24,1);
                Block("Blanket strap brass catch",p,V(side*.11f,.015f,.14f),V(.07f,.075f,.018f),C("BE904F"),Finish.Brass);
            }
            var pouch=Models.Root("Rear travel pouch",parent);pouch.transform.position=V(.28f,1.03f,.55f);pouch.transform.localEulerAngles=V(0,180,-5);p=pouch.transform;
            Block("Rounded leather pouch body",p,V(0,0),V(.255f,.31f,.165f),C("68432B"),Finish.Leather);
            Panel("Heavy overlapping pouch flap",p,new[]{P(-.127f,.15f),P(.127f,.15f),P(.11f,.015f),P(.065f,-.035f),P(-.06f,-.035f),P(-.116f,.025f)},-.092f,.028f,.09f,C("885A34"),Finish.Leather);
            Stud(p,V(0,.015f,-.113f),.021f);
            Curve("Pouch sewn lower hem",p,C("B38C5A"),Finish.Cloth,.004f,V(-.105f,-.075f,-.09f),V(-.10f,-.132f,-.09f),V(.10f,-.132f,-.09f),V(.105f,-.075f,-.09f));
            foreach(var root in new[]{roll,pouch})foreach(var r in root.GetComponentsInChildren<MeshRenderer>()){
                var m=new Material(r.sharedMaterial);m.SetFloat("_CraftStrength",.12f);m.SetFloat("_OutlineWidth",.0025f);r.sharedMaterial=m;
            }
        }
    }
}
