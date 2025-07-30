using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace RollABall.Editor
{
    public static class MissingScriptDetector
    {
        [MenuItem("Tools/Find All Missing Scripts")]
        public static void FindMissingScripts()
        {
            Debug.Log("=== MISSING SCRIPT DETECTION STARTED ===");

            List<GameObject> missingScriptObjects = new List<GameObject>();
            int totalObjects = 0;
            int missingScripts = 0;

            GameObject[] allObjects = Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);

            foreach (GameObject go in allObjects)
            {
                totalObjects++;
                Component[] components = go.GetComponents<Component>();

                foreach (Component comp in components)
                {
                    if (comp == null)
                    {
                        missingScripts++;
                        if (!missingScriptObjects.Contains(go))
                        {
                            missingScriptObjects.Add(go);
                        }
                        Debug.LogError($"Missing Script found on GameObject: '{go.name}' (Path: {GetGameObjectPath(go)})", go);
                    }
                }
            }

            Debug.Log($"=== SCAN COMPLETE ===");
            Debug.Log($"Total GameObjects scanned: {totalObjects}");
            Debug.Log($"Missing scripts found: {missingScripts}");
            Debug.Log($"GameObjects with missing scripts: {missingScriptObjects.Count}");

            if (missingScriptObjects.Count > 0)
            {
                Debug.LogWarning("=== OBJECTS WITH MISSING SCRIPTS ===");
                foreach (GameObject go in missingScriptObjects)
                {
                    Debug.LogWarning($"- {go.name} (Path: {GetGameObjectPath(go)})", go);
                }
            }
            else
            {
                Debug.Log("✅ No missing scripts found!");
            }
        }

        [MenuItem("Tools/Remove All Missing Scripts")]
        public static void RemoveMissingScripts()
        {
            Debug.Log("=== REMOVING MISSING SCRIPTS ===");

            int removedScripts = 0;
            GameObject[] allObjects = Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);

            foreach (GameObject go in allObjects)
            {
                SerializedObject serializedObject = new SerializedObject(go);
                SerializedProperty prop = serializedObject.FindProperty("m_Component");

                int r = 0;
                for (int j = 0; j < prop.arraySize; j++)
                {
                    var componentProp = prop.GetArrayElementAtIndex(j);
                    var component = componentProp.FindPropertyRelative("component");

                    if (component.objectReferenceValue == null)
                    {
                        prop.DeleteArrayElementAtIndex(j);
                        removedScripts++;
                        r++;
                        j--;
                        Debug.Log($"Removed missing script from: {go.name}");
                    }
                }

                if (r > 0)
                {
                    serializedObject.ApplyModifiedProperties();
                    EditorUtility.SetDirty(go);
                }
            }

            Debug.Log($"✅ Removed {removedScripts} missing script references");

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
        }

        private static string GetGameObjectPath(GameObject obj)
        {
            string path = obj.name;
            while (obj.transform.parent != null)
            {
                obj = obj.transform.parent.gameObject;
                path = obj.name + "/" + path;
            }
            return path;
        }
    }
}
