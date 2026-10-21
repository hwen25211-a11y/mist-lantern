using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Globalization;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
namespace Mist.Editor
{
    public static class ModelExport
    {
        static string F(float value){return value.ToString("0.######",CultureInfo.InvariantCulture);}
        static string P(Vector3 v){return F(v.x)+" "+F(v.y)+" "+F(-v.z);}
        static string Name(Transform t){string n=t.name.Replace(' ','_');while(t.parent){t=t.parent;n=t.name.Replace(' ','_')+"__"+n;}return n;}
        public static void Run()
        {
            Directory.CreateDirectory("ArtSource/Models");var report=new List<string>();
            for(int k=-3;k<7;k++){
                string name=k<0?"Hunter"+(k+3):"Enemy"+k;var root=UnityEngine.Object.Instantiate(Resources.Load<GameObject>("Models/"+name));
                var obj=new StringBuilder("# Mist Lantern solid model authoring parts; units match Unity.\n# OBJ right-handed +Y up. Procedural animation and custom shaders remain in Unity.\nmtllib "+name+".mtl\n");
                var mtl=new StringBuilder("# Base material palette. Crafted surface shader is defined in the Unity project.\n");var materials=new Dictionary<Material,string>();int offset=1,parts=0,triangles=0;
                foreach(var renderer in ModelGeometry.Renderers(root)){
                    var source=renderer.sharedMaterial;
                    string material;if(!materials.TryGetValue(source,out material)){material="m"+materials.Count;materials[source]=material;Color c=source.color;
                        mtl.Append("newmtl "+material+"\nKd "+F(c.r)+" "+F(c.g)+" "+F(c.b)+"\nKa 0.1 0.1 0.1\nKs 0.2 0.2 0.2\nNs "+F(source.GetFloat("_Smoothness")*150)+"\nd 1\n");
                        if(source.HasProperty("_MainTex")&&source.GetTexture("_MainTex")){
                            string texture=AssetDatabase.GetAssetPath(source.GetTexture("_MainTex"));
                            if(File.Exists(texture)){string file=name+"-"+material+Path.GetExtension(texture);File.Copy(texture,"ArtSource/Models/"+file,true);mtl.Append("map_Kd "+file+"\n");}
                        }
                        mtl.Append("\n");}
                    var skin=renderer as SkinnedMeshRenderer;var mesh=skin?new Mesh():ModelGeometry.SharedMesh(renderer);if(skin)skin.BakeMesh(mesh);
                    var matrix=renderer.transform.localToWorldMatrix;var normalMatrix=matrix.inverse.transpose;var vertices=mesh.vertices;var normals=mesh.normals;
                    var colors=mesh.colors;var uv=mesh.uv;
                    obj.Append("o "+Name(renderer.transform)+"\nusemtl "+material+"\n");
                    for(int i=0;i<vertices.Length;i++){obj.Append("v "+P(matrix.MultiplyPoint3x4(vertices[i])));if(colors.Length==vertices.Length)obj.Append(" "+F(colors[i].r)+" "+F(colors[i].g)+" "+F(colors[i].b));obj.Append("\n");}
                    for(int i=0;i<vertices.Length;i++){var tex=uv.Length==vertices.Length?uv[i]:Vector2.zero;obj.Append("vt "+F(tex.x)+" "+F(tex.y)+"\n");}
                    foreach(var n in normals)obj.Append("vn "+P(normalMatrix.MultiplyVector(n).normalized)+"\n");
                    var indices=mesh.triangles;bool flip=matrix.determinant>=0;
                    for(int i=0;i<indices.Length;i+=3){int a=offset+indices[i],b=offset+indices[i+(flip?2:1)],c=offset+indices[i+(flip?1:2)];obj.Append("f "+a+"/"+a+"/"+a+" "+b+"/"+b+"/"+b+" "+c+"/"+c+"/"+c+"\n");}
                    triangles+=indices.Length/3;offset+=vertices.Length;parts++;if(skin)UnityEngine.Object.DestroyImmediate(mesh);
                }
                File.WriteAllText("ArtSource/Models/"+name+".obj",obj.ToString());File.WriteAllText("ArtSource/Models/"+name+".mtl",mtl.ToString());report.Add(name+" parts="+parts+" vertices="+(offset-1)+" triangles="+triangles);UnityEngine.Object.DestroyImmediate(root);
            }
            File.WriteAllLines("docs/qa/sculpt-review/model-export.txt",report);Debug.Log("MODEL_EXPORT_OK models=10");
        }
        public static void ExportAndBuildWeb(){Run();BuildGame.Web();}
    }
}
