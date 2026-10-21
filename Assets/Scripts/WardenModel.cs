using UnityEngine;
namespace Mist
{
    public static class WardenModel
    {
        public static GameObject Create(int weapon)
        {
            var prefab=Resources.Load<GameObject>("Characters/Warden");if(!prefab)return null;
            var root=Object.Instantiate(prefab);root.name="Hunter"+weapon;
            var right=root.transform.Find("Rig/Body/ArmR/Forearm");var left=root.transform.Find("Rig/Body/ArmL/Forearm");
            var rgrip=right.Find("Hand/Grip");var lgrip=left.Find("Hand/Grip");
            var w=Sculpt.KeeperWeapon(right,weapon);w.transform.position=rgrip.position;w.transform.localScale=Vector3.one*(weapon==0?.78f:.68f);w.transform.localEulerAngles=new Vector3(weapon==0?15:0,0,weapon==0?210:-12);
            var lamp=Sculpt.KeeperLantern(left,.62f);lamp.transform.position=lgrip.position-Vector3.up*(1.04f*.62f);
            Sculpt.KeeperBackGear(root.transform.Find("Rig/Body"));
            Sculpt.Optimize(root);return root;
        }
    }
}
