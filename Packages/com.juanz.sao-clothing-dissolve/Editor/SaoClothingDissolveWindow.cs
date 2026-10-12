using System.Collections.Generic;
using SaoClothingDissolve;
using UnityEditor;
using UnityEngine;

namespace SaoClothingDissolve.Editor
{
    public sealed class SaoClothingDissolveWindow : EditorWindow
    {
        private SaoClothingDissolveConfig config;
        private UnityEditor.Editor configEditor;
        private Vector2 scroll;

        [MenuItem("Tools/SAO Clothing Dissolve")]
        public static void Open()
        {
            GetWindow<SaoClothingDissolveWindow>("SAO Clothing Dissolve");
        }

        private void OnGUI()
        {
            scroll = EditorGUILayout.BeginScrollView(scroll);
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("SAO Clothing Dissolve", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Avatar-only generator for VRChat Avatars SDK projects. It creates animation clips, materials, meshes, and avatar child objects; it does not use Udon or world behaviours.", MessageType.Info);

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                config = (SaoClothingDissolveConfig)EditorGUILayout.ObjectField("Effect Config", config, typeof(SaoClothingDissolveConfig), false);

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Create Config"))
                    {
                        CreateConfig();
                    }

                    using (new EditorGUI.DisabledScope(config == null))
                    {
                        if (GUILayout.Button("Select Asset"))
                        {
                            Selection.activeObject = config;
                            EditorGUIUtility.PingObject(config);
                        }
                    }
                }
            }

            if (config != null)
            {
                DrawConfig();
                DrawActions();
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawConfig()
        {
            if (configEditor == null || configEditor.target != config)
            {
                UnityEditor.Editor.CreateCachedEditor(config, null, ref configEditor);
            }

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                configEditor.OnInspectorGUI();
            }
        }

        private void DrawActions()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Actions", EditorStyles.boldLabel);

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Validate"))
                    {
                        ShowValidation();
                    }

                    if (GUILayout.Button("Generate / Update"))
                    {
                        Generate();
                    }
                }

                using (new EditorGUI.DisabledScope(config.generatedRoot == null))
                {
                    if (GUILayout.Button("Preview In Editor"))
                    {
                        SaoClothingDissolvePreview.Preview(config);
                    }
                }

                if (GUILayout.Button("Clean Generated Assets"))
                {
                    CleanGeneratedAssets();
                }
            }
        }

        private void CreateConfig()
        {
            var folder = EditorUtility.SaveFilePanelInProject("Create SAO Clothing Dissolve Config", "SAO Clothing Dissolve Config", "asset", "Choose where to save the config asset.");
            if (string.IsNullOrEmpty(folder))
            {
                return;
            }

            var asset = CreateInstance<SaoClothingDissolveConfig>();
            if (Selection.activeGameObject != null)
            {
                asset.targetRenderer = Selection.activeGameObject.GetComponentInChildren<SkinnedMeshRenderer>();
            }

            AssetDatabase.CreateAsset(asset, folder);
            AssetDatabase.SaveAssets();
            config = asset;
            Selection.activeObject = asset;
        }

        private void ShowValidation()
        {
            var issues = SaoClothingDissolveValidator.Validate(config);
            if (issues.Count == 0)
            {
                EditorUtility.DisplayDialog("SAO Clothing Dissolve", "Validation passed.", "OK");
                return;
            }

            EditorUtility.DisplayDialog("SAO Clothing Dissolve", string.Join("\n", issues), "OK");
        }

        private void Generate()
        {
            var issues = SaoClothingDissolveValidator.Validate(config);
            if (issues.Count > 0)
            {
                EditorUtility.DisplayDialog("Cannot Generate", string.Join("\n", issues), "OK");
                return;
            }

            try
            {
                SaoClothingDissolveGenerator.Generate(config);
                EditorUtility.SetDirty(config);
                AssetDatabase.SaveAssets();
                EditorUtility.DisplayDialog("SAO Clothing Dissolve", "Generated assets updated.", "OK");
            }
            catch (System.Exception ex)
            {
                Debug.LogException(ex);
                EditorUtility.DisplayDialog("Generation Failed", ex.Message, "OK");
            }
        }

        private void CleanGeneratedAssets()
        {
            if (!EditorUtility.DisplayDialog("Clean Generated Assets", "This deletes only assets referenced by this config in its generated folder.", "Clean", "Cancel"))
            {
                return;
            }

            SaoClothingDissolveGenerator.Clean(config);
        }
    }
}

