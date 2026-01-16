#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace MidiPlayerTK
{
    public class TextArea
    {
        private static GUIStyle Style;
        private readonly int MaxHeight;
        private Vector2 scrollPosition = Vector2.zero;
        private readonly string Title;

        public TextArea(string title, int maxHeight = 100)
        {
            Title = title;
            MaxHeight = maxHeight;
            Style = new GUIStyle(EditorStyles.textArea);
            Style.normal.textColor = new Color(0, 0, 0.99f);
            Style.alignment = TextAnchor.UpperLeft;
        }

        public void Display(string text)
        {
            EditorGUILayout.LabelField(Title);
            var width = EditorGUIUtility.currentViewWidth - 20f;
            var height = Style.CalcHeight(new GUIContent(text), width) + 5;
            if (height > MaxHeight) height = MaxHeight;
            scrollPosition =
                GUILayout.BeginScrollView(scrollPosition, GUILayout.Width(width), GUILayout.Height(height));
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(15);
            GUILayout.TextField(text, Style);
            EditorGUILayout.EndHorizontal();
            GUILayout.EndScrollView();
        }
    }
}
#endif