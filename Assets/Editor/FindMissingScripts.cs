// Assets/Editor/FindMissingScripts.cs
using UnityEngine;
using UnityEditor;

public class FindMissingScripts
{
    [MenuItem("Tools/Find Missing Scripts In Project")]
    private static void FindInProject()
    {
        string[] guids = AssetDatabase.FindAssets("t:Prefab");
        int total = 0;
        foreach (string g in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(g);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            int found = FindInGO(prefab, path);
            if (found > 0) total += found;
        }
        Debug.Log($"Procurou {guids.Length} prefabs. Encontrados {total} missing scripts no total.");
    }

    [MenuItem("Tools/Find Missing Scripts In Selected")]
    private static void FindInSelected()
    {
        int total = 0;
        foreach (GameObject go in Selection.gameObjects)
        {
            total += FindInGO(go, go.name);
        }
        Debug.Log($"Verificados {Selection.gameObjects.Length} objetos selecionados. Encontrados {total} missing scripts.");
    }

    private static int FindInGO(GameObject go, string contextPath)
    {
        int count = 0;
        Component[] components = go.GetComponents<Component>();
        for (int i = 0; i < components.Length; i++)
        {
            if (components[i] == null)
            {
                Debug.LogWarning($"Missing script: '{FullPath(go)}'  (na asset/prefab: {contextPath})", go);
                count++;
            }
        }
        foreach (Transform t in go.transform)
            count += FindInGO(t.gameObject, contextPath);
        return count;
    }

    private static string FullPath(GameObject go)
    {
        string s = go.name;
        Transform t = go.transform.parent;
        while (t != null)
        {
            s = t.name + "/" + s;
            t = t.parent;
        }
        return s;
    }

    [MenuItem("Tools/Remove Missing Scripts From Selected")]
    private static void RemoveMissingFromSelected()
    {
        foreach (GameObject go in Selection.gameObjects)
        {
            Undo.RegisterFullObjectHierarchyUndo(go, "Remove Missing Scripts");
            int removed = UnityEditor.GameObjectUtility.RemoveMonoBehavioursWithMissingScript(go);
            if (removed > 0) Debug.Log($"Removidos {removed} componentes missing de {FullPath(go)}", go);
        }
    }
}
