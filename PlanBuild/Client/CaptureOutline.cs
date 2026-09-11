using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace PlanBuild.Client
{
    internal sealed class CaptureOutline : IDisposable
    {
        private Mesh mesh;
        private Material material;
        private readonly MaterialPropertyBlock color = new MaterialPropertyBlock();

        public void Draw(Vector3? first, Vector3? second)
        {
            if ((!first.HasValue && !second.HasValue) || !GameCamera.instance) return;
            if (!material)
            {
                var shader = Shader.Find("Hidden/Internal-Colored");
                if (!shader) return;
                material = new Material(shader) { renderQueue = 4000 };
                material.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
                material.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
                material.SetInt("_Cull", (int)CullMode.Off);
                material.SetInt("_ZWrite", 0);
                material.SetInt("_ZTest", (int)CompareFunction.Always);
                mesh = new Mesh { name = "Buildheim capture box" };
                var vertices = new Vector3[8];
                for (int i = 0; i < vertices.Length; i++)
                    vertices[i] = new Vector3(i & 1, (i >> 1) & 1, (i >> 2) & 1);
                mesh.vertices = vertices;
                mesh.SetIndices(new[] { 0,1, 2,3, 4,5, 6,7, 0,2, 1,3, 4,6, 5,7, 0,4, 1,5, 2,6, 3,7 }, MeshTopology.Lines, 0);
                mesh.RecalculateBounds();
            }
            var camera = GameCamera.instance.GetComponent<Camera>();
            if (first.HasValue && second.HasValue)
                Box(Vector3.Min(first.Value, second.Value), Vector3.Max(first.Value, second.Value) - Vector3.Min(first.Value, second.Value),
                    new Color(0.3f, 0.9f, 1f, 0.8f), camera);
            if (first.HasValue) Box(first.Value - Vector3.one * 0.12f, Vector3.one * 0.24f, Color.cyan, camera);
            if (second.HasValue) Box(second.Value - Vector3.one * 0.12f, Vector3.one * 0.24f, new Color(1, 0.65f, 0.15f), camera);
        }

        private void Box(Vector3 position, Vector3 size, Color tint, Camera camera)
        {
            color.SetColor("_Color", tint);
            Graphics.DrawMesh(mesh, Matrix4x4.TRS(position, Quaternion.identity, size), material, 0, camera,
                0, color, ShadowCastingMode.Off, false);
        }

        public void Dispose()
        {
            if (mesh) UnityEngine.Object.Destroy(mesh);
            if (material) UnityEngine.Object.Destroy(material);
        }
    }
}
