using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
namespace Mist.Editor
{
    public static class SculptReview
    {
        public static void BakeCavities(GameObject root)
        {
            var filters=root.GetComponentsInChildren<MeshFilter>().Where(m=>m.GetComponent<Renderer>().enabled).ToArray();
            var colliders=new List<MeshCollider>();var layers=new Dictionary<GameObject,int>();
            try{
                foreach(var f in filters){layers[f.gameObject]=f.gameObject.layer;f.gameObject.layer=30;var c=f.gameObject.AddComponent<MeshCollider>();c.sharedMesh=f.sharedMesh;colliders.Add(c);}Physics.SyncTransforms();
                foreach(var f in filters){
                    if(f.GetComponent<Renderer>().sharedMaterial.GetColor("_EmissionColor").maxColorComponent>.02f)continue;
                    var m=f.sharedMesh;var v=m.vertices;var n=m.normals;var c=m.colors;if(c.Length!=v.Length){c=new Color[v.Length];for(int i=0;i<c.Length;i++)c[i]=Color.white;}
                    var matrix=f.transform.localToWorldMatrix;var normalMatrix=matrix.inverse.transpose;
                    for(int i=0;i<v.Length;i++){
                        Vector3 at=matrix.MultiplyPoint3x4(v[i]),normal=normalMatrix.MultiplyVector(n[i]).normalized;
                        Vector3 tangent=Vector3.Cross(normal,Mathf.Abs(normal.y)<.9f?Vector3.up:Vector3.forward).normalized,bitangent=Vector3.Cross(normal,tangent);float total=0;
                        for(int ray=0;ray<10;ray++){float z=(ray+.5f)/10,rad=Mathf.Sqrt(1-z*z),a=ray*2.3999632f;Vector3 direction=tangent*(Mathf.Cos(a)*rad)+bitangent*(Mathf.Sin(a)*rad)+normal*z;RaycastHit hit;
                            if(Physics.Raycast(at+normal*.008f,direction,out hit,.34f,1<<30,QueryTriggerInteraction.Ignore))total+=1-Mathf.Clamp01(hit.distance/.34f);}
                        c[i].a=1-total/10*.8f;
                    }
                    m.colors=c;
                }
            }finally{foreach(var c in colliders)if(c)UnityEngine.Object.DestroyImmediate(c);foreach(var e in layers)e.Key.layer=e.Value;}
        }
        public static void VerifyAndBuildMac(){RuleChecks.Run();CombatChecks.Run();ModelChecks.Run();BuildGame.Mac();}
        public static void Run()
        {
            BuildGame.GenerateModels();BuildGame.Prepare();ModelChecks.Run();CaptureModels();CaptureAreas();BuildGame.Gallery();
            Debug.Log("SCULPT_REVIEW_OK");
        }
        public static void CaptureModels()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);Directory.CreateDirectory("docs/screenshots/sculpt-review");
            RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Trilight;RenderSettings.ambientSkyColor=Models.Hex("6B7C85");RenderSettings.ambientEquatorColor=Models.Hex("35474F");RenderSettings.ambientGroundColor=Models.Hex("242C2C");RenderSettings.fog=false;
            var light=new GameObject("Inspection warm key").AddComponent<Light>();light.type=LightType.Directional;light.color=Models.Hex("FFE5BB");light.intensity=.9f;light.transform.rotation=Quaternion.Euler(38,-35,0);light.shadows=LightShadows.Soft;light.shadowStrength=.65f;
            var fill=new GameObject("Inspection cool rim").AddComponent<Light>();fill.type=LightType.Directional;fill.color=Models.Hex("9EC2C6");fill.intensity=.3f;fill.transform.rotation=Quaternion.Euler(32,150,0);
            var camera=new GameObject("Inspection camera").AddComponent<Camera>();camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Models.Hex("28373D");camera.orthographic=true;camera.orthographicSize=1.46f;camera.nearClipPlane=.1f;camera.farClipPlane=30;
            var stats=new List<string>();
            for(int kind=-1;kind<7;kind++){
                string name=kind<0?"Hunter0":"Enemy"+kind;var g=UnityEngine.Object.Instantiate(Resources.Load<GameObject>("Models/"+name));
                var renderers=ModelGeometry.Renderers(g);var bounds=ModelGeometry.WorldBounds(g);
                camera.orthographicSize=Mathf.Max(bounds.size.y*.6f,bounds.size.x*.6f);Vector3 target=bounds.center;
                int triangles=renderers.Sum(r=>ModelGeometry.SharedMesh(r).triangles.Length/3);stats.Add(name+" rendered_triangles="+triangles+" renderers="+renderers.Length+" modeled_parts="+g.GetComponentsInChildren<MeshFilter>().Count(m=>!m.GetComponent<Renderer>().enabled));
                for(int view=0;view<3;view++){float angle=view==0?-22:view==1?70:155;camera.transform.position=target+Quaternion.Euler(0,angle,0)*new Vector3(0,2,-8);camera.transform.LookAt(target);Capture(camera,"docs/screenshots/sculpt-review/"+name+"-view-"+view+".png",1200,1200);}
                var original=renderers.Select(r=>r.sharedMaterial).ToArray();var clay=new Material(Shader.Find("Mist/Sculpt")){color=Models.Hex("A9A795")};clay.SetFloat("_Smoothness",.15f);
                var savedColors=new List<Color[]>();foreach(var r in renderers){var mesh=ModelGeometry.SharedMesh(r);var colors=mesh.colors;savedColors.Add(colors);mesh.colors=colors.Select(c=>new Color(1,1,1,c.a)).ToArray();r.sharedMaterial=clay;}
                camera.transform.position=target+new Vector3(-3,2,-8);camera.transform.LookAt(target);Capture(camera,"docs/screenshots/sculpt-review/"+name+"-clay.png",1200,1200);
                for(int i=0;i<renderers.Length;i++){renderers[i].sharedMaterial=original[i];ModelGeometry.SharedMesh(renderers[i]).colors=savedColors[i];}
                UnityEngine.Object.DestroyImmediate(clay);UnityEngine.Object.DestroyImmediate(g);
            }
            File.WriteAllLines("docs/qa/sculpt-review/model-stats.txt",stats);
        }
        public static void CaptureAreas()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            UI.Font=Resources.Load<TMPro.TMP_FontAsset>("Fonts/Mist SDF");var world=new GameObject("Actual game render").AddComponent<World>();world.Setup();
            for(int area=0;area<3;area++){var s=Simulation.NewRun(42,0,new Profile());s.room=area*4+3;var sim=new Simulation(s,new Profile());sim.Enter();s.pos=new Vector2(-3,0);s.enemies[0].pos=new Vector2(3,0);world.ResetModels();world.Render(s,.016f,true);Capture(world.cam,"docs/screenshots/sculpt-review/game-area-"+area+".png",1600,900);}
        }
        static void Capture(Camera cam,string path,int width,int height)
        {
            using(var pose=new ModelGeometry.PoseBake()){
            var rt=new RenderTexture(width,height,24);rt.antiAliasing=4;cam.targetTexture=rt;cam.Render();RenderTexture.active=rt;var tex=new Texture2D(width,height,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,width,height),0,0);tex.Apply();File.WriteAllBytes(path,tex.EncodeToPNG());cam.targetTexture=null;RenderTexture.active=null;UnityEngine.Object.DestroyImmediate(tex);rt.Release();UnityEngine.Object.DestroyImmediate(rt);
            }
        }
    }
}
