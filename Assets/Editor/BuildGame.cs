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
            if(!File.Exists("Assets/Resources/Models/Hunter0.prefab"))GenerateModels();
            if(!File.Exists(Scene))
            {
                var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
                new GameObject("Mist Lantern scene marker");EditorSceneManager.SaveScene(scene,Scene);
            }
            EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(Scene,true)};
            PlayerSettings.companyName="Mist Lantern Studio";PlayerSettings.productName="雾灯遗城";PlayerSettings.bundleVersion="0.1.0";
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Standalone,"com.MistLantern.Game");
            PlayerSettings.defaultScreenWidth=1440;PlayerSettings.defaultScreenHeight=810;PlayerSettings.fullScreenMode=FullScreenMode.Windowed;PlayerSettings.resizableWindow=true;
            PlayerSettings.runInBackground=false;PlayerSettings.colorSpace=ColorSpace.Gamma;
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone,ScriptingImplementation.Mono2x);
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.WebGL,ScriptingImplementation.IL2CPP);
            PlayerSettings.WebGL.compressionFormat=WebGLCompressionFormat.Disabled;PlayerSettings.WebGL.memorySize=256;PlayerSettings.WebGL.dataCaching=true;
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
            for(int i=0;i<3;i++)Export(Models.Character(-1,i),"Assets/Resources/Models/Hunter"+i+".prefab",ref count);
            for(int i=0;i<7;i++)Export(Models.Character(i),"Assets/Resources/Models/Enemy"+i+".prefab",ref count);
            for(int i=0;i<3;i++)Export(Models.Stage(i),"Assets/Resources/Stages/Area"+i+".prefab",ref count);
            AssetDatabase.SaveAssets();Debug.Log("MIST_MODELS_OK meshes="+count);
        }
        static void Export(GameObject root,string path,ref int count)
        {
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
