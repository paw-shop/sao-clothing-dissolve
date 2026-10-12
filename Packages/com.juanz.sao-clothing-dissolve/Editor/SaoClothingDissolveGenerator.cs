using System.Collections.Generic;
using SaoClothingDissolve;
using UnityEditor;
using UnityEngine;

namespace SaoClothingDissolve.Editor
{
    internal static class SaoClothingDissolveGenerator
    {
        private const string GeneratedRootName = "SAO Clothing Dissolve Generated";
        private const string ShaderName = "SAO Clothing Dissolve/Glow Dissolve";

        public static void Generate(SaoClothingDissolveConfig config)
        {
            EnsureFolder(config.generatedAssetFolder);
            var safeName = Sanitize(config.targetRenderer.name);
            var assetFolder = $"{config.generatedAssetFolder}/{safeName}";
            if (AssetDatabase.IsValidFolder(assetFolder))
            {
                AssetDatabase.DeleteAsset(assetFolder);
            }

            EnsureFolder(assetFolder);

            var avatarRoot = FindAvatarRoot(config.targetRenderer.transform);
            var effectRoot = GetOrCreateEffectRoot(config, avatarRoot, safeName);
            Undo.RegisterFullObjectHierarchyUndo(effectRoot, "Generate SAO Clothing Dissolve");

            ClearChildren(effectRoot.transform);

            var glowMaterial = CreateGlowMaterial(config, assetFolder);
            var fragmentMaterial = CreateFragmentMaterial(config, assetFolder);
            AssignGeneratedMaterial(config, glowMaterial);
            var fragmentData = BuildFragments(config, assetFolder, effectRoot.transform, fragmentMaterial);

            var hideClip = CreateHideClip(config, avatarRoot, effectRoot.transform, fragmentData, glowMaterial, assetFolder);
            var showClip = CreateShowClip(config, avatarRoot, effectRoot.transform, fragmentData, glowMaterial, assetFolder);

            config.generatedRoot = effectRoot;
            config.hideClip = hideClip;
            config.showClip = showClip;
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
        }

        public static void Clean(SaoClothingDissolveConfig config)
        {
            if (config == null)
            {
                return;
            }

            if (config.generatedRoot != null)
            {
                Undo.DestroyObjectImmediate(config.generatedRoot);
                config.generatedRoot = null;
            }

            if (config.targetRenderer != null)
            {
                var folder = $"{config.generatedAssetFolder}/{Sanitize(config.targetRenderer.name)}";
                if (AssetDatabase.IsValidFolder(folder))
                {
                    AssetDatabase.DeleteAsset(folder);
                }
            }

            config.hideClip = null;
            config.showClip = null;
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
        }

