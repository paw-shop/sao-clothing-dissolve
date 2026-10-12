using System.Collections.Generic;
using SaoClothingDissolve;

namespace SaoClothingDissolve.Editor
{
    internal static class SaoClothingDissolveValidator
    {
        public static List<string> Validate(SaoClothingDissolveConfig config)
        {
            var issues = new List<string>();

            if (config == null)
            {
                issues.Add("Missing effect config.");
                return issues;
            }

            if (config.targetRenderer == null)
            {
                issues.Add("Missing target SkinnedMeshRenderer.");
            }
            else if (config.targetRenderer.sharedMesh == null)
            {
                issues.Add("Target SkinnedMeshRenderer has no shared mesh.");
            }

            if (config.fragmentCount < 4)
            {
                issues.Add("Fragment count must be at least 4.");
            }

            if (config.fragmentScale <= 0f)
            {
                issues.Add("Fragment scale must be greater than 0.");
            }

            if (string.IsNullOrWhiteSpace(config.generatedAssetFolder) || !config.generatedAssetFolder.StartsWith("Assets"))
            {
                issues.Add("Generated asset folder must be inside Assets.");
            }

            return issues;
        }
    }
}

