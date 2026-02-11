using UnityEditor;
using UnityEngine;

using Rope;

namespace Editor.Scripts
{
    [CustomEditor(typeof(TwoSegmentWinchPulley))]
    public class TwoSegmentWinchPulleyEditor : UnityEditor.Editor
    {
        TwoSegmentWinchPulley container;

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            container = (TwoSegmentWinchPulley)target;

            if (GUILayout.Button("ApplySettings"))
            {
                container.ApplySettings();
            }
        }
    }
}