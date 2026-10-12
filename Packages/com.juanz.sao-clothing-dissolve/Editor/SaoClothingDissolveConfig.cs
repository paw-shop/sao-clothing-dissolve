using UnityEngine;

namespace SaoClothingDissolve
{
    [CreateAssetMenu(menuName = "SAO Clothing Dissolve/Effect Config", fileName = "SAO Clothing Dissolve Config")]
    public sealed class SaoClothingDissolveConfig : ScriptableObject
    {
        [Header("Avatar Target")]
        [Tooltip("A SkinnedMeshRenderer from an avatar clothing item. This addon is not intended for VRChat worlds or Udon.")]
        public SkinnedMeshRenderer targetRenderer;

        [Tooltip("Optional source material to duplicate before adding dissolve properties.")]
        public Material sourceMaterialOverride;

        [Tooltip("Optional mesh shapes to use as emitted polygon fragments instead of sampled garment triangles.")]
        public Mesh[] customFragmentMeshes;

        [Header("Timing")]
        [Min(0.01f)] public float glowDuration = 0.45f;
        [Min(0.01f)] public float flashDuration = 0.12f;
        [Min(0.01f)] public float scatterDuration = 0.9f;

        [Header("Fragments")]
        [Range(4, 256)] public int fragmentCount = 48;
        [Min(0.001f)] public float fragmentScale = 0.055f;
        [Min(0f)] public float fragmentSpeed = 1.8f;
        [Range(0f, 2f)] public float spread = 0.75f;
        [Range(-1f, 1f)] public float upwardBias = 0.15f;
        public int randomSeed = 1729;

        [Header("Visuals")]
        public Color emissionColor = new Color(0.25f, 0.7f, 1f, 1f);
        [Min(0f)] public float emissionIntensity = 4f;
        [Range(0f, 1f)] public float dissolveEdgeWidth = 0.12f;

        [Header("Generated Avatar Assets")]
        public string generatedAssetFolder = "Assets/SAOClothingDissolve/Generated";
        public GameObject generatedRoot;
        public AnimationClip hideClip;
        public AnimationClip showClip;

        public float TotalHideDuration => glowDuration + flashDuration + scatterDuration;
    }
}
