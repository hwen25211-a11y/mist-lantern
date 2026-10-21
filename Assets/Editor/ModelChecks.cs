using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
namespace Mist.Editor
{
    public static class ModelChecks
    {
        public static void Diagnose()
        {
            var root=Resources.Load<GameObject>("Models/Hunter0");
            foreach(var f in root.GetComponentsInChildren<MeshFilter>()){
                if(f.GetComponent<Renderer>().enabled)continue;var m=f.sharedMesh;var v=m.vertices;var n=m.normals;var t=m.triangles;int bad=0;
                for(int i=0;i<t.Length;i+=3){var normal=Vector3.Cross(v[t[i+1]]-v[t[i]],v[t[i+2]]-v[t[i]]);if(normal.sqrMagnitude<1e-12f)continue;var average=n[t[i]]+n[t[i+1]]+n[t[i+2]];if(average.sqrMagnitude<.01f)continue;if(Vector3.Dot(normal.normalized,average.normalized)<-.08f){bad++;if(bad==1)Debug.Log("BAD_FACE "+f.name+" first="+v[t[i]]+" "+v[t[i+1]]+" "+v[t[i+2]]);}}
                if(bad>0)Debug.Log("BAD_SOURCE "+f.name+" bad="+bad+" faces="+t.Length/3);
            }
        }
        public static void Run()
        {
            var lines=new List<string>();int checkedFaces=0,badFaces=0;
            for(int k=-3;k<7;k++){
                string name=k<0?"Hunter"+(k+3):"Enemy"+k;var root=Resources.Load<GameObject>("Models/"+name);
                if(!root)throw new Exception("Missing model "+name);
                var renders=root.GetComponentsInChildren<Renderer>().Where(x=>x.enabled&&(x is MeshRenderer||x is SkinnedMeshRenderer)).ToArray();
                foreach(var r in renders){
                    if(!r.sharedMaterial||!r.sharedMaterial.shader||!r.sharedMaterial.shader.isSupported||(r.sharedMaterial.shader.name!="Mist/Sculpt"&&r.sharedMaterial.shader.name!="Mist/CharacterPaint"))throw new Exception("Missing character shader: "+name);
                    var skin=r as SkinnedMeshRenderer;var m=skin?skin.sharedMesh:r.GetComponent<MeshFilter>().sharedMesh;var v=m.vertices;var n=m.normals;var t=m.triangles;var c=m.colors;
                    if(skin){
                        if(m.boneWeights.Length!=m.vertexCount||m.bindposes.Length!=skin.bones.Length||skin.bones.Any(b=>!b))throw new Exception("Invalid skin binding "+name+" "+r.name);
                        foreach(var w in m.boneWeights)if(Mathf.Abs(w.weight0+w.weight1+w.weight2+w.weight3-1)>.002f||w.boneIndex0>=skin.bones.Length||w.boneIndex1>=skin.bones.Length||w.boneIndex2>=skin.bones.Length||w.boneIndex3>=skin.bones.Length)throw new Exception("Invalid skin weight "+name+" "+r.name);
                    }
                    if(c.Length!=v.Length||n.Length!=v.Length)throw new Exception("Vertex channels missing "+name);
                    for(int i=0;i<v.Length;i++)if(float.IsNaN(v[i].x+v[i].y+v[i].z)||float.IsInfinity(v[i].magnitude)||float.IsNaN(n[i].magnitude))throw new Exception("Nonfinite mesh "+name);
                    int mismatches=0,faces=0;
                    for(int i=0;i<t.Length;i+=3){var normal=Vector3.Cross(v[t[i+1]]-v[t[i]],v[t[i+2]]-v[t[i]]);if(normal.sqrMagnitude<1e-12f)continue;var average=n[t[i]]+n[t[i+1]]+n[t[i+2]];if(average.sqrMagnitude<.01f)continue;faces++;if(Vector3.Dot(normal.normalized,average.normalized)<-.08f)mismatches++;}
                    checkedFaces+=faces;badFaces+=mismatches;if(mismatches>Mathf.Max(3,faces*.001f))throw new Exception("Triangle winding disagrees with normals: "+name+" "+r.name+" bad="+mismatches+" / "+faces);
                }
                if(k<0&&(!root.transform.Find("Rig/Body/ArmR/Forearm/Weapon")||!root.transform.Find("Rig/Body/ArmL/Forearm/Lantern")||!root.transform.Find("Rig/LegL/Shin")))throw new Exception("Articulation missing "+name);
                lines.Add("PASS "+name+" materials, vertex data, winding, articulation");
            }
            foreach(int area in new[]{0,1,2}){
                var stage=Resources.Load<GameObject>("Stages/Area"+area);if(!stage)throw new Exception("Missing stage");
                foreach(var r in stage.GetComponentsInChildren<MeshRenderer>().Where(x=>x.enabled))if(!r.sharedMaterial||!r.sharedMaterial.shader)throw new Exception("Missing stage material");
                lines.Add("PASS Area"+area+" stage materials");
            }
            lines.Add("MODEL_CHECKS_OK checked_faces="+checkedFaces+" exceptions="+badFaces);File.WriteAllLines("docs/qa/sculpt-review/model-checks.txt",lines);Debug.Log(lines.Last());
        }
    }
}
