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
                        var mesh=new Mesh();
                        // Remove mirrored ancestors only while baking. Skin baking
                        // otherwise folds the mirror into vertices and a static
                        // renderer under the same hierarchy would apply it twice.
                        var mirrored=new List<KeyValuePair<Transform,Vector3>>();
                        for(var parent=skin.transform;parent;parent=parent.parent){var scale=parent.localScale;if(scale.x<0||scale.y<0||scale.z<0){mirrored.Add(new KeyValuePair<Transform,Vector3>(parent,scale));parent.localScale=new Vector3(Mathf.Abs(scale.x),Mathf.Abs(scale.y),Mathf.Abs(scale.z));}}
                        try{skin.BakeMesh(mesh);}finally{foreach(var item in mirrored)item.Key.localScale=item.Value;}
                        var g=new GameObject("Review pose bake");temporary.Add(g);
                        g.transform.SetParent(skin.transform,false);g.AddComponent<MeshFilter>().sharedMesh=mesh;var renderer=g.AddComponent<MeshRenderer>();renderer.sharedMaterials=skin.sharedMaterials;var properties=new MaterialPropertyBlock();skin.GetPropertyBlock(properties);renderer.SetPropertyBlock(properties);
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
