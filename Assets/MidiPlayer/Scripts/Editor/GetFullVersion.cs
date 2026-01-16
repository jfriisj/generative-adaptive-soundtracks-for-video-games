#if UNITY_EDITOR

using System;
using UnityEditor;
using UnityEngine;

namespace MidiPlayerTK
{
    public class GetFullVersion : PopupWindowContent
    {
        private readonly int winHeight = 260;

        private readonly int winWidth = 365;

        public override Vector2 GetWindowSize()
        {
            return new Vector2(winWidth, winHeight);
        }

        public override void OnGUI(Rect rect)
        {
            try
            {
                float xCol0 = 5;
                float xCol1 = 20;
                float xCol2 = 120;
                float yStart = 5;
                float ySpace = 18;
                float colWidth = 230;
                float colHeight = 17;

                MPTKGui.LoadSkinAndStyle(false);

                var style16Bold = new GUIStyle("Label");
                style16Bold.fontSize = 16;
                style16Bold.fontStyle = FontStyle.Bold;

                var styleBold = new GUIStyle("Label");
                styleBold.fontStyle = FontStyle.Bold;

                try
                {
                    var sizePicture = 90;
                    var aTexture = Resources.Load<Texture>("Logo_MPTK");
                    EditorGUI.DrawPreviewTexture(new Rect(winWidth - sizePicture - 5, yStart, sizePicture, sizePicture),
                        aTexture);
                }
                catch (Exception ex)
                {
                    MidiPlayerGlobal.ErrorDetail(ex);
                }

                var cont = new GUIContent("Midi Player ToolKit (MPTK)");
                EditorGUI.LabelField(new Rect(xCol0, yStart, 300, 30), cont, style16Bold);
                EditorGUI.LabelField(new Rect(xCol0, yStart + 8, 300, colHeight), "_________________________________");

                yStart += 20;
                EditorGUI.LabelField(new Rect(xCol0, yStart += ySpace, colWidth, colHeight),
                    "This functionality is not available", styleBold);
                EditorGUI.LabelField(new Rect(xCol0, yStart += ySpace, colWidth, colHeight),
                    "        with the free version", styleBold);
                yStart += 25;
                EditorGUI.LabelField(new Rect(xCol0, yStart += ySpace, colWidth + 20, colHeight),
                    "The pro version includes these capacities :");
                // yStart += 15;

                EditorGUI.LabelField(new Rect(xCol1, yStart += ySpace, colWidth, colHeight),
                    "- Import and optimize SoundFont,");
                EditorGUI.LabelField(new Rect(xCol1, yStart += ySpace, colWidth, colHeight), "- new prefab and class,");
                EditorGUI.LabelField(new Rect(xCol1, yStart += ySpace, colWidth, colHeight), "- MIDI editor,");
                EditorGUI.LabelField(new Rect(xCol1, yStart += ySpace, colWidth, colHeight),
                    "- and a lot of cool functions!");
                yStart += 30;
                EditorGUI.LabelField(new Rect(xCol1, yStart, colWidth, colHeight), "Website:");
                EditorGUI.TextField(new Rect(xCol2, yStart, colWidth, colHeight), Constant.paxSite);
                yStart += 30;

                colWidth = 110;
                var space = 8;
                if (GUI.Button(new Rect(xCol0, yStart, colWidth, colHeight), "Open Web Site"))
                    Application.OpenURL(Constant.paxSite);
                if (GUI.Button(new Rect(xCol0 + colWidth + space, yStart, colWidth, colHeight), "Help"))
                    Application.OpenURL(Constant.blogSite);

                if (GUI.Button(new Rect(xCol0 + colWidth + space + colWidth + space, yStart, colWidth, colHeight),
                        "Get Full Version")) Application.OpenURL(Constant.UnitySite);
                //EditorUtility.DisplayDialog("Not yet Available", "Pro version not yet available. Soon ....", "Ok");
            }
            catch (Exception ex)
            {
                MidiPlayerGlobal.ErrorDetail(ex);
            }
        }
    }
}
#endif