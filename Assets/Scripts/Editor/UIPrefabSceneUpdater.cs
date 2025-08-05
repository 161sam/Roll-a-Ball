#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;

namespace RollABall.Editor
{
    /// <summary>
    /// Provides a menu action to replace scene UI objects with the GameUI prefab.
    /// </summary>
    public static class UIPrefabSceneUpdater
    {
        private const string uiPrefabPath = "Assets/Prefabs/UI/GameUI.prefab";

        /// <summary>
        /// Replaces existing UI objects in all scenes under <c>Assets/Scenes</c> with the GameUI prefab.
        /// </summary>
        [MenuItem("Tools/Update UI Prefab In Scenes")]
        public static void UpdateUIPrefabInAllScenes()
        {
            GameObject uiPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(uiPrefabPath);
            if (!uiPrefab)
            {
                Debug.LogError($"UI prefab not found at {uiPrefabPath}");
                return;
            }

            string[] sceneGuids = AssetDatabase.FindAssets("t:Scene", new[] {"Assets/Scenes"});
            foreach (string guid in sceneGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.Contains("_backup")) continue;

                var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);

                // UI PREFAB INTEGRATION: remove existing UI objects
                foreach (Canvas canvas in Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
                {
                    Object.DestroyImmediate(canvas.gameObject);
                }
                foreach (EventSystem evt in Object.FindObjectsByType<EventSystem>(FindObjectsSortMode.None))
                {
                    Object.DestroyImmediate(evt.gameObject);
                }

                PrefabUtility.InstantiatePrefab(uiPrefab);

                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }

            Debug.Log("Updated UI prefabs in all scenes");
        }
    }
}
#endif
