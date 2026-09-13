using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace PlanBuild.Client
{
    internal sealed class TargetHighlight : IDisposable
    {
        private Material material;
        private Mesh outline;
        private readonly MaterialPropertyBlock tint = new MaterialPropertyBlock();

        public void Draw(BlueprintProjection projection, BlueprintProjection.ProjectedPiece piece, Camera camera, bool buildable)
        {
            if (!material)
            {
                var shader = Shader.Find("Hidden/Internal-Colored");
                if (!shader) return;
                material = new Material(shader) { renderQueue = 3100 };
                material.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
                material.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
                material.SetInt("_Cull", (int)CullMode.Off);
                material.SetInt("_ZWrite", 0);
                material.SetInt("_ZTest", (int)CompareFunction.LessEqual);
                outline = new Mesh { name = "Buildheim target outline" };
                var vertices = new Vector3[8];
                for (int i = 0; i < vertices.Length; i++)
                    vertices[i] = new Vector3(i & 1, (i >> 1) & 1, (i >> 2) & 1);
                outline.vertices = vertices;
                outline.SetIndices(new[] { 0,1, 2,3, 4,5, 6,7, 0,2, 1,3, 4,6, 5,7, 0,4, 1,5, 2,6, 3,7 }, MeshTopology.Lines, 0);
                outline.RecalculateBounds();
            }
            var color = buildable ? new Color(0.4f, 1f, 0.2f, 0.5f) : new Color(1f, 0.8f, 0.1f, 0.5f);
            var matrix = projection.PieceMatrix(piece);
            tint.SetColor("_Color", color);
            foreach (var part in piece.Parts)
                for (int submesh = 0; submesh < part.Mesh.subMeshCount; submesh++)
                    Graphics.DrawMesh(part.Mesh, matrix * part.LocalMatrix, material, 0, camera, submesh,
                        tint, ShadowCastingMode.Off, false);
            color.a = 1;
            tint.SetColor("_Color", color);
            var bounds = piece.LocalBounds;
            bounds.Expand(0.04f);
            Graphics.DrawMesh(outline, matrix * Matrix4x4.TRS(bounds.min, Quaternion.identity, bounds.size),
                material, 0, camera, 0, tint, ShadowCastingMode.Off, false);
        }

        public void Dispose()
        {
            if (material) UnityEngine.Object.Destroy(material);
            if (outline) UnityEngine.Object.Destroy(outline);
        }
    }
}
