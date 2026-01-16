#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace MidiPlayerTK
{
    public class PopupListPatchs : PopupWindowContent
    {
        private readonly GUIStyle BtSelected;
        private readonly GUIStyle BtStandard;
        private readonly GUIStyle CellStyle;
        private readonly GUIContent Content;
        private readonly List<string> Data;

        private Vector2 scroller;
        private readonly bool Selectable;
        public int Selected;
        private readonly GUIStyle TitleStyle;
        private readonly int winHeight = 175;

        private readonly int winWidth = 300;

        public PopupListPatchs(string title, bool pselectable, List<string> data)
        {
            Content = new GUIContent(title);
            Selectable = pselectable;
            Data = data;

            CellStyle = new GUIStyle("label");
            CellStyle.alignment = TextAnchor.MiddleLeft;
            CellStyle.normal.textColor = Color.black;
            CellStyle.wordWrap = false;
            CellStyle.fontSize = 12;
            CellStyle.border = new RectOffset(0, 0, 0, 0);
            CellStyle.padding = new RectOffset(0, 0, 0, 0);

            TitleStyle = new GUIStyle("textField");
            TitleStyle.alignment = TextAnchor.MiddleCenter;
            TitleStyle.normal.background = Resources.Load<Texture2D>("Textures/greendark");
            TitleStyle.normal.textColor = Color.black;
            TitleStyle.wordWrap = true;
            TitleStyle.fontSize = 14;

            BtStandard = new GUIStyle("Button");
            BtSelected = new GUIStyle("Button");
            BtSelected.fontStyle = FontStyle.Bold;
            BtSelected.normal.textColor = new Color(0.5f, 0.9f, 0.5f);
            BtSelected.hover.textColor = BtSelected.normal.textColor;
            BtSelected.active.textColor = BtSelected.normal.textColor;

            //winHeight =(int)( Data.Count * CellStyle.CalcHeight(Content,300f)+ TitleStyle.CalcHeight(Content, 300f));
            winHeight = (int)((Data.Count + 2) * CellStyle.lineHeight + TitleStyle.lineHeight);
        }

        public override Vector2 GetWindowSize()
        {
            return new Vector2(winWidth, winHeight);
        }

        public override void OnGUI(Rect rect)
        {
            try
            {
                // When loading a skin in a popup, Unity generate ugly warning log.
                //MidiCommonEditor.LoadSkinAndStyle(false);

                GUILayout.BeginHorizontal();
                GUILayout.Label(Content, TitleStyle);
                if (GUILayout.Button("Close", GUILayout.Width(50), GUILayout.Height(20)))
                    editorWindow.Close();
                GUILayout.EndHorizontal();

                scroller = GUILayout.BeginScrollView(scroller, false, false);
                for (var index = 0; index < Data.Count; index++)
                    if (Selectable)
                    {
                        var style = BtStandard;
                        if (Selected == index) style = BtSelected;
                        if (GUILayout.Button(Data[index], style))
                        {
                            Selected = index;
                            editorWindow.Close();
                        }
                    }
                    else
                    {
                        GUILayout.Label(Data[index], CellStyle);
                    }

                GUILayout.EndScrollView();
            }
            catch (Exception ex)
            {
                MidiPlayerGlobal.ErrorDetail(ex);
            }
        }
    }
}
#endif