        private static Material CreateGlowMaterial(SaoClothingDissolveConfig config, string folder)
        {
            var shader = Shader.Find(ShaderName);
            if (shader == null)
            {
                shader = Shader.Find("Unlit/Color");
            }

            var source = config.sourceMaterialOverride != null ? config.sourceMaterialOverride : FirstSharedMaterial(config.targetRenderer);
            var material = source != null ? new Material(source) : new Material(shader);
            material.name = $"{config.targetRenderer.name}_SAO_Glow";
            material.shader = shader;
            ApplyColor(material, "_EmissionColor", config.emissionColor * config.emissionIntensity);
            ApplyColor(material, "_Color", Color.white);
            ApplyFloat(material, "_Dissolve", 0f);
            ApplyFloat(material, "_EdgeWidth", config.dissolveEdgeWidth);

            var path = AssetDatabase.GenerateUniqueAssetPath($"{folder}/{material.name}.mat");
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        private static Material CreateFragmentMaterial(SaoClothingDissolveConfig config, string folder)
        {
            var shader = Shader.Find("Unlit/Color");
            var material = new Material(shader)
            {
                name = $"{config.targetRenderer.name}_SAO_Fragment",
                color = config.emissionColor
            };
            ApplyColor(material, "_Color", config.emissionColor);

            var path = AssetDatabase.GenerateUniqueAssetPath($"{folder}/{material.name}.mat");
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        private static List<FragmentData> BuildFragments(SaoClothingDissolveConfig config, string folder, Transform effectRoot, Material material)
        {
            var bakedMesh = new Mesh { name = $"{config.targetRenderer.name}_BakedForFragments" };
            config.targetRenderer.BakeMesh(bakedMesh);

            var vertices = bakedMesh.vertices;
            var normals = bakedMesh.normals;
            var triangles = bakedMesh.triangles;
            var random = new System.Random(config.randomSeed);
            var fragments = new List<FragmentData>(config.fragmentCount);

            for (var i = 0; i < config.fragmentCount; i++)
            {
                var triangleIndex = random.Next(0, triangles.Length / 3) * 3;
                var customMesh = PickCustomMesh(config, random);
                var mesh = customMesh != null ? customMesh : CreateTriangleFragmentMesh(vertices, normals, triangles, triangleIndex, config.fragmentScale);
                var visibleScale = customMesh != null ? Vector3.one * config.fragmentScale : Vector3.one;
                if (customMesh == null)
                {
                    var meshPath = AssetDatabase.GenerateUniqueAssetPath($"{folder}/{config.targetRenderer.name}_Fragment_{i:000}.asset");
                    AssetDatabase.CreateAsset(mesh, meshPath);
                }

                var child = new GameObject($"Fragment_{i:000}");
                Undo.RegisterCreatedObjectUndo(child, "Create SAO fragment");
                child.transform.SetParent(effectRoot, false);

                var center = config.targetRenderer.transform.TransformPoint(GetTriangleCenter(vertices, triangles, triangleIndex));
                child.transform.position = center;
                child.transform.rotation = RandomRotation(random);
                child.transform.localScale = Vector3.zero;

                var filter = child.AddComponent<MeshFilter>();
                filter.sharedMesh = mesh;
                var renderer = child.AddComponent<MeshRenderer>();
                renderer.sharedMaterial = material;
                renderer.enabled = false;

                var direction = ComputeDirection(config, center, effectRoot.position, random);
                var startLocal = child.transform.localPosition;
                var endLocal = effectRoot.InverseTransformPoint(center + direction * config.fragmentSpeed);

                fragments.Add(new FragmentData(child.transform, renderer, startLocal, endLocal, child.transform.localRotation, visibleScale));
            }

            Object.DestroyImmediate(bakedMesh);
            return fragments;
        }

        private static Mesh CreateTriangleFragmentMesh(Vector3[] vertices, Vector3[] normals, int[] triangles, int triangleIndex, float scale)
        {
            var a = vertices[triangles[triangleIndex]];
            var b = vertices[triangles[triangleIndex + 1]];
            var c = vertices[triangles[triangleIndex + 2]];
            var center = (a + b + c) / 3f;
            var n = normals != null && normals.Length > 0 ? (normals[triangles[triangleIndex]] + normals[triangles[triangleIndex + 1]] + normals[triangles[triangleIndex + 2]]).normalized : Vector3.up;

            var mesh = new Mesh { name = "SAO_TriangleFragment" };
            mesh.vertices = new[]
            {
                (a - center) * scale,
                (b - center) * scale,
                (c - center) * scale
            };
            mesh.normals = new[] { n, n, n };
            mesh.triangles = new[] { 0, 1, 2 };
            mesh.RecalculateBounds();
            return mesh;
        }

        private static AnimationClip CreateHideClip(SaoClothingDissolveConfig config, Transform avatarRoot, Transform effectRoot, List<FragmentData> fragments, Material glowMaterial, string folder)
        {
            var clip = new AnimationClip
            {
                name = $"{config.targetRenderer.name}_SAO_Hide",
                frameRate = 60f
            };

            var glowEnd = config.glowDuration;
            var flashEnd = glowEnd + config.flashDuration;
            var total = config.TotalHideDuration;
            var targetPath = AnimationUtility.CalculateTransformPath(config.targetRenderer.transform, avatarRoot);

            SetRendererEnabledCurve(clip, targetPath, typeof(SkinnedMeshRenderer), 0f, true, total, false);
            SetMaterialFloatCurve(clip, targetPath, "_Dissolve", 0f, 0f, total, 1f);
            SetMaterialColorCurve(clip, targetPath, "_EmissionColor", config.emissionColor * 0.1f, glowEnd, config.emissionColor * config.emissionIntensity);

            foreach (var fragment in fragments)
            {
                var path = AnimationUtility.CalculateTransformPath(fragment.transform, avatarRoot);
                SetRendererEnabledCurve(clip, path, typeof(MeshRenderer), 0f, false, flashEnd, true, total, false);
                SetVector3Curve(clip, path, "m_LocalPosition", flashEnd, fragment.startLocal, total, fragment.endLocal);
                SetVector3Curve(clip, path, "m_LocalScale", 0f, Vector3.zero, flashEnd, fragment.visibleScale, total, Vector3.zero);
                SetQuaternionCurve(clip, path, fragment.startRotation, total);
            }

            var pathOut = AssetDatabase.GenerateUniqueAssetPath($"{folder}/{clip.name}.anim");
            AssetDatabase.CreateAsset(clip, pathOut);
            return clip;
        }

        private static AnimationClip CreateShowClip(SaoClothingDissolveConfig config, Transform avatarRoot, Transform effectRoot, List<FragmentData> fragments, Material glowMaterial, string folder)
        {
            var clip = new AnimationClip
            {
                name = $"{config.targetRenderer.name}_SAO_Show",
                frameRate = 60f
            };

            var total = Mathf.Max(0.2f, config.glowDuration + config.flashDuration);
            var targetPath = AnimationUtility.CalculateTransformPath(config.targetRenderer.transform, avatarRoot);
            SetRendererEnabledCurve(clip, targetPath, typeof(SkinnedMeshRenderer), 0f, true, total, true);
            SetMaterialFloatCurve(clip, targetPath, "_Dissolve", 0f, 1f, total, 0f);
            SetMaterialColorCurve(clip, targetPath, "_EmissionColor", config.emissionColor * config.emissionIntensity, total, config.emissionColor * 0.1f);

            foreach (var fragment in fragments)
            {
                var path = AnimationUtility.CalculateTransformPath(fragment.transform, avatarRoot);
                SetRendererEnabledCurve(clip, path, typeof(MeshRenderer), 0f, false, total, false);
                SetVector3Curve(clip, path, "m_LocalScale", 0f, Vector3.zero, total, Vector3.zero);
            }

            var pathOut = AssetDatabase.GenerateUniqueAssetPath($"{folder}/{clip.name}.anim");
            AssetDatabase.CreateAsset(clip, pathOut);
            return clip;
        }

        private static void SetRendererEnabledCurve(AnimationClip clip, string path, System.Type type, float t0, bool v0, float t1, bool v1)
        {
            var curve = new AnimationCurve(new Keyframe(t0, v0 ? 1f : 0f), new Keyframe(t1, v1 ? 1f : 0f));
            clip.SetCurve(path, type, "m_Enabled", curve);
        }

        private static void SetRendererEnabledCurve(AnimationClip clip, string path, System.Type type, float t0, bool v0, float t1, bool v1, float t2, bool v2)
        {
            var curve = new AnimationCurve(new Keyframe(t0, v0 ? 1f : 0f), new Keyframe(t1, v1 ? 1f : 0f), new Keyframe(t2, v2 ? 1f : 0f));
            clip.SetCurve(path, type, "m_Enabled", curve);
        }

        private static void SetMaterialFloatCurve(AnimationClip clip, string path, string property, float t0, float v0, float t1, float v1)
        {
            var curve = AnimationCurve.Linear(t0, v0, t1, v1);
            clip.SetCurve(path, typeof(SkinnedMeshRenderer), $"material.{property}", curve);
        }

        private static void SetMaterialColorCurve(AnimationClip clip, string path, string property, Color start, float t1, Color end)
        {
            clip.SetCurve(path, typeof(SkinnedMeshRenderer), $"material.{property}.r", AnimationCurve.Linear(0f, start.r, t1, end.r));
            clip.SetCurve(path, typeof(SkinnedMeshRenderer), $"material.{property}.g", AnimationCurve.Linear(0f, start.g, t1, end.g));
            clip.SetCurve(path, typeof(SkinnedMeshRenderer), $"material.{property}.b", AnimationCurve.Linear(0f, start.b, t1, end.b));
            clip.SetCurve(path, typeof(SkinnedMeshRenderer), $"material.{property}.a", AnimationCurve.Linear(0f, start.a, t1, end.a));
        }

        private static void SetVector3Curve(AnimationClip clip, string path, string property, float t0, Vector3 v0, float t1, Vector3 v1)
        {
            clip.SetCurve(path, typeof(Transform), $"{property}.x", AnimationCurve.Linear(t0, v0.x, t1, v1.x));
            clip.SetCurve(path, typeof(Transform), $"{property}.y", AnimationCurve.Linear(t0, v0.y, t1, v1.y));
            clip.SetCurve(path, typeof(Transform), $"{property}.z", AnimationCurve.Linear(t0, v0.z, t1, v1.z));
        }

        private static void SetVector3Curve(AnimationClip clip, string path, string property, float t0, Vector3 v0, float t1, Vector3 v1, float t2, Vector3 v2)
        {
            clip.SetCurve(path, typeof(Transform), $"{property}.x", new AnimationCurve(new Keyframe(t0, v0.x), new Keyframe(t1, v1.x), new Keyframe(t2, v2.x)));
            clip.SetCurve(path, typeof(Transform), $"{property}.y", new AnimationCurve(new Keyframe(t0, v0.y), new Keyframe(t1, v1.y), new Keyframe(t2, v2.y)));
            clip.SetCurve(path, typeof(Transform), $"{property}.z", new AnimationCurve(new Keyframe(t0, v0.z), new Keyframe(t1, v1.z), new Keyframe(t2, v2.z)));
        }

        private static void SetQuaternionCurve(AnimationClip clip, string path, Quaternion start, float total)
        {
            var end = start * Quaternion.Euler(220f, 480f, 130f);
            clip.SetCurve(path, typeof(Transform), "m_LocalRotation.x", AnimationCurve.Linear(0f, start.x, total, end.x));
            clip.SetCurve(path, typeof(Transform), "m_LocalRotation.y", AnimationCurve.Linear(0f, start.y, total, end.y));
            clip.SetCurve(path, typeof(Transform), "m_LocalRotation.z", AnimationCurve.Linear(0f, start.z, total, end.z));
            clip.SetCurve(path, typeof(Transform), "m_LocalRotation.w", AnimationCurve.Linear(0f, start.w, total, end.w));
        }

        private static GameObject GetOrCreateEffectRoot(SaoClothingDissolveConfig config, Transform avatarRoot, string safeName)
        {
            var rootName = $"{GeneratedRootName}/{safeName}";
            var existing = avatarRoot.Find(rootName);
            if (existing != null)
            {
                return existing.gameObject;
            }

            var container = avatarRoot.Find(GeneratedRootName);
            if (container == null)
            {
                var containerObject = new GameObject(GeneratedRootName);
                Undo.RegisterCreatedObjectUndo(containerObject, "Create SAO generated root");
                containerObject.transform.SetParent(avatarRoot, false);
                container = containerObject.transform;
            }

            var root = new GameObject(safeName);
            Undo.RegisterCreatedObjectUndo(root, "Create SAO generated garment root");
            root.transform.SetParent(container, false);
            return root;
        }

        private static Transform FindAvatarRoot(Transform target)
        {
            var animator = target.GetComponentInParent<Animator>();
            if (animator != null)
            {
                return animator.transform;
            }

            var current = target;
            while (current.parent != null)
            {
                current = current.parent;
            }

            return current;
        }

        private static void ClearChildren(Transform root)
        {
            for (var i = root.childCount - 1; i >= 0; i--)
            {
                Undo.DestroyObjectImmediate(root.GetChild(i).gameObject);
            }
        }

        private static Vector3 ComputeDirection(SaoClothingDissolveConfig config, Vector3 center, Vector3 origin, System.Random random)
        {
            var radial = (center - origin).normalized;
            if (radial.sqrMagnitude < 0.001f)
            {
                radial = Vector3.up;
            }

            var jitter = new Vector3(RandomSigned(random), RandomSigned(random), RandomSigned(random)).normalized * config.spread;
            var direction = (radial + jitter + Vector3.up * config.upwardBias).normalized;
            return direction.sqrMagnitude > 0.001f ? direction : Vector3.up;
        }

        private static Quaternion RandomRotation(System.Random random)
        {
            return Quaternion.Euler((float)random.NextDouble() * 360f, (float)random.NextDouble() * 360f, (float)random.NextDouble() * 360f);
        }

        private static float RandomSigned(System.Random random)
        {
            return ((float)random.NextDouble() * 2f) - 1f;
        }

        private static Vector3 GetTriangleCenter(Vector3[] vertices, int[] triangles, int triangleIndex)
        {
            return (vertices[triangles[triangleIndex]] + vertices[triangles[triangleIndex + 1]] + vertices[triangles[triangleIndex + 2]]) / 3f;
        }

        private static Mesh PickCustomMesh(SaoClothingDissolveConfig config, System.Random random)
        {
            if (config.customFragmentMeshes == null || config.customFragmentMeshes.Length == 0)
            {
                return null;
            }

            for (var attempts = 0; attempts < config.customFragmentMeshes.Length; attempts++)
            {
                var mesh = config.customFragmentMeshes[random.Next(0, config.customFragmentMeshes.Length)];
                if (mesh != null)
                {
                    return mesh;
                }
            }

            return null;
        }

        private static Material FirstSharedMaterial(Renderer renderer)
        {
            return renderer.sharedMaterials != null && renderer.sharedMaterials.Length > 0 ? renderer.sharedMaterials[0] : null;
        }

        private static void AssignGeneratedMaterial(SaoClothingDissolveConfig config, Material material)
        {
            Undo.RecordObject(config.targetRenderer, "Assign SAO dissolve material");
            var materials = config.targetRenderer.sharedMaterials;
            if (materials == null || materials.Length == 0)
            {
                config.targetRenderer.sharedMaterial = material;
                return;
            }

            materials[0] = material;
            config.targetRenderer.sharedMaterials = materials;
            EditorUtility.SetDirty(config.targetRenderer);
        }

        private static void ApplyColor(Material material, string name, Color color)
        {
            if (material.HasProperty(name))
            {
                material.SetColor(name, color);
            }
        }

        private static void ApplyFloat(Material material, string name, float value)
        {
            if (material.HasProperty(name))
            {
                material.SetFloat(name, value);
            }
        }

        private static void EnsureFolder(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder))
            {
                return;
            }

            var parts = folder.Split('/');
            var current = parts[0];
            for (var i = 1; i < parts.Length; i++)
            {
                var next = $"{current}/{parts[i]}";
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[i]);
                }

                current = next;
            }
        }

        private static string Sanitize(string value)
        {
            foreach (var c in System.IO.Path.GetInvalidFileNameChars())
            {
                value = value.Replace(c, '_');
            }

            return value.Replace(' ', '_');
        }

        private readonly struct FragmentData
        {
            public readonly Transform transform;
            public readonly MeshRenderer renderer;
            public readonly Vector3 startLocal;
            public readonly Vector3 endLocal;
            public readonly Quaternion startRotation;
            public readonly Vector3 visibleScale;

            public FragmentData(Transform transform, MeshRenderer renderer, Vector3 startLocal, Vector3 endLocal, Quaternion startRotation, Vector3 visibleScale)
            {
                this.transform = transform;
                this.renderer = renderer;
                this.startLocal = startLocal;
                this.endLocal = endLocal;
                this.startRotation = startRotation;
                this.visibleScale = visibleScale;
            }
        }
    }
}

