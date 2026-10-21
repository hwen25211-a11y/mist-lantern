using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
namespace Mist.Editor
{
    public static class WardenReview
    {
        const string Dir="docs/screenshots/character-rebuild/";
        public static void Run(){Check();Capture();SculptReview.CaptureAreas();Debug.Log("WARDEN_REVIEW_OK");}
        public static void Check()
        {
            int vertices=0,triangles=0;
            for(int w=0;w<3;w++){
                var root=UnityEngine.Object.Instantiate(Resources.Load<GameObject>("Models/Hunter"+w));
                var skins=root.GetComponentsInChildren<SkinnedMeshRenderer>();if(skins.Length==0)throw new Exception("Missing character skin mesh");
                foreach(var skin in skins){var m=skin.sharedMesh;if(!m||m.bindposes.Length!=skin.bones.Length||skin.bones.Any(b=>!b)||!skin.sharedMaterial.shader.isSupported)throw new Exception("Broken character dependencies");
                    if(m.boneWeights.Length!=m.vertexCount||m.normals.Length!=m.vertexCount||m.colors.Length!=m.vertexCount)throw new Exception("Missing character channels");
                    foreach(var b in m.boneWeights){float total=b.weight0+b.weight1+b.weight2+b.weight3;if(Mathf.Abs(total-1)>.001f)throw new Exception("Unnormalized skin weights");if(b.boneIndex0>=skin.bones.Length||b.boneIndex1>=skin.bones.Length||b.boneIndex2>=skin.bones.Length||b.boneIndex3>=skin.bones.Length)throw new Exception("Invalid bone index");}
                    vertices+=m.vertexCount;triangles+=m.triangles.Length/3;
                }
                foreach(float angle in new[]{-125f,-80f,-30f,0f,35f,65f}){
                    root.transform.Find("Rig/Body/ArmR").localRotation=Quaternion.Euler(angle,30,-25);
                    root.transform.Find("Rig/Body/ArmR/Forearm").localRotation=Quaternion.Euler(-38,0,0);
                    root.transform.Find("Rig/Body/ArmL").localRotation=Quaternion.Euler(-20,0,15);
                    root.transform.Find("Rig/Body/ScarfTail").localRotation=Quaternion.Euler(4,5,2);
                    root.transform.Find("Rig/Body/ScarfTail/CapeTip").localRotation=Quaternion.Euler(7,0,3);
                    root.transform.Find("Rig/LegL").localRotation=Quaternion.Euler(26,0,0);
                    root.transform.Find("Rig/LegL/Shin").localRotation=Quaternion.Euler(-38,0,0);
                    foreach(var skin in skins){var baked=new Mesh();skin.BakeMesh(baked);var posed=baked.vertices;var rest=skin.sharedMesh.vertices;
                        if(posed.Any(v=>float.IsNaN(v.sqrMagnitude)||float.IsInfinity(v.sqrMagnitude)))throw new Exception("Nonfinite deformed vertex");
                        var indices=skin.sharedMesh.triangles;float worstStretch=1;
                        for(int j=0;j<indices.Length;j+=3)for(int e=0;e<3;e++){int a=indices[j+e],b=indices[j+(e+1)%3];float length=(rest[a]-rest[b]).magnitude;if(length>.0005f)worstStretch=Mathf.Max(worstStretch,(posed[a]-posed[b]).magnitude/length);}
                        UnityEngine.Object.DestroyImmediate(baked);if(worstStretch>6)throw new Exception("Excessive character edge stretching: "+skin.name+" ratio="+worstStretch+" pose="+angle);
                    }
                }
                var right=root.transform.Find("Rig/Body/ArmR");right.localRotation=Quaternion.identity;
                var reference=skins.Select(s=>{var m=new Mesh();s.BakeMesh(m);return m;}).ToArray();right.localRotation=Quaternion.Euler(-80,20,0);float displacement=0;
                for(int i=0;i<skins.Length;i++){var posed=new Mesh();skins[i].BakeMesh(posed);var a=reference[i].vertices;var b=posed.vertices;var weights=skins[i].sharedMesh.boneWeights;
                    for(int v=0;v<a.Length;v++)if(skins[i].bones[weights[v].boneIndex0].IsChildOf(right))displacement=Mathf.Max(displacement,(a[v]-b[v]).magnitude);
                    UnityEngine.Object.DestroyImmediate(reference[i]);UnityEngine.Object.DestroyImmediate(posed);}
                if(displacement<.12f)throw new Exception("Arm bones did not deform the anatomical mesh");
                if(!root.transform.Find("Rig/Body/ArmR/Forearm/Weapon")||!root.transform.Find("Rig/Body/ArmL/Forearm/Lantern"))throw new Exception("Missing held prop");
                UnityEngine.Object.DestroyImmediate(root);
            }
            Directory.CreateDirectory("docs/qa/character-rebuild");File.WriteAllText("docs/qa/character-rebuild/mesh-checks.txt","PASS three weapon variants: mesh channels, normalized skin weights, valid bones and shaders, held props, finite deformations and edge stretch below 6 at six attack poses with bent elbow/knee and moving cape.\nTotal variant vertices="+vertices+" triangles="+triangles+"\n");Debug.Log("WARDEN_CHECK_OK");
        }
        static void Capture()
        {
            Directory.CreateDirectory(Dir);EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Trilight;RenderSettings.ambientSkyColor=Models.Hex("86969A");RenderSettings.ambientEquatorColor=Models.Hex("546468");RenderSettings.ambientGroundColor=Models.Hex("32373C");RenderSettings.fog=false;
            var light=new GameObject("Portrait key").AddComponent<Light>();light.type=LightType.Directional;light.intensity=.85f;light.color=Models.Hex("FFE6CF");light.transform.rotation=Quaternion.Euler(35,-32,0);light.shadows=LightShadows.Soft;
            var rim=new GameObject("Portrait rim").AddComponent<Light>();rim.type=LightType.Directional;rim.intensity=.25f;rim.color=Models.Hex("9CC7D2");rim.transform.rotation=Quaternion.Euler(25,145,0);
            var cam=new GameObject("Actual Unity model camera").AddComponent<Camera>();cam.orthographic=true;cam.backgroundColor=Models.Hex("29383E");cam.clearFlags=CameraClearFlags.SolidColor;
            var root=UnityEngine.Object.Instantiate(Resources.Load<GameObject>("Models/Hunter0"));
            foreach(int view in new[]{0,1,2}){Vector3 target=new Vector3(0,1.15f,0);cam.transform.position=target+Quaternion.Euler(0,view==0?-35:view==1?85:165,0)*new Vector3(0,.8f,-6);cam.transform.LookAt(target);Fit(cam,root,1200f/1400);Save(cam,Dir+"unity-hero-"+view+".png",1200,1400);}
            var renderers=root.GetComponentsInChildren<Renderer>();var materials=renderers.Select(r=>r.sharedMaterials).ToArray();var clay=new Material(Shader.Find("Standard"));clay.color=new Color(.52f,.52f,.48f);clay.SetFloat("_Glossiness",.18f);
            foreach(var renderer in renderers)renderer.sharedMaterials=renderer.sharedMaterials.Select(_=>clay).ToArray();cam.transform.position=new Vector3(-3,2.3f,-6);cam.transform.LookAt(new Vector3(0,1.15f,0));Fit(cam,root,1200f/1400);Save(cam,Dir+"unity-clay.png",1200,1400);for(int i=0;i<renderers.Length;i++)renderers[i].sharedMaterials=materials[i];UnityEngine.Object.DestroyImmediate(clay);
            var head=root.transform.Find("Rig/Body/Head");Vector3 face=head.position+new Vector3(0,.39f,-.12f);cam.orthographicSize=.55f;cam.transform.position=face+new Vector3(-.65f,.05f,-2);cam.transform.LookAt(face);Save(cam,Dir+"unity-face.png",1200,1200);
            root.transform.Find("Rig/Body/ArmR").localRotation=Quaternion.Euler(-90,30,-25);root.transform.Find("Rig/Body/ArmR/Forearm").localRotation=Quaternion.Euler(-32,0,0);root.transform.Find("Rig/LegL").localRotation=Quaternion.Euler(26,0,0);root.transform.Find("Rig/LegL/Shin").localRotation=Quaternion.Euler(-38,0,0);
            cam.transform.position=new Vector3(-3,2.5f,-6);cam.transform.LookAt(new Vector3(0,1.2f,0));Fit(cam,root,1200f/1400);Save(cam,Dir+"unity-deformation.png",1200,1400);
            UnityEngine.Object.DestroyImmediate(root);
        }
        static void Fit(Camera cam,GameObject root,float aspect)
        {
            var bounds=new Bounds();bool first=true;var inverse=Quaternion.Inverse(cam.transform.rotation);
            foreach(var r in root.GetComponentsInChildren<Renderer>().Where(r=>r.enabled)){
                Mesh m;bool baked=r is SkinnedMeshRenderer;if(baked){m=new Mesh();((SkinnedMeshRenderer)r).BakeMesh(m);}else {var mf=r.GetComponent<MeshFilter>();if(!mf)continue;m=mf.sharedMesh;}
                foreach(var v in m.vertices){var p=inverse*r.transform.TransformPoint(v);if(first){bounds=new Bounds(p,Vector3.zero);first=false;}else bounds.Encapsulate(p);}if(baked)UnityEngine.Object.DestroyImmediate(m);
            }
            var center=cam.transform.rotation*bounds.center;cam.transform.position=center-cam.transform.forward*6;cam.orthographicSize=Mathf.Max(bounds.extents.y,bounds.extents.x/aspect)*1.10f;
        }
        static void Save(Camera cam,string path,int width,int height){
            // Camera.Render in a batch editor does not advance the player's skinning
            // update. Bake the current bone pose explicitly for truthful offline images.
            var skins=UnityEngine.Object.FindObjectsOfType<SkinnedMeshRenderer>().Where(s=>s.enabled).ToArray();var temps=new System.Collections.Generic.List<GameObject>();
            foreach(var s in skins){var m=new Mesh();s.BakeMesh(m);var g=new GameObject("Review pose bake");g.transform.SetParent(s.transform,false);g.AddComponent<MeshFilter>().sharedMesh=m;g.AddComponent<MeshRenderer>().sharedMaterials=s.sharedMaterials;temps.Add(g);s.enabled=false;}
            var rt=new RenderTexture(width,height,24){antiAliasing=4};cam.targetTexture=rt;cam.Render();RenderTexture.active=rt;var t=new Texture2D(width,height,TextureFormat.RGB24,false);t.ReadPixels(new Rect(0,0,width,height),0,0);t.Apply();File.WriteAllBytes(path,t.EncodeToPNG());cam.targetTexture=null;RenderTexture.active=null;UnityEngine.Object.DestroyImmediate(t);rt.Release();UnityEngine.Object.DestroyImmediate(rt);
            foreach(var g in temps){UnityEngine.Object.DestroyImmediate(g.GetComponent<MeshFilter>().sharedMesh);UnityEngine.Object.DestroyImmediate(g);}foreach(var s in skins)s.enabled=true;
        }
    }
}
