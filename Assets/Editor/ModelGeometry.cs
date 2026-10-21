using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Mist.Editor
{
    // Editor inspection must include the deforming body as well as rigid props.
    public static class ModelGeometry
    {
        public static Renderer[] Renderers(GameObject root) { return root.GetComponentsInChildren<Renderer>().Where(r=>r.enabled&&(r is MeshRenderer||r is SkinnedMeshRenderer)).ToArray(); }
        public static Mesh SharedMesh(Renderer renderer) { var skin=renderer as SkinnedMeshRenderer;return skin?skin.sharedMesh:renderer.GetComponent<MeshFilter>().sharedMesh; }
        public static Bounds WorldBounds(GameObject root)
        {
            bool first=true;var bounds=new Bounds(root.transform.position,Vector3.zero);
            foreach(var r in Renderers(root)) {
                var skin=r as SkinnedMeshRenderer;var mesh=skin?new Mesh():SharedMesh(r);
                if(skin)skin.BakeMesh(mesh);
                foreach(var vertex in mesh.vertices){var p=r.transform.TransformPoint(vertex);if(first){bounds=new Bounds(p,Vector3.zero);first=false;}else bounds.Encapsulate(p);}
                if(skin)UnityEngine.Object.DestroyImmediate(mesh);
            }
            return bounds;
        }
        public sealed class PoseBake : IDisposable
        {
            readonly List<SkinnedMeshRenderer> skins=new List<SkinnedMeshRenderer>();
            readonly List<GameObject> temporary=new List<GameObject>();
            public PoseBake()
            {
                try {
                    foreach(var skin in UnityEngine.Object.FindObjectsOfType<SkinnedMeshRenderer>().Where(s=>s.enabled)) {
                        var mesh=new Mesh();skin.BakeMesh(mesh);var g=new GameObject("Review pose bake");temporary.Add(g);
                        g.transform.SetParent(skin.transform,false);g.AddComponent<MeshFilter>().sharedMesh=mesh;g.AddComponent<MeshRenderer>().sharedMaterials=skin.sharedMaterials;
                        skins.Add(skin);skin.enabled=false;
                    }
                } catch {Dispose();throw;}
            }
            public void Dispose()
            {
                foreach(var g in temporary)if(g){var f=g.GetComponent<MeshFilter>();if(f&&f.sharedMesh)UnityEngine.Object.DestroyImmediate(f.sharedMesh);UnityEngine.Object.DestroyImmediate(g);}
                foreach(var skin in skins)if(skin)skin.enabled=true;
                temporary.Clear();skins.Clear();
            }
        }
    }
}
