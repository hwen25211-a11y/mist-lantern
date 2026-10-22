using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Mist.Editor
{
    public static class ReleasePreparation
    {
        public static void Run()
        {
            WardenImport.Run();
            ModelChecks.Run();
            RuleChecks.Run();
            CombatChecks.Run();
            ModelExport.Run();
            BuildGame.Gallery();
            CleanUnusedGeneratedAssets();
            Debug.Log("RELEASE_PREPARATION_OK");
        }

        public static void CleanUnusedGeneratedAssets()
        {
            var all=AssetDatabase.GetAllAssetPaths();
            var roots=all.Where(p=>p.StartsWith("Assets/Resources/")||p.EndsWith(".prefab")||p.EndsWith(".unity")).ToArray();
            var used=new HashSet<string>(AssetDatabase.GetDependencies(roots,true));
            var candidates=all.Where(p=>(p.StartsWith("Assets/Models/Meshes/")||p.StartsWith("Assets/Models/Materials/")||p.StartsWith("Assets/Characters/Warden/"))&&!AssetDatabase.IsValidFolder(p)&&!used.Contains(p)).ToArray();
            AssetDatabase.StartAssetEditing();
            try{foreach(var p in candidates)if(!AssetDatabase.DeleteAsset(p))throw new Exception("Cannot remove unused generated asset "+p);}
            finally{AssetDatabase.StopAssetEditing();}
            AssetDatabase.SaveAssets();
            File.WriteAllText("docs/qa/character-rebuild/generated-assets.txt","Removed unreferenced generated assets: "+candidates.Length+"\nAll Resources and prefab/scene dependencies retained.\n");
        }
    }
}
