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
            public BlueprintHitTest HitTest { get; }
            public Part(Mesh mesh, Matrix4x4 matrix, BlueprintHitTest hitTest)
            { Mesh = mesh; LocalMatrix = matrix; HitTest = hitTest; }
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
        public BlueprintLayers Layers { get; }
        public int MissingPrefabs { get; }
        // Bounding-sphere radius around the blueprint's origin, for AssistanceRange.
        public float Radius { get; }
        // True while any piece in an active layer is still unbuilt. The delegate is held rather than
        // rebuilt, because this is read every frame.
        public bool HasWork => AssistanceWork.HasWork(Pieces.Count, pieceState, Layers);
        private readonly Func<int, (bool Completed, float Height)> pieceState;
        public Vector3 Position { get; set; }
        public float Yaw { get; set; }
        public bool Enabled { get; set; } = true;
        public Quaternion Rotation => Quaternion.Euler(0, Yaw, 0);
        private readonly Material material;
        private readonly Material aimedMaterial;

        private BlueprintProjection(BlueprintDocument document, List<ProjectedPiece> pieces, int missing, Material material)
        {
            Name = document.Name;
            Pieces = pieces;
            Layers = new BlueprintLayers(pieces.Select(piece => piece.Entry.posY));
            MissingPrefabs = missing;
            foreach (var piece in pieces)
                Radius = Mathf.Max(Radius, AssistanceRange.PieceRadius(
                    piece.Entry.GetPosition(), piece.LocalBounds, piece.Entry.GetScale()));
            pieceState = index => (Pieces[index].Completed, Pieces[index].Entry.posY);
            this.material = material;
            aimedMaterial = new Material(material) { color = new Color(1f, 0.85f, 0.2f, 0.3f) };
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
            var hitTests = new Dictionary<Mesh, BlueprintHitTest>();
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
                    var mesh = filter.sharedMesh;
                    if (!hitTests.TryGetValue(mesh, out var hitTest))
                    {
                        // Some imported meshes discard CPU data. Use their individual mesh bounds
                        // as a fallback, never the empty volume around the whole piece.
                        hitTest = mesh.isReadable && Enumerable.Range(0, mesh.subMeshCount).All(i => mesh.GetTopology(i) == MeshTopology.Triangles)
                            ? new BlueprintHitTest(mesh.vertices, mesh.triangles) : null;
                        hitTests.Add(mesh, hitTest);
                    }
                    parts.Add(new Part(mesh, prefab.transform.worldToLocalMatrix * filter.transform.localToWorldMatrix, hitTest));
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

        public void Draw(ProjectedPiece selected = null, ProjectedPiece aimed = null)
        {
            var camera = GameCamera.instance ? GameCamera.instance.GetComponent<Camera>() : null;
            if (!camera) return;
            foreach (var piece in Pieces)
            {
                if (piece.Completed || !Layers.Contains(piece.Entry.posY) || piece == selected) continue;
                var matrix = PieceMatrix(piece);
                foreach (var part in piece.Parts)
                {
                    for (int submesh = 0; submesh < part.Mesh.subMeshCount; submesh++)
                        Graphics.DrawMesh(part.Mesh, matrix * part.LocalMatrix, piece == aimed ? aimedMaterial : material, 0, camera,
                            submesh, null, ShadowCastingMode.Off, false);
                }
            }
        }

        public void Dispose()
        {
            UnityEngine.Object.Destroy(material);
            UnityEngine.Object.Destroy(aimedMaterial);
        }
    }
}
