using SaoClothingDissolve;
using UnityEditor;
using UnityEngine;

namespace SaoClothingDissolve.Editor
{
    internal static class SaoClothingDissolvePreview
    {
        private static SaoClothingDissolveConfig activeConfig;
        private static double startTime;

        public static void Preview(SaoClothingDissolveConfig config)
        {
            if (config == null || config.hideClip == null || config.targetRenderer == null)
            {
                EditorUtility.DisplayDialog("Preview Unavailable", "Generate the effect before previewing it.", "OK");
                return;
            }

            activeConfig = config;
            startTime = EditorApplication.timeSinceStartup;
            AnimationMode.StartAnimationMode();
            EditorApplication.update -= UpdatePreview;
            EditorApplication.update += UpdatePreview;
        }

        private static void UpdatePreview()
        {
            if (activeConfig == null || activeConfig.hideClip == null || activeConfig.targetRenderer == null)
            {
                Stop();
                return;
            }

            var root = FindAvatarRoot(activeConfig.targetRenderer.transform);
            var elapsed = (float)(EditorApplication.timeSinceStartup - startTime);
            AnimationMode.SampleAnimationClip(root.gameObject, activeConfig.hideClip, elapsed);
            SceneView.RepaintAll();

            if (elapsed >= activeConfig.hideClip.length)
            {
                Stop();
            }
        }

        private static void Stop()
        {
            EditorApplication.update -= UpdatePreview;
            if (AnimationMode.InAnimationMode())
            {
                AnimationMode.StopAnimationMode();
            }

            activeConfig = null;
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
    }
}

