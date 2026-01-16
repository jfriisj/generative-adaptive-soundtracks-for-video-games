#if UNITY_EDITOR

using System;
using UnityEditor;
using UnityEngine;

namespace MidiPlayerTK
{
    public class AboutMPTK : PopupWindowContent
    {
        private static Vector2 scrollPosAnalyze = Vector2.zero;

        private readonly GUIStyle styleLabelUpperLeft;
        private readonly GUIStyle stylePanel;

        /*static*/
        private readonly int winHeight = 450;

        /*static*/
        private readonly int winWidth = 838;

        public AboutMPTK()
        {
            styleLabelUpperLeft = new GUIStyle(EditorStyles.label);
            styleLabelUpperLeft.alignment = TextAnchor.UpperLeft;
            styleLabelUpperLeft.normal.textColor = Color.black;
            styleLabelUpperLeft.hover.textColor = Color.black;

            var gray3 = 0.7f;
            var gray2 = 0.1f;
            var borderSize = 1; // Border size in pixels
            var rectBorder = new RectOffset(borderSize, borderSize, borderSize, borderSize);

            stylePanel = new GUIStyle("box");
            stylePanel.normal.background = MPTKGui.MakeTex(10, 10, new Color(gray3, gray3, gray3, 1f), rectBorder,
                new Color(gray2, gray2, gray2, 1f));
            stylePanel.alignment = TextAnchor.MiddleCenter;
        }

        public override Vector2 GetWindowSize()
        {
            return new Vector2(winWidth, winHeight);
        }

        [MenuItem(Constant.MENU_MAESTRO + "/Version and Doc &V", false, 100)]
        private static void Display()
        {
            try
            {
                var rect = new Rect { x = 100, y = 120 /*, width = winWidth, height = winHeight */ };
                PopupWindow.Show(rect, new AboutMPTK());
            }
            catch (Exception)
            {
            }
        }

        public override void OnGUI(Rect rect)
        {
            // When loading a skin in a popup, Unity generate ugly warning log.
            // MidiCommonEditor.LoadSkinAndStyle(false);
            try
            {
                float xCol0 = 5;
                float xCol1 = 20;
                float xCol2 = 120;
                float yStart = 0;
                float ySpace = 18;
                float colWidth = 230;
                float colHeight = 17;
                float btWidth = 130;
                float btHeight = 22;
                var btx = xCol0;
                float spaceH = 8;
                float spaceV = 8;
                var textVersion = "";

                MPTKGui.LoadSkinAndStyle();

                var style = new GUIStyle("Label");
                style.fontSize = 16;
                style.normal.textColor = Color.black;
                style.fontStyle = FontStyle.Bold;

                try
                {
                    var sizePicture = 90;
                    var aTexture = Resources.Load<Texture>("Logo_MPTK");
                    //TextAsset textAsset = Resources.Load<TextAsset>("Version changes");
                    //textVersion = System.Text.Encoding.UTF8.GetString(textAsset.bytes);
                    textVersion = ToolsEditor.ReadTextFile(Application.dataPath + "/MidiPlayer/Version changes.txt");
                    EditorGUI.DrawPreviewTexture(new Rect(winWidth - sizePicture - 5, yStart, sizePicture, sizePicture),
                        aTexture);
                }
                catch (Exception ex)
                {
                    MidiPlayerGlobal.ErrorDetail(ex);
                }

                var cont = new GUIContent("Maestro - Midi Player Tool Kit");
                EditorGUI.LabelField(new Rect(xCol0, yStart - 5, 300, 30), cont, style);
                EditorGUI.LabelField(new Rect(xCol0, yStart + 8, 800, colHeight),
                    "_________________________________________________________________________________________________________________________________________________");

                yStart += 15;
                EditorGUI.LabelField(new Rect(xCol1, yStart += ySpace, colWidth, colHeight), "Version:");
                EditorGUI.LabelField(new Rect(xCol2, yStart, colWidth, colHeight), Constant.version);

                EditorGUI.LabelField(new Rect(xCol1, yStart += ySpace, colWidth, colHeight), "Release:");
                EditorGUI.LabelField(new Rect(xCol2, yStart, colWidth, colHeight), Constant.releaseDate);

                yStart += 15;
                EditorGUI.LabelField(new Rect(xCol0, yStart += ySpace, colWidth * 2, colHeight),
                    "Design and Development by Thierry Bachmann");
                EditorGUI.LabelField(new Rect(xCol1, yStart += ySpace, colWidth, colHeight), "Email:");
                EditorGUI.TextField(new Rect(xCol2, yStart, colWidth, colHeight), "thierry.bachmann@gmail.com");
                EditorGUI.LabelField(new Rect(xCol1, yStart += ySpace, colWidth, colHeight), "Website:");
                EditorGUI.TextField(new Rect(xCol2, yStart, colWidth, colHeight), Constant.paxSite);

                yStart += 30;

                if (GUI.Button(new Rect(btx, yStart, btWidth, btHeight), "Unity Forum"))
                    Application.OpenURL(Constant.forumSite);

                btx += btWidth + spaceH;
                if (GUI.Button(new Rect(btx, yStart, btWidth, btHeight), "Discord"))
                    Application.OpenURL(Constant.DiscordSite);

                btx += btWidth + spaceH;
                if (GUI.Button(new Rect(btx, yStart, btWidth, btHeight), "Custom GPT"))
                    Application.OpenURL(Constant.CustomGptSite);

                btx += btWidth + spaceH;
                if (GUI.Button(new Rect(btx, yStart, btWidth, btHeight), "Documentation"))
                    Application.OpenURL(Constant.blogSite);

                btx += btWidth + spaceH;
                if (GUI.Button(new Rect(btx, yStart, btWidth, btHeight), "API Helper"))
                    Application.OpenURL(Constant.apiSite);

                btx += btWidth + spaceH;
                if (GUI.Button(new Rect(btx, yStart, btWidth, btHeight), "PaxStellar"))
                    Application.OpenURL(Constant.paxSite);

                btx += btWidth + spaceH;
                if (GUI.Button(new Rect(btx, yStart, btWidth, btHeight), "Get Full Version"))
                    Application.OpenURL(Constant.UnitySite);

                yStart += btHeight + spaceV;
                var heightList = winHeight - yStart - 10;

                float wList = winWidth - 10;
                var listVisibleRect = new Rect(xCol0, yStart, wList, heightList);
                var listContentRect = new Rect(0, 0, 2 * wList, 121 * styleLabelUpperLeft.lineHeight + spaceV);
                var fondRect = new Rect(xCol0, yStart, wList, heightList);
                GUI.Box(fondRect, "", stylePanel);

                scrollPosAnalyze = GUI.BeginScrollView(listVisibleRect, scrollPosAnalyze, listContentRect);
                if (!string.IsNullOrEmpty(textVersion))
                    GUI.Box(listContentRect, textVersion, styleLabelUpperLeft);
                GUI.EndScrollView();
            }
            catch (Exception ex)
            {
                MidiPlayerGlobal.ErrorDetail(ex);
            }
        }
    }
}
#endif