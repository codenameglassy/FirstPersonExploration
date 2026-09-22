using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class HierarchySeparator : MonoBehaviour
{
    [MenuItem("GameObject/Create Separator", false, 0)]
    private static void CreateSeparator()
    {
        GameObject separator = new GameObject("―――――― Separator ――――――");
        separator.tag = "EditorOnly"; // optional, so it won’t appear in builds
        separator.hideFlags = HideFlags.None;

        // Keep it at root
        if (Selection.activeTransform != null)
        {
            separator.transform.SetParent(Selection.activeTransform.parent);
            separator.transform.SetSiblingIndex(Selection.activeTransform.GetSiblingIndex() + 1);
        }
        Undo.RegisterCreatedObjectUndo(separator, "Create Separator");
        Selection.activeGameObject = separator;
    }
}
