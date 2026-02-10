using UnityEditor;
using UnityEngine;

using Rope;

namespace Editor.Scripts
{
    [CustomEditor(typeof(TwoSegmentWinchController))]
    public class TwoSegmentWinchEditor : UnityEditor.Editor
    {
        TwoSegmentWinchController container;

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            container = (TwoSegmentWinchController)target;

            if (GUILayout.Button("ApplySettings"))
            {
                container.ApplySettings();
            }
        }
    }
}