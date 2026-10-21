using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;
namespace Mist.Editor
{
    public static class VisualReview
    {
        public static void Run()
        {
            BuildGame.Prepare();RuleChecks.Run();CombatChecks.Run();
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            UI.Font=Resources.Load<TMPro.TMP_FontAsset>("Fonts/Mist SDF");
            var world=new GameObject("Review").AddComponent<World>();world.Setup();
            for(int area=0;area<3;area++)
            {
                var s=Simulation.NewRun(42,area,new Profile());s.room=area*4+3;var sim=new Simulation(s,new Profile());sim.Enter();s.pos=new Vector2(-3,0);s.enemies[0].pos=new Vector2(3,0);
                world.ResetModels();world.Render(s,.016f,true);Capture(world.cam,"docs/screenshots/v2-area-"+area+".png");
            }
            EditorSceneManager.OpenScene(BuildGame.Scene);Debug.Log("VISUAL_REVIEW_OK");
        }
        public static void Capture(Camera camera,string path)
        {
            var rt=new RenderTexture(1600,900,24);rt.antiAliasing=2;camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;
            var texture=new Texture2D(1600,900,TextureFormat.RGB24,false);texture.ReadPixels(new Rect(0,0,1600,900),0,0);texture.Apply();File.WriteAllBytes(path,texture.EncodeToPNG());
            camera.targetTexture=null;RenderTexture.active=null;Object.DestroyImmediate(texture);rt.Release();Object.DestroyImmediate(rt);
        }
    }
}
