using System;
using System.Collections.Generic;
using System.Linq;
using Jotunn.Managers;
using UnityEngine;
using UnityEngine.Rendering;

namespace PlanBuild.Client
{
    // Projections borrow meshes only. Never instantiate a game prefab or attach a Piece/ZNetView.
    internal sealed class BlueprintProjection : IDisposable
    {
        internal sealed class Part
        {
            public Mesh Mesh { get; }
            public Matrix4x4 LocalMatrix { get; }
            public Part(Mesh mesh, Matrix4x4 matrix) { Mesh = mesh; LocalMatrix = matrix; }
        }

        internal sealed class ProjectedPiece
        {
            public Blueprints.PieceEntry Entry { get; }
            public GameObject Prefab { get; }
            public IReadOnlyList<Part> Parts { get; }
            public Bounds LocalBounds { get; }
            public bool Completed { get; set; }
            public ProjectedPiece(Blueprints.PieceEntry entry, GameObject prefab, IReadOnlyList<Part> parts)
            {
                Entry = entry; Prefab = prefab; Parts = parts;
                var bounds = new Bounds();
                bool first = true;
                foreach (var part in parts)
                {
                    var meshBounds = part.Mesh.bounds;
                    for (int corner = 0; corner < 8; corner++)
                    {
                        var offset = Vector3.Scale(meshBounds.extents, new Vector3(
                            (corner & 1) == 0 ? -1 : 1, (corner & 2) == 0 ? -1 : 1, (corner & 4) == 0 ? -1 : 1));
                        var point = part.LocalMatrix.MultiplyPoint3x4(meshBounds.center + offset);
                        if (first) { bounds = new Bounds(point, Vector3.zero); first = false; }
                        else bounds.Encapsulate(point);
                    }
                }
                LocalBounds = bounds;
            }
        }

        public string Name { get; }
        public IReadOnlyList<ProjectedPiece> Pieces { get; }
        public int MissingPrefabs { get; }
        public Vector3 Position { get; set; }
        public float Yaw { get; set; }
        public Quaternion Rotation => Quaternion.Euler(0, Yaw, 0);
        private readonly Material material;
        private readonly MaterialPropertyBlock highlight = new MaterialPropertyBlock();

        private BlueprintProjection(BlueprintDocument document, List<ProjectedPiece> pieces, int missing, Material material)
        {
            Name = document.Name;
            Pieces = pieces;
            MissingPrefabs = missing;
            this.material = material;
            highlight.SetColor("_Color", new Color(1f, 0.8f, 0.2f, 0.5f));
        }

        public static bool TryCreate(BlueprintDocument document, out BlueprintProjection projection, out string error)
        {
            projection = null;
            error = string.Empty;
            var shader = PrefabManager.Cache.GetPrefab<Shader>("Lux Lit Particles/ Bumped");
            if (!shader)
            {
                error = "The blueprint shader is unavailable. Rejoin the world and try again.";
                return false;
            }
            var pieces = new List<ProjectedPiece>();
            int missing = 0;
            foreach (var entry in document.Pieces)
            {
                var prefab = ZNetScene.instance.GetPrefab(entry.name);
                if (!prefab) { missing++; continue; }
                var parts = new List<Part>();
                var lowerLods = new HashSet<Renderer>(prefab.GetComponentsInChildren<LODGroup>(true)
                    .SelectMany(group => group.GetLODs().Skip(1)).SelectMany(lod => lod.renderers));
                foreach (var filter in prefab.GetComponentsInChildren<MeshFilter>(true))
                {
                    var renderer = filter.GetComponent<MeshRenderer>();
                    if (!filter.sharedMesh || !renderer || !renderer.enabled || lowerLods.Contains(renderer) ||
                        !ActiveBelowRoot(filter.transform, prefab.transform)) continue;
                    parts.Add(new Part(filter.sharedMesh, prefab.transform.worldToLocalMatrix * filter.transform.localToWorldMatrix));
                }
                if (parts.Count == 0) { missing++; continue; }
                pieces.Add(new ProjectedPiece(entry, prefab, parts));
            }
            if (pieces.Count == 0)
            {
                error = "No pieces in this blueprint can be displayed. Check its required piece mods.";
                return false;
            }
            var material = new Material(shader) { color = new Color(0.15f, 0.8f, 1f, 0.3f), renderQueue = 3000 };
            material.SetOverrideTag("RenderType", "Transparent");
            material.SetFloat("_ZWrite", 0);
            material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            material.DisableKeyword("DIRECTIONAL");
            projection = new BlueprintProjection(document, pieces, missing, material);
            return true;
        }

        private static bool ActiveBelowRoot(Transform node, Transform root)
        {
            while (node != root)
            {
                if (!node.gameObject.activeSelf) return false;
                node = node.parent;
            }
            return true;
        }

        public Vector3 PiecePosition(ProjectedPiece piece) => Position + Rotation * piece.Entry.GetPosition();
        public Quaternion PieceRotation(ProjectedPiece piece) => Rotation * piece.Entry.GetRotation();
        public Matrix4x4 PieceMatrix(ProjectedPiece piece) => Matrix4x4.TRS(PiecePosition(piece), PieceRotation(piece), piece.Entry.GetScale());

        public void Draw(ProjectedPiece selected = null)
        {
            var camera = GameCamera.instance ? GameCamera.instance.GetComponent<Camera>() : null;
            if (!camera) return;
            foreach (var piece in Pieces)
            {
                if (piece.Completed) continue;
                var matrix = PieceMatrix(piece);
                foreach (var part in piece.Parts)
                {
                    for (int submesh = 0; submesh < part.Mesh.subMeshCount; submesh++)
                        Graphics.DrawMesh(part.Mesh, matrix * part.LocalMatrix, material, 0, camera,
                            submesh, piece == selected ? highlight : null, ShadowCastingMode.Off, false);
                }
            }
        }

        public void Dispose() => UnityEngine.Object.Destroy(material);
    }
}
