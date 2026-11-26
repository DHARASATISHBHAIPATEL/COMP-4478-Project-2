using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Tether.EditorTools
{
    /// <summary>
    /// On first import, creates Assets/Tether/Tether.unity, adds it to Build
    /// Settings and opens it, so the project is ready to press Play or Build.
    /// The game itself is created from code at runtime (see Game.cs).
    /// </summary>
    [InitializeOnLoad]
    static class TetherSetup
    {
        const string ScenePath = "Assets/Tether/Tether.unity";

        static TetherSetup()
        {
            EditorApplication.delayCall += () => Ensure(false);
        }

        [UnityEditor.MenuItem("Tether/Open Game Scene")]
        static void OpenFromMenu() { Ensure(true); }

        internal static void Ensure(bool forceOpen)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            bool created = false;
            if (!System.IO.File.Exists(ScenePath))
            {
                if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
                var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
                EditorSceneManager.SaveScene(scene, ScenePath);
                AssetDatabase.Refresh();
                created = true;
                Debug.Log("Tether: created " + ScenePath + ". Press Play to start the game.");
            }

            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            int idx = scenes.FindIndex(s => s.path == ScenePath);
            if (idx != 0)
            {
                if (idx > 0) scenes.RemoveAt(idx);
                scenes.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
                EditorBuildSettings.scenes = scenes.ToArray();
            }

            if (forceOpen && !created && EditorSceneManager.GetActiveScene().path != ScenePath)
            {
                if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                    EditorSceneManager.OpenScene(ScenePath);
            }

            const string MatPath = "Assets/Resources/TetherSprite.mat";
            if (!System.IO.File.Exists(MatPath))
            {
                var sh = Shader.Find("Sprites/Default");
                if (sh != null)
                {
                    if (!AssetDatabase.IsValidFolder("Assets/Resources")) AssetDatabase.CreateFolder("Assets", "Resources");
                    AssetDatabase.CreateAsset(new Material(sh), MatPath);
                    AssetDatabase.SaveAssets();
                }
            }

            if (string.IsNullOrEmpty(PlayerSettings.productName) || PlayerSettings.productName == "New Unity Project")
                PlayerSettings.productName = "Tether";
        }
    }
}
