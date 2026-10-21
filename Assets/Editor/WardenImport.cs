using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Mist.Editor
{
    // Imports the Blender-authored deforming mesh without requiring a Blender app
    // installation on the teacher's machine. The .blend remains editable source.
    public static class WardenImport
    {
        [Serializable] public class Bone { public string name; public int parent; public float[] position; }
        [Serializable] public class Part
        {
            public string name,texture;public bool alphaCutout,painted;public float[] color,vertices,normals,colors,uv,boneWeights;
            public int[] indices,boneIndices;public float roughness,metallic;
        }
        [Serializable] public class Document {public int version;public Bone[] bones;public Part[] parts;public float[] gripRight,gripLeft;}
        static Vector3 V(float[] a,int i=0){return new Vector3(a[i],a[i+1],a[i+2]);}
        static string DisplayName(string n){return n=="Cape"?"ScarfTail":n.StartsWith("Forearm")?"Forearm":n.StartsWith("Shin")?"Shin":n.StartsWith("Hand")?"Hand":n.StartsWith("Foot")?"Foot":n;}

        public static void Run()
        {
            var data=JsonUtility.FromJson<Document>(File.ReadAllText("Build/CharacterSource/Warden.mesh.json"));
            if(data.version!=1||data.parts.Length==0||data.bones.Length<15)throw new Exception("Invalid Warden export");
            const string dir="Assets/Characters/Warden";Directory.CreateDirectory(dir);Directory.CreateDirectory("Assets/Resources/Characters");
            if(data.parts.Any(p=>p.alphaCutout)){
                File.Copy("ArtSource/Characters/Base/HairCandidates/hair/cortu_short_messy_hair/short_messy_diff.png",dir+"/WardenHair.png",true);AssetDatabase.Refresh();
                var ti=(TextureImporter)AssetImporter.GetAtPath(dir+"/WardenHair.png");ti.alphaSource=TextureImporterAlphaSource.FromInput;ti.alphaIsTransparency=true;ti.mipmapEnabled=true;ti.maxTextureSize=1024;ti.textureCompression=TextureImporterCompression.CompressedHQ;ti.SaveAndReimport();
            }
            var root=new GameObject("Keeper character");var bones=new Transform[data.bones.Length];
            for(int i=0;i<bones.Length;i++)
            {
                var b=data.bones[i];bones[i]=new GameObject(DisplayName(b.name)).transform;bones[i].SetParent(b.parent<0?root.transform:bones[b.parent],false);bones[i].position=V(b.position);
            }
            var binds=bones.Select(b=>b.worldToLocalMatrix*root.transform.localToWorldMatrix).ToArray();
            int vertices=0,triangles=0;var paintedTextures=new System.Collections.Generic.Dictionary<string,Texture2D>();
            for(int p=0;p<data.parts.Length;p++)
            {
                var part=data.parts[p];int n=part.vertices.Length/3;
                if(n==0||part.normals.Length!=n*3||part.colors.Length!=n*4||part.boneWeights.Length!=n*4)throw new Exception("Invalid vertex channels "+part.name);
                var mesh=new Mesh{name=part.name,indexFormat=UnityEngine.Rendering.IndexFormat.UInt32};
                var vv=new Vector3[n];var nn=new Vector3[n];var cc=new Color[n];var uv=new Vector2[n];var ww=new BoneWeight[n];
                for(int i=0;i<n;i++)
                {
                    vv[i]=V(part.vertices,i*3);nn[i]=V(part.normals,i*3);cc[i]=new Color(Mathf.LinearToGammaSpace(part.colors[i*4]),Mathf.LinearToGammaSpace(part.colors[i*4+1]),Mathf.LinearToGammaSpace(part.colors[i*4+2]),part.colors[i*4+3]);uv[i]=new Vector2(part.uv[i*2],part.uv[i*2+1]);
                    ww[i]=new BoneWeight{boneIndex0=part.boneIndices[i*4],boneIndex1=part.boneIndices[i*4+1],boneIndex2=part.boneIndices[i*4+2],boneIndex3=part.boneIndices[i*4+3],weight0=part.boneWeights[i*4],weight1=part.boneWeights[i*4+1],weight2=part.boneWeights[i*4+2],weight3=part.boneWeights[i*4+3]};
                }
                mesh.vertices=vv;mesh.normals=nn;mesh.colors=cc;mesh.uv=uv;mesh.triangles=part.indices;mesh.boneWeights=ww;mesh.bindposes=binds;mesh.RecalculateBounds();
                string mp=dir+"/Part"+p+".asset";if(AssetDatabase.LoadAssetAtPath<Mesh>(mp))AssetDatabase.DeleteAsset(mp);AssetDatabase.CreateAsset(mesh,mp);
                Sculpt.Finish finish=part.name.StartsWith("Skin")?Sculpt.Finish.Skin:part.name.StartsWith("Hair")?Sculpt.Finish.Hair:part.name.StartsWith("Eye")?Sculpt.Finish.Ceramic:part.name.StartsWith("Brass")?Sculpt.Finish.Brass:part.name.StartsWith("Iron")?Sculpt.Finish.Iron:part.name.StartsWith("Leather")?Sculpt.Finish.Leather:Sculpt.Finish.Cloth;
                var mat=part.painted?new Material(Shader.Find("Mist/CharacterPaint")):part.alphaCutout?new Material(Shader.Find("Mist/Hair")):new Material(Sculpt.Material(Color.white,finish));mat.name=part.name;mat.color=Color.white;
                if(part.painted){
                    if(string.IsNullOrEmpty(part.texture)||!File.Exists(part.texture))throw new Exception("Missing painted albedo: "+part.name);
                    Texture2D painted;if(!paintedTextures.TryGetValue(part.texture,out painted)){
                        string tp=dir+"/Part"+p+"-Albedo.png";File.Copy(part.texture,tp,true);AssetDatabase.ImportAsset(tp,ImportAssetOptions.ForceSynchronousImport);
                        var ti=(TextureImporter)AssetImporter.GetAtPath(tp);ti.sRGBTexture=true;ti.alphaSource=TextureImporterAlphaSource.None;ti.mipmapEnabled=true;ti.maxTextureSize=4096;ti.textureCompression=TextureImporterCompression.CompressedHQ;ti.SaveAndReimport();
                        painted=AssetDatabase.LoadAssetAtPath<Texture2D>(tp);paintedTextures[part.texture]=painted;
                    }
                    mat.SetTexture("_MainTex",painted);mat.SetFloat("_OutlineWidth",.003f);
                }
                else if(part.alphaCutout){mat.SetTexture("_MainTex",AssetDatabase.LoadAssetAtPath<Texture2D>(dir+"/WardenHair.png"));mat.SetFloat("_Cutoff",.38f);}
                else {mat.SetFloat("_Metallic",part.metallic);mat.SetFloat("_Smoothness",1-part.roughness);mat.SetFloat("_OutlineWidth",finish==Sculpt.Finish.Skin?.0025f:finish==Sculpt.Finish.Hair?.002f:.003f);mat.SetFloat("_CraftStrength",.30f);mat.SetFloat("_CavityStrength",finish==Sculpt.Finish.Skin?.42f:.8f);mat.SetFloat("_ToonSkin",1);}
                string matp=dir+"/Part"+p+".mat";if(AssetDatabase.LoadAssetAtPath<Material>(matp))AssetDatabase.DeleteAsset(matp);AssetDatabase.CreateAsset(mat,matp);
                var child=new GameObject(part.name);child.transform.SetParent(root.transform,false);var skin=child.AddComponent<SkinnedMeshRenderer>();skin.sharedMesh=mesh;skin.sharedMaterial=mat;skin.bones=bones;skin.rootBone=bones[0];skin.quality=SkinQuality.Bone4;skin.updateWhenOffscreen=true;skin.localBounds=new Bounds(new Vector3(0,1.3f,0),new Vector3(3,3.5f,3));
                vertices+=n;triangles+=part.indices.Length/3;
            }
            foreach(var side in new[]{"R","L"})
            {
                int hand=Array.FindIndex(data.bones,b=>b.name=="Hand"+side);var grip=new GameObject("Grip").transform;grip.SetParent(bones[hand],false);grip.position=V(side=="R"?data.gripRight:data.gripLeft);
            }
            PrefabUtility.SaveAsPrefabAsset(root,"Assets/Resources/Characters/Warden.prefab");UnityEngine.Object.DestroyImmediate(root);AssetDatabase.SaveAssets();AssetDatabase.Refresh();
            Debug.Log("WARDEN_IMPORT_OK vertices="+vertices+" triangles="+triangles+" bones="+bones.Length);
            // Only regenerate the three hero variants; environments and monsters are unchanged here.
            BuildGame.GenerateHunters();WardenReview.Run();
        }
    }
}
