using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace KnockbackRumble.EditorTools
{
    /// <summary>
    /// Menu: Knockback Rumble > Build Scrap Sprint Scene
    /// Creates a fresh scene with the greybox course and the game objects, saves it to
    /// Assets/KnockbackRumble/Scenes/ScrapSprint.unity and makes it the first build scene.
    /// Safe to run again after changing ScrapSprintBuilder.cs; it rebuilds from scratch.
    /// </summary>
    public static class KnockbackRumbleMenu
    {
        const string SceneFolder = "Assets/KnockbackRumble/Scenes";
        const string ScenePath = SceneFolder + "/ScrapSprint.unity";

        [MenuItem("Knockback Rumble/Build Scrap Sprint Scene", false, 0)]
        public static void BuildScrapSprint()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            ScrapSprintBuilder.Build(null);

            var game = new GameObject("Game");
            game.AddComponent<RaceManager>();
            game.AddComponent<HUD>();
            game.AddComponent<PrototypeBootstrap>();

            // Frame the course nicely in the Scene view / before Play
            var cam = Camera.main;
            if (cam != null)
            {
                cam.transform.position = new Vector3(0f, 6f, -14f);
                cam.transform.rotation = Quaternion.Euler(15f, 0f, 0f);
            }

            if (!AssetDatabase.IsValidFolder(SceneFolder))
                AssetDatabase.CreateFolder("Assets/KnockbackRumble", "Scenes");
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            AddToBuildSettings(ScenePath);

            Debug.Log("[Knockback Rumble] Built " + ScenePath + ". Press Play.");
        }

        [MenuItem("Knockback Rumble/Open Scrap Sprint Scene", false, 1)]
        public static void OpenScrapSprint()
        {
            if (!System.IO.File.Exists(ScenePath))
            {
                BuildScrapSprint();
                return;
            }
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                EditorSceneManager.OpenScene(ScenePath);
        }

        static void AddToBuildSettings(string path)
        {
            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            scenes.RemoveAll(s => s.path == path);
            scenes.Insert(0, new EditorBuildSettingsScene(path, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
