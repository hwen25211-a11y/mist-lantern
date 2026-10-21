using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine.TextCore.LowLevel;
using TMPro;

namespace Mist.Editor
{
    public static class BuildGame
    {
        public const string Scene="Assets/Scenes/MistLantern.unity";
        [MenuItem("雾灯遗城/准备场景、模型与字体")]
        public static void Prepare()
        {
            Directory.CreateDirectory("docs/qa");AssetDatabase.Refresh();
            foreach(var shaderName in new[]{"Mist/SoftFX","Mist/Ink","Unlit/Texture","Mist/Backdrop","Mist/Sculpt","Mist/CharacterPaint"})
            {
                string mp="Assets/Resources/Art/"+shaderName.Replace("/","-")+".mat";
                if(!AssetDatabase.LoadAssetAtPath<Material>(mp))AssetDatabase.CreateAsset(new Material(Shader.Find(shaderName)),mp);
            }
            const string fontPath="Assets/Resources/Fonts/Mist SDF.asset";
            var font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(fontPath);
            if(!font)
            {
                font=TMP_FontAsset.CreateFontAsset(AssetDatabase.LoadAssetAtPath<Font>("Assets/Resources/Fonts/NotebookSans.ttf"),42,5,GlyphRenderMode.SDFAA,2048,2048,AtlasPopulationMode.Dynamic,true);
                font.name="Mist SDF";font.isMultiAtlasTexturesEnabled=true;AssetDatabase.CreateAsset(font,fontPath);AssetDatabase.AddObjectToAsset(font.material,font);
                foreach(var t in font.atlasTextures)AssetDatabase.AddObjectToAsset(t,font);
            }
            font.ClearFontAssetData(true);
            string chars=new string(string.Concat(Directory.GetFiles("Assets/Scripts","*.cs").Select(File.ReadAllText)).Concat("0123456789 /:.-+×").Where(c=>c>=32).Distinct().ToArray());
            string missing;font.TryAddCharacters(chars,out missing);if(!string.IsNullOrEmpty(missing))Debug.LogWarning("FONT_MISSING "+missing);
            foreach(var t in font.atlasTextures)if(!AssetDatabase.Contains(t))AssetDatabase.AddObjectToAsset(t,font);
            EditorUtility.SetDirty(font);
            var a=AssetImporter.GetAtPath("Assets/Resources/Art/Title.png") as TextureImporter;
            if(a){a.textureType=TextureImporterType.Default;a.maxTextureSize=2048;a.mipmapEnabled=false;a.textureCompression=TextureImporterCompression.CompressedHQ;a.SaveAndReimport();}
            foreach(var path in new[]{"Assets/Resources/Art/Worlds-v2.png","Assets/Resources/Art/Ground-v2.png","Assets/Resources/Art/Relics-v3.png"}) {
                var ti=AssetImporter.GetAtPath(path) as TextureImporter;if(ti){ti.maxTextureSize=2048;ti.mipmapEnabled=false;ti.wrapMode=TextureWrapMode.Clamp;ti.textureCompression=TextureImporterCompression.CompressedHQ;ti.SaveAndReimport();}}
            var craftImporter=AssetImporter.GetAtPath("Assets/Resources/Art/CraftSurfaces-v1.png") as TextureImporter;
            if(craftImporter){craftImporter.maxTextureSize=2048;craftImporter.mipmapEnabled=true;craftImporter.wrapMode=TextureWrapMode.Clamp;craftImporter.textureCompression=TextureImporterCompression.CompressedHQ;craftImporter.SaveAndReimport();}
            if(!File.Exists("Assets/Resources/Models/Hunter0.prefab"))GenerateModels();
            if(!File.Exists(Scene))
            {
                var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
                new GameObject("Mist Lantern scene marker");EditorSceneManager.SaveScene(scene,Scene);
            }
            EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(Scene,true)};
            PlayerSettings.companyName="Mist Lantern Studio";PlayerSettings.productName="雾灯遗城";PlayerSettings.bundleVersion="1.1.0";
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Standalone,"com.MistLantern.Game");
            PlayerSettings.defaultScreenWidth=1440;PlayerSettings.defaultScreenHeight=810;PlayerSettings.fullScreenMode=FullScreenMode.Windowed;PlayerSettings.resizableWindow=true;
            PlayerSettings.runInBackground=false;PlayerSettings.colorSpace=ColorSpace.Gamma;
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone,ScriptingImplementation.Mono2x);
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.WebGL,ScriptingImplementation.IL2CPP);
            PlayerSettings.WebGL.compressionFormat=WebGLCompressionFormat.Disabled;PlayerSettings.WebGL.memorySize=256;PlayerSettings.WebGL.dataCaching=true;
            PlayerSettings.WebGL.template="PROJECT:Mist";
            QualitySettings.antiAliasing=2;QualitySettings.vSyncCount=0;QualitySettings.shadowDistance=40;QualitySettings.shadows=ShadowQuality.All;QualitySettings.shadowResolution=ShadowResolution.Medium;QualitySettings.pixelLightCount=3;
            AssetDatabase.SaveAssets();Debug.Log("MIST_PREPARE_OK");
        }
        [MenuItem("雾灯遗城/重新生成原创模型")]
        public static void GenerateModels()
        {
            // Only these two folders contain generated data; .cs source and prefabs stay intact.
            if(AssetDatabase.IsValidFolder("Assets/Models/Meshes"))AssetDatabase.DeleteAsset("Assets/Models/Meshes");
            if(AssetDatabase.IsValidFolder("Assets/Models/Materials"))AssetDatabase.DeleteAsset("Assets/Models/Materials");
            Directory.CreateDirectory("Assets/Models/Meshes");Directory.CreateDirectory("Assets/Models/Materials");
            Directory.CreateDirectory("Assets/Resources/Models");Directory.CreateDirectory("Assets/Resources/Stages");
            AssetDatabase.Refresh();int count=0;
            AssetDatabase.StartAssetEditing();try{
            for(int i=0;i<3;i++)Export(Models.Character(-1,i),"Assets/Resources/Models/Hunter"+i+".prefab",ref count);
            for(int i=0;i<7;i++)Export(Models.Character(i),"Assets/Resources/Models/Enemy"+i+".prefab",ref count);
            for(int i=0;i<3;i++)Export(Models.Stage(i),"Assets/Resources/Stages/Area"+i+".prefab",ref count);
            }finally{AssetDatabase.StopAssetEditing();}
            AssetDatabase.SaveAssets();Debug.Log("MIST_MODELS_OK meshes="+count);
        }
        static void Export(GameObject root,string path,ref int count)
        {
            if(path.Contains("/Models/"))SculptReview.BakeCavities(root);
            foreach(var mf in root.GetComponentsInChildren<MeshFilter>())
            {
                var mesh=mf.sharedMesh;
                if(mesh&&!AssetDatabase.Contains(mesh))
                {string p=AssetDatabase.GenerateUniqueAssetPath("Assets/Models/Meshes/Sculpt_"+(count++)+".asset");AssetDatabase.CreateAsset(mesh,p);}
            }
            foreach(var renderer in root.GetComponentsInChildren<Renderer>())
            foreach(var m in renderer.sharedMaterials)
            {if(m&&!AssetDatabase.Contains(m))AssetDatabase.CreateAsset(m,AssetDatabase.GenerateUniqueAssetPath("Assets/Models/Materials/"+m.name+".mat"));}
            PrefabUtility.SaveAsPrefabAsset(root,path);UnityEngine.Object.DestroyImmediate(root);
        }
        public static void GenerateHunters()
        {
            int count=0;AssetDatabase.StartAssetEditing();try{for(int i=0;i<3;i++)Export(Models.Character(-1,i),"Assets/Resources/Models/Hunter"+i+".prefab",ref count);}finally{AssetDatabase.StopAssetEditing();}AssetDatabase.SaveAssets();
        }
        [MenuItem("雾灯遗城/生成可编辑模型陈列场景")]
        public static void Gallery()
        {
            Prepare();
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Flat;RenderSettings.ambientLight=Models.Hex("667B87");RenderSettings.fog=false;
            var camera=new GameObject("Gallery camera").AddComponent<Camera>();camera.orthographic=true;camera.orthographicSize=12.8f;
            camera.transform.position=new Vector3(0,23,-25);camera.transform.LookAt(new Vector3(0,1,5));camera.backgroundColor=Models.Hex("132A38");camera.clearFlags=CameraClearFlags.SolidColor;
            var light=new GameObject("Soft key").AddComponent<Light>();light.type=LightType.Directional;light.intensity=1.2f;light.transform.rotation=Quaternion.Euler(45,-25,0);light.shadows=LightShadows.Soft;
            var rim=new GameObject("Warm rim").AddComponent<Light>();rim.type=LightType.Directional;rim.color=Models.Ember;rim.intensity=.5f;rim.transform.rotation=Quaternion.Euler(35,140,0);
            var font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Resources/Fonts/Mist SDF.asset");
            for(int row=0;row<3;row++)
            {
                int count=row==1?4:3;
                for(int i=0;i<count;i++)
                {
                    int kind=row==0?-1:row==1?i:i+4;
                    string name=kind<0?"Hunter"+i:"Enemy"+kind;
                    var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/Models/"+name+".prefab");
                    var model=(GameObject)PrefabUtility.InstantiatePrefab(prefab);
                    float x=(i-(count-1)*.5f)*6.2f,z=row*6.5f;
                    model.transform.position=new Vector3(x,0,z);model.transform.rotation=Quaternion.Euler(0,-20,0);
                    // All showcase geometry is saved as a standalone scene, separate from gameplay.
                    var basePlate=GameObject.CreatePrimitive(PrimitiveType.Cylinder);basePlate.name=name+" pedestal";basePlate.transform.position=new Vector3(x,-.15f,z);basePlate.transform.localScale=new Vector3(row==2?4.7f:3.8f,.15f,row==2?4.7f:3.8f);
                    var pedestalMaterial=AssetDatabase.LoadAssetAtPath<Material>("Assets/Models/Materials/GalleryPedestal.mat");
                    if(!pedestalMaterial){pedestalMaterial=Sculpt.Material(Models.Hex("24363B"),Sculpt.Finish.Stone);AssetDatabase.CreateAsset(pedestalMaterial,"Assets/Models/Materials/GalleryPedestal.mat");}
                    basePlate.GetComponent<Renderer>().sharedMaterial=pedestalMaterial;
                    var label=new GameObject(name+" label").AddComponent<TextMeshPro>();label.font=font;label.text=row==0?Catalog.Weapons[i]:row==1?Catalog.EnemyNames[i]:Catalog.Bosses[i];
                    label.fontSize=4;label.alignment=TextAlignmentOptions.Center;label.color=Models.Ember;label.rectTransform.sizeDelta=new Vector2(5,1);
                    label.transform.position=new Vector3(x,.1f,z-2.2f);label.transform.rotation=camera.transform.rotation;
                }
            }
            EditorSceneManager.SaveScene(scene,"Assets/Scenes/ModelGallery.unity");
            Directory.CreateDirectory("docs/screenshots");var rt=new RenderTexture(2000,1400,24);rt.antiAliasing=2;camera.targetTexture=rt;
            using(var pose=new ModelGeometry.PoseBake())camera.Render();RenderTexture.active=rt;
            var image=new Texture2D(2000,1400,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,2000,1400),0,0);image.Apply();File.WriteAllBytes("docs/screenshots/model-gallery.png",image.EncodeToPNG());
            camera.targetTexture=null;RenderTexture.active=null;UnityEngine.Object.DestroyImmediate(image);rt.Release();UnityEngine.Object.DestroyImmediate(rt);
            EditorSceneManager.OpenScene(Scene);Debug.Log("MIST_GALLERY_OK");
        }
        [MenuItem("雾灯遗城/构建 macOS")]
        public static void Mac(){Prepare();Build(BuildTarget.StandaloneOSX,"Build/macOS/雾灯遗城.app");}
        [MenuItem("雾灯遗城/构建 WebGL")]
        public static void Web(){Prepare();Build(BuildTarget.WebGL,"Build/WebGL");}
        static void Build(BuildTarget target,string path)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{Scene},locationPathName=path,target=target,options=BuildOptions.None});
            if(report.summary.result!=BuildResult.Succeeded)throw new Exception("Build failed: "+report.summary.result);
            Debug.Log("MIST_BUILD_OK "+target+" bytes="+report.summary.totalSize);
        }
    }
}
