using Game.PathSystem;
using UnityEditor;
using UnityEngine;

namespace Game.EditorTools
{
    /// <summary>
    /// Inspector add-ons for <see cref="WaypointPath"/>:
    /// - "Rebuild Cache" button so designers can refresh after moving control points
    ///   without entering Play Mode.
    /// - "Add Child Waypoint" creates a properly-named empty child at the path's centre.
    ///
    /// Keeping all editor-only code in this assembly means runtime builds carry no
    /// UnityEditor references and the IL2CPP build stays clean.
    /// </summary>
    [CustomEditor(typeof(WaypointPath))]
    public sealed class WaypointPathEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            var path = (WaypointPath)target;

            GUILayout.Space(8);
            if (GUILayout.Button("Rebuild Cache"))
            {
                path.RebuildCache();
                EditorUtility.SetDirty(path);
            }
            if (GUILayout.Button("Add Child Waypoint"))
            {
                var go = new GameObject($"WP_{path.transform.childCount}");
                Undo.RegisterCreatedObjectUndo(go, "Add Waypoint");
                go.transform.SetParent(path.transform, false);
                Selection.activeGameObject = go;
            }
        }
    }
}
