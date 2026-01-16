#if UNITY_EDITOR
//#define MPTK_PRO
using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace MidiPlayerTK
{
    public class MPTKGui
    {
        // https://github.com/halak/unity-editor-icons
        // https://github.com/nukadelic/UnityEditorIcons
        private static Texture iconComboBox;
        private static Texture iconFirst;
        private static Texture iconPrevious;
        private static Texture iconNext;
        private static Texture iconLast;
        private static Texture iconHelp;
        private static Texture iconHelpBlack;
        private static Texture iconEye;
        private static Texture iconSave;
        private static Texture iconFolders;
        private static Texture iconDeleteGray;
        private static Texture iconDeleteRed;
        private static Texture iconClose;
        private static Texture iconRefresh;
        private static Texture tabNext;
        private static Texture tabPrev;
        private static Texture iconClear;


        //static public Texture IconGoToEnd = LoadIcon(IconGoToEnd, "");

        public static Color ButtonColor = new(.7f, .9f, .7f, 1f);

        private static GUIStyle labelLeft;

        private static GUIStyle labelRight;

        private static GUIStyle labelCenter;

        private static GUIStyle labelCenterSmall;

        private static GUIStyle buttonSmall;

        public static CustomStyle myStyle;

        public static GUIStyle styleWindow;
        public static GUIStyle stylePanelGrayMiddle;
        public static GUIStyle stylePanelGrayBlack;
        public static GUIStyle stylePanelGrayLight;
        public static GUIStyle styleBold;
        public static GUIStyle styleAlertRed;
        public static GUIStyle styleRichText;
        public static GUIStyle styleLabelLeft;
        public static GUIStyle styleMiniPullDown;
        public static GUIStyle styleListTitle;
        public static GUIStyle styleListRow;
        public static GUIStyle styleListRowLeft;
        public static GUIStyle styleListRowCenter;
        public static GUIStyle styleListRowSelected;

        public static GUIStyle styleLabelFontCourier;
        //static public float lineHeight = 0f;

        public static GUISkin MaestroSkin;
        private static Stopwatch watchPerf = new();
        private static GUIStyle labelBoldCentered;

        public static Texture IconComboBox
        {
            get
            {
                if (iconComboBox == null) iconComboBox = EditorGUIUtility.IconContent("d_icon dropdown").image;
                return iconComboBox;
            }
        }

        public static Texture IconFirst
        {
            get
            {
                if (iconFirst == null) iconFirst = EditorGUIUtility.IconContent("d_Animation.FirstKey").image;
                return iconFirst;
            }
        }

        public static Texture IconPrevious
        {
            get
            {
                if (iconPrevious == null) iconPrevious = EditorGUIUtility.IconContent("d_Animation.PrevKey").image;
                return iconPrevious;
            }
        }

        public static Texture IconNext
        {
            get
            {
                if (iconNext == null) iconNext = EditorGUIUtility.IconContent("d_Animation.NextKey").image;
                return iconNext;
            }
        }

        public static Texture IconLast
        {
            get
            {
                if (iconLast == null) iconLast = EditorGUIUtility.IconContent("d_Animation.LastKey").image;
                return iconLast;
            }
        }

        public static Texture IconHelp
        {
            get
            {
                if (iconHelp == null) iconHelp = Resources.Load<Texture2D>("Textures/question");
                return iconHelp;
            }
        }

        public static Texture IconHelpBlack
        {
            get
            {
                if (iconHelpBlack == null) iconHelpBlack = Resources.Load<Texture2D>("Textures/questionBlack");
                return iconHelpBlack;
            }
        }

        public static Texture IconEye
        {
            get
            {
                if (iconEye == null) iconEye = EditorGUIUtility.IconContent("d_ViewToolOrbit").image;
                return iconEye;
            }
        }

        public static Texture IconSave
        {
            get
            {
                if (iconSave == null) iconSave = EditorGUIUtility.IconContent("SaveActive").image;
                return iconSave;
            }
        }

        public static Texture IconFolders
        {
            get
            {
                if (iconFolders == null) iconFolders = EditorGUIUtility.IconContent("Folder On Icon").image;
                return iconFolders;
            }
        }

        public static Texture IconDeleteGray
        {
            get
            {
                if (iconDeleteGray == null) iconDeleteGray = Resources.Load<Texture2D>("Textures/Delete_32x32_gray");
                return iconDeleteGray;
            }
        }

        public static Texture IconDeleteRed
        {
            get
            {
                if (iconDeleteRed == null) iconDeleteRed = Resources.Load<Texture2D>("Textures/Delete_32x32");
                return iconDeleteRed;
            }
        }

        public static Texture IconClose
        {
            get
            {
                if (iconClose == null) iconClose = EditorGUIUtility.IconContent("winbtn_win_close").image;
                return iconClose;
            }
        }

        public static Texture IconRefresh
        {
            get
            {
                if (iconRefresh == null) iconRefresh = EditorGUIUtility.IconContent("d_TreeEditor.Refresh").image;
                return iconRefresh;
            }
        }

        public static Texture IconTabNext
        {
            get
            {
                if (tabNext == null) tabNext = EditorGUIUtility.IconContent("d_forward").image;
                return tabNext;
            }
        }

        public static Texture IconTabPrev
        {
            get
            {
                if (tabPrev == null) tabPrev = EditorGUIUtility.IconContent("d_back").image;
                return tabPrev;
            }
        }

        public static Texture IconClear
        {
            get
            {
                if (iconClear == null) iconClear = EditorGUIUtility.IconContent("clear").image;
                return iconClear;
            }
        }

        public static GUIStyle Label => MaestroSkin.GetStyle("label");

        public static GUIStyle LabelLeft
        {
            get
            {
                if (labelLeft == null)
                    labelLeft = BuildStyle(MaestroSkin.GetStyle("label"), 12, textAnchor: TextAnchor.MiddleLeft);
                return labelLeft;
            }
        }

        public static GUIStyle LabelRight
        {
            get
            {
                if (labelRight == null)
                    labelRight = BuildStyle(MaestroSkin.GetStyle("label"), 12, textAnchor: TextAnchor.MiddleRight);
                return labelRight;
            }
        }

        public static GUIStyle LabelCenter
        {
            get
            {
                if (labelCenter == null)
                    labelCenter = BuildStyle(MaestroSkin.GetStyle("label"), 12, textAnchor: TextAnchor.MiddleCenter);
                return labelCenter;
            }
        }

        public static GUIStyle LabelCenterSmall
        {
            get
            {
                if (labelCenterSmall == null)
                    labelCenterSmall =
                        BuildStyle(MaestroSkin.GetStyle("label"), 9, textAnchor: TextAnchor.MiddleCenter);
                return labelCenterSmall;
            }
        }

        public static GUIStyle LabelListPlayed => MaestroSkin.GetStyle("LabelListPlayed");

        public static GUIStyle LabelListSelected => MaestroSkin.GetStyle("LabelListSelected");

        //public static GUIStyle LabelListNormal { get { return MidiCommonEditor.MaestroSkin.GetStyle("LabelListNormal"); } }
        public static GUIStyle LabelListNormal => MaestroSkin.GetStyle("LabelListNormal");
        public static GUIStyle ButtonCombo => MaestroSkin.GetStyle("ButtonCombo");
        public static GUIStyle ButtonHighLight => MaestroSkin.GetStyle("ButtonHighLight");
        public static GUIStyle Button => MaestroSkin.GetStyle("button");

        public static GUIStyle ButtonSmall
        {
            get
            {
                if (buttonSmall == null)
                    buttonSmall = BuildStyle(MaestroSkin.GetStyle("label"), 12, textAnchor: TextAnchor.MiddleRight);
                //buttonSmall.margin= new RectOffset(0,0,0, 0);
                //buttonSmall.contentOffset = Vector2.zero;
                //buttonSmall.overflow = new RectOffset(0, 0, 0, 0);
                //buttonSmall.stretchHeight = false;
                //buttonSmall.stretchWidth = false;
                //buttonSmall.border = new RectOffset(0, 0, 0, 0);
                return buttonSmall;
            }
        }

        public static GUIStyle TextArea
        {
            get
            {
                var style = MaestroSkin.GetStyle("textarea");
                return style;
            }
        }

        public static GUIStyle TextField
        {
            get
            {
                var style = MaestroSkin.GetStyle("textfield");
                return style;
            }
        }

        public static GUIStyle HorizontalSlider => MaestroSkin.GetStyle("horizontalslider");
        public static GUIStyle HorizontalThumb => MaestroSkin.GetStyle("horizontalsliderthumb");
        public static GUIStyle VerticalSlider => MaestroSkin.GetStyle("verticalslider");
        public static GUIStyle VerticalThumb => MaestroSkin.GetStyle("verticalsliderthumb");
        public static GUIStyle LabelGray => MaestroSkin.GetStyle("LabelGray");

        public static GUIStyle styleLabelCenter => MaestroSkin.GetStyle("Label");
        public static GUIStyle styleLabelRight => MaestroSkin.GetStyle("LabelRight");
        public static GUIStyle styleToggle => MaestroSkin.GetStyle("toggle");

        public static GUIStyle LabelBoldCentered
        {
            get
            {
                if (labelBoldCentered == null)
                {
                    labelBoldCentered = new GUIStyle(MaestroSkin.GetStyle("label"));
                    labelBoldCentered.wordWrap = true;
                    labelBoldCentered.fontStyle = FontStyle.Bold;
                    labelBoldCentered.alignment = TextAnchor.MiddleCenter;
                }

                return labelBoldCentered;
            }
        }

        public static Texture LoadIcon(string name, Texture icon = null)
        {
            if (icon == null)
            {
                icon = Resources.Load<Texture2D>("Textures/" + name);
                if (icon == null)
                    Debug.LogWarning($"LoadIcon texture not found {name}");
            }

            return icon;
        }

        public static void LoadSkinAndStyle(bool loadSkin = true)
        {
            if (MaestroSkin == null || MaestroSkin.name != "MaestroSkin")
                MaestroSkin = EditorGUIUtility.Load("Assets/MidiPlayer/MaestroSkin.GUISkin") as GUISkin;
            //Debug.Log($"Loaded skin {MaestroSkin.name} {((double)watchPerf.ElapsedTicks) / ((double)System.Diagnostics.Stopwatch.Frequency / 1000d):F2} ms ");
            //Debug.Log($"Custom skin loaded {MaestroSkin.name}");

            var borderSize = 1; // Border size in pixels
            var rectBorder = new RectOffset(borderSize, borderSize, borderSize, borderSize);

            styleMiniPullDown = new GUIStyle(EditorStyles.miniPullDown);

            styleBold = new GUIStyle(EditorStyles.boldLabel);
            styleBold.fontStyle = FontStyle.Bold;
            styleBold.alignment = TextAnchor.UpperLeft;
            styleBold.normal.textColor = Color.black;

            var grayBlack = 0.1f;
            var grayMiddle = 0.5f;
            var grayLight = 0.7f;
            //        float grayWhite = 0.8f;

            styleWindow = new GUIStyle("box");
            styleWindow.normal.background = MakeTex(10, 10, new Color(grayMiddle, grayMiddle, grayMiddle, 1f),
                rectBorder, new Color(grayBlack, grayBlack, grayBlack, 1f));
            styleWindow.alignment = TextAnchor.MiddleCenter;

            stylePanelGrayMiddle = new GUIStyle("box");
            stylePanelGrayMiddle.normal.background = MakeTex(10, 10, new Color(grayMiddle, grayMiddle, grayMiddle, 1f),
                rectBorder, new Color(grayBlack, grayBlack, grayBlack, 1f));
            stylePanelGrayMiddle.alignment = TextAnchor.MiddleCenter;

            stylePanelGrayBlack = new GUIStyle("box");
            stylePanelGrayBlack.normal.background = MakeTex(10, 10, new Color(grayBlack, grayBlack, grayBlack, 1f),
                rectBorder, new Color(grayBlack, grayBlack, grayBlack, 1f));
            stylePanelGrayBlack.alignment = TextAnchor.MiddleCenter;

            stylePanelGrayLight = new GUIStyle("box");
            stylePanelGrayLight.normal.background = MakeTex(10, 10, new Color(grayLight, grayLight, grayLight, 1f),
                rectBorder, new Color(grayBlack, grayBlack, grayBlack, 1f));
            stylePanelGrayLight.alignment = TextAnchor.MiddleCenter;

            styleListTitle = new GUIStyle("box");
            styleListTitle.normal.background = MakeTex(10, 10, new Color(grayMiddle, grayMiddle, grayMiddle, 1f),
                rectBorder, new Color(grayBlack, grayBlack, grayBlack, 1f));
            styleListTitle.normal.textColor = Color.black;
            styleListTitle.alignment = TextAnchor.MiddleCenter;

            styleListRow = new GUIStyle("box");
            styleListRow.normal.background = MakeTex(10, 10, new Color(grayLight, grayLight, grayLight, 1f), rectBorder,
                new Color(grayBlack, grayBlack, grayBlack, 1f));
            styleListRow.normal.textColor = Color.black;
            styleListRow.alignment = TextAnchor.MiddleCenter;

            styleListRowLeft = new GUIStyle("box");
            styleListRowLeft.normal.background = MakeTex(10, 10, new Color(grayLight, grayLight, grayLight, 1f),
                rectBorder, new Color(grayBlack, grayBlack, grayBlack, 1f));
            styleListRowLeft.normal.textColor = Color.black;
            styleListRowLeft.alignment = TextAnchor.MiddleLeft;

            styleListRowCenter = new GUIStyle("box");
            styleListRowCenter.normal.background = MakeTex(10, 10, new Color(grayLight, grayLight, grayLight, 1f),
                rectBorder, new Color(grayBlack, grayBlack, grayBlack, 1f));
            styleListRowCenter.normal.textColor = Color.black;
            styleListRowCenter.alignment = TextAnchor.MiddleCenter;

            styleListRowSelected = new GUIStyle("box");
            styleListRowSelected.normal.background = MakeTex(10, 10, new Color(.6f, .8f, .6f, 1f), rectBorder,
                new Color(grayBlack, grayBlack, grayBlack, 1f));
            styleListRowSelected.normal.background.name = "bckgname"; // kind hack to check if custom style are loaded
            styleListRowSelected.normal.textColor = Color.black;
            styleListRowSelected.alignment = TextAnchor.MiddleCenter;

            styleAlertRed = new GUIStyle(EditorStyles.label);
            styleAlertRed.normal.textColor = new Color(188f / 255f, 56f / 255f, 56f / 255f);
            styleAlertRed.fontSize = 12;

            styleRichText = new GUIStyle(EditorStyles.label);
            styleRichText.richText = true;
            styleRichText.alignment = TextAnchor.UpperLeft;
            styleRichText.normal.textColor = Color.black;

            styleLabelLeft = new GUIStyle(EditorStyles.label);
            styleLabelLeft.alignment = TextAnchor.MiddleLeft;
            styleLabelLeft.normal.textColor = Color.black;

            // Load and set Font
            var myFont = (Font)Resources.Load("Courier", typeof(Font));
            styleLabelFontCourier = new GUIStyle(EditorStyles.label);
            styleLabelFontCourier.font = myFont;
            styleLabelFontCourier.alignment = TextAnchor.UpperLeft;
            styleLabelFontCourier.normal.textColor = Color.black;
            styleLabelFontCourier.hover.textColor = Color.black;

            // Debug.Log($"End Custom {((double)watchPerf.ElapsedTicks) / ((double)System.Diagnostics.Stopwatch.Frequency / 1000d):F2} ms ");
        }

        public static GUIStyle BuildStyle(GUIStyle inheritedStyle = null, int fontSize = 10, bool wrapText = false,
            FontStyle fontStyle = FontStyle.Normal, TextAnchor textAnchor = TextAnchor.MiddleLeft)
        {
            var style = inheritedStyle == null ? new GUIStyle() : new GUIStyle(inheritedStyle);
            style.alignment = textAnchor;
            style.fontSize = fontSize;
            style.fontStyle = fontStyle;
            style.wordWrap = wrapText;
            style.clipping = TextClipping.Overflow;
            return style;
        }

        public static GUIStyle ColorStyle(GUIStyle style, Color fontColor, Texture2D backColor = null)
        {
            style.normal.textColor = fontColor;
            style.focused.textColor = fontColor;
            style.normal.background = backColor != null ? backColor : style.onNormal.background;
            style.focused.background = backColor != null ? backColor : style.onNormal.background;
            return style;
        }


        public static int IntField(string label = null, int val = 0, int min = 0, int max = 99999999,
            int maxLength = 10, int widthLabel = 60, int widthText = -1)
        {
            int newval;
            if (label != null)
                GUILayout.Label(label, LabelLeft, GUILayout.Width(widthLabel));
            if (val < min) val = min;
            if (val > max) val = max;

            var oldtxt = val.ToString();
            string newtxt;
            if (widthText <= 0)
                newtxt = GUILayout.TextField(oldtxt, maxLength, TextField);
            else
                newtxt = GUILayout.TextField(oldtxt, maxLength, TextField, GUILayout.Width(widthText));
            if (newtxt != oldtxt)
                try
                {
                    newval = newtxt.Length > 0 ? Convert.ToInt32(newtxt) : 0;
                    if (newval < min) newval = min;
                    if (newval > max) newval = max;
                    return newval;
                }
                catch
                {
                }

            return val;
        }

        public static long LongField(string label = null, long val = 0, long min = 0, long max = 99999999,
            int maxLength = 10, int widthLabel = 60, int widthText = -1)
        {
            long newval;
            if (label != null)
                GUILayout.Label(label, LabelLeft, GUILayout.Width(widthLabel));
            if (val < min) val = min;
            if (val > max) val = max;

            var oldtxt = val.ToString();
            string newtxt;
            if (widthText <= 0)
                newtxt = GUILayout.TextField(oldtxt, maxLength, TextField);
            else
                newtxt = GUILayout.TextField(oldtxt, maxLength, TextField, GUILayout.Width(widthText));

            if (newtxt != oldtxt)
                try
                {
                    newval = newtxt.Length > 0 ? Convert.ToInt64(newtxt) : 0;
                    if (newval < min) newval = min;
                    if (newval > max) newval = max;
                    return newval;
                }
                catch
                {
                }

            return val;
        }

        /// <summary>
        ///     Combobox with GUILayout
        /// </summary>
        /// <param name="p_popup"></param>
        /// <param name="title"></param>
        /// <param name="items"></param>
        /// <param name="selectedIndex"></param>
        /// <param name="action"></param>
        /// <param name="style"></param>
        /// <param name="widthPopup"></param>
        /// <param name="option"></param>
        public static void ComboBox(ref PopupList p_popup, string title, List<StyleItem> items, bool multiSelection,
            Action<int> action,
            GUIStyle style = null, float widthPopup = 0, params GUILayoutOption[] option)
        {
            ComboBox(Rect.zero, ref p_popup, title, items, multiSelection, action, style, widthPopup, option);
        }

        /// <summary>
        ///     Combobox with GUI and rect
        /// </summary>
        /// <param name="rect"></param>
        /// <param name="p_popup"></param>
        /// <param name="title"></param>
        /// <param name="items"></param>
        /// <param name="selectedIndex"></param>
        /// <param name="action"></param>
        /// <param name="style"></param>
        /// <param name="widthPopup"></param>
        /// <param name="option"></param>
        public static void ComboBox(Rect rect, ref PopupList p_popup, string title, List<StyleItem> items,
            bool multiSelection, Action<int> action,
            GUIStyle style = null, float widthPopup = 0, params GUILayoutOption[] option)
        {
            //Debug.Log(Event.current);
            if (p_popup == null)
            {
                //Debug.Log($"BuildPopup popupLoadType {items.Count}");
                p_popup = new PopupList("", items, multiSelection);
                p_popup.OnSelect = action;
            }

            if (!multiSelection)
            {
                //p_popup.SelectedIndex = selectedIndex;
                // Mono selection
                title = title.Replace("{Label}", p_popup.SelectedLabel)
                    .Replace("{Index}", p_popup.SelectedIndex.ToString());
            }
            else
            {
                // Multi selection
                if (title.Contains("{Count}"))
                {
                    var count = $"{p_popup.SelectedCount}/{p_popup.TotalCount}";
                    title = title.Replace("{Count}", count);
                }

                if (title.Contains("{*}"))
                    title = title.Replace("{*}", p_popup.SelectedCount != p_popup.TotalCount ? "*" : "");
            }

            if (style == null)
                // Style for the combo button
                style = ButtonCombo;
            //else
            //    Debug.Log($"ComboBox style.contentOffset {title} {style.contentOffset}");

            if (rect.width == 0f)
            {
                GUILayout.Label(new GUIContent(title, IconComboBox), style, option);
                if (Event.current.type == EventType.Repaint)
                {
                    p_popup.RectPopup = GUILayoutUtility.GetLastRect();
                    if (widthPopup != 0)
                        p_popup.RectPopup.width = widthPopup;
                    //Debug.Log($"GetLastRect {title} {p_popup.RectActivation}");
                    p_popup.RectPopup.x += style.contentOffset.x;
                }
            }
            else
            {
                //GUI.Label(rect, new GUIContent(title, MPTKGui.IconComboBox), style);
                GUI.Label(rect, title, style);
                p_popup.RectPopup = rect;
                if (widthPopup != 0)
                    p_popup.RectPopup.width = widthPopup;
                p_popup.RectPopup.x += style.contentOffset.x;
            }

            if (Event.current.type == EventType.MouseDown)
            {
                var lastRect = rect.width == 0f ? GUILayoutUtility.GetLastRect() : rect;
                //Debug.Log($"MouseDown style.contentOffset {title} {style.contentOffset}");
                if (lastRect.Contains(Event.current.mousePosition - style.contentOffset))
                    //Debug.Log($"Show PopupWindow {p_popup.RectActivation}");
                    try
                    {
                        PopupWindow.Show(p_popup.RectPopup, p_popup);
                    }
                    catch (ExitGUIException)
                    {
                    } // Unity bug ?
            }
        }

        public static Texture2D SetColor(Texture2D tex2, Color32 color)
        {
            var fillColorArray = tex2.GetPixels32();
            for (var i = 0; i < fillColorArray.Length; ++i)
                fillColorArray[i] = color;
            tex2.SetPixels32(fillColorArray);
            tex2.Apply();
            return tex2;
        }

        public static Texture2D MakeTex(float grey, RectOffset border)
        {
            var color = new Color(grey, grey, grey, 1f);
            return MakeTex(10, 10, color, border, color);
        }

        public static Texture2D MakeTex(Color textureColor, RectOffset border)
        {
            return MakeTex(10, 10, textureColor, border, textureColor);
        }

        public static Texture2D MakeTex(int width, int height, Color textureColor, RectOffset border)
        {
            return MakeTex(width, height, textureColor, border, textureColor);
        }

        public static Texture2D MakeTex(int width, int height, Color textureColor, RectOffset border, Color bordercolor)
        {
            var widthInner = width;
            width += border.left;
            width += border.right;

            var pix = new Color[width * (height + border.top + border.bottom)];

            for (var i = 0; i < pix.Length; i++)
                if (i < border.bottom * width)
                {
                    pix[i] = bordercolor;
                }
                else if (i >= border.bottom * width + height * width) //Border Top
                {
                    pix[i] = bordercolor;
                }
                else
                {
                    //Center of Texture
                    if (i % width < border.left) // Border left
                        pix[i] = bordercolor;
                    else if (i % width >= border.left + widthInner) //Border right
                        pix[i] = bordercolor;
                    else
                        pix[i] = textureColor; //Color texture
                }

            var result = new Texture2D(width, height + border.top + border.bottom);
            result.SetPixels(pix);
            result.Apply();
            return result;
        }

        public static Texture2D MakeTex(Color textureColor)
        {
            var pix = new Color[1];
            pix[0] = textureColor;
            var result = new Texture2D(1, 1);
            result.SetPixels(pix);
            result.Apply();
            return result;
        }

        public class StyleItem
        {
            public string Caption;
            public bool Hidden;

            /// <summary>
            ///     If defined, a popup filter is displayed to filter the list
            /// </summary>
            public PopupList ItemPopup;

            public float ItemPopupWidth;
            private float offset;
            public bool Selected;
            public GUIStyle Style;
            public string Tooltip;
            public int Value; // v2.9.0
            public bool Visible;
            public float Width;

            public StyleItem()
            {
                Visible = true;
                Style = LabelListNormal;
            }

            public StyleItem(string label, bool visible = true, bool selected = false, GUIStyle style = null)
            {
                Caption = label;
                Visible = visible;
                Selected = selected;
                Style = style == null ? LabelListNormal : style;
            }

            public StyleItem(string label, int value = 0, bool visible = true, bool selected = false,
                GUIStyle style = null)
            {
                Caption = label;
                Value = value;
                Visible = visible;
                Selected = selected;
                Style = style == null ? LabelListNormal : style;
            }

            public List<StyleItem> ItemPopupContent { get; set; }

            public float Offset
            {
                get => offset;
                set
                {
                    offset = value;
                    OffsetV = new Vector2(value, 0);
                }
            }

            public Vector2 OffsetV { get; private set; }
        }

        public class PopupList : PopupWindowContent
        {
            private readonly List<StyleItem> listItem;

            public Action<int> OnSelect;

            public Rect RectPopup;

            private Vector2 scroller;
            private int selectedIndex;
            private readonly GUIStyle styleboldLabel;
            private readonly GUIStyle styleLabel;

            public PopupList(string title, List<StyleItem> listItem, bool multiSelect = false)
            {
                MultiSelection = multiSelect;
                var items = new List<StyleItem>(listItem);
                // Search initial selectedInFilterList index
                if (!MultiSelection)
                    for (var i = 0; i < items.Count; i++)
                        if (items[i].Selected)
                        {
                            selectedIndex = i;
                            SelectedLabel = items[i].Caption;
                            break;
                        }

                this.listItem = items;
                styleLabel = EditorStyles.label;
                styleboldLabel = EditorStyles.boldLabel;
                Count();
            }

            public int SelectedIndex
            {
                get => selectedIndex;
                set
                {
                    if (value >= 0 && value < listItem.Count && listItem != null)
                    {
                        if (!MultiSelection)
                            listItem.ForEach(item => item.Selected = false);
                        listItem[value].Selected = true;
                        selectedIndex = value;
                        SelectedLabel = listItem != null && value >= 0 && value < listItem.Count
                            ? listItem[value].Caption
                            : "unknown";
                    }
                    //else
                    //    Debug.LogWarning($"SelectedIndex {value} not valid");
                }
            }

            public int SelectedValue => listItem[selectedIndex].Value;
            public string SelectedLabel { get; private set; }

            public int SelectedCount { get; private set; }

            public int TotalCount { get; private set; }

            public bool MultiSelection { get; }


            public override Vector2 GetWindowSize()
            {
                float winHeight, winWidth;
                winHeight = listItem.Count * (EditorStyles.label.lineHeight + 2f) + 2f;
                if (MultiSelection)
                    winHeight += EditorStyles.miniButtonMid.fixedHeight + 4f;
                //Debug.Log($"EditorStyles.miniButtonMid.fixedHeight={EditorStyles.miniButtonMid.fixedHeight} lineHeight:{EditorStyles.miniButtonMid.lineHeight}");
                //Debug.Log($"EditorStyles.label.fixedHeight={ EditorStyles.label.fixedHeight} lineHeight:{EditorStyles.label.lineHeight}");
                //Debug.Log($"EditorStyles.toggle.fixedHeight={EditorStyles.toggle.fixedHeight} lineHeight:{EditorStyles.toggle.lineHeight}");
                //Debug.Log($"EditorStyles.boldLabel.fixedHeight={EditorStyles.boldLabel.fixedHeight} lineHeight:{EditorStyles.boldLabel.lineHeight}");
                winWidth = RectPopup.width;
                //Debug.Log($"GetWindowSize {winWidth} {winHeight} {Data.Count} {MultiSelection}");
                return new Vector2(winWidth, winHeight);
            }

            private void Count()
            {
                SelectedCount = 0;
                foreach (var item in listItem)
                    if (item.Selected)
                        SelectedCount++;
                TotalCount = listItem.Count;
            }

            public override void OnGUI(Rect rect)
            {
                try
                {
                    if (MultiSelection)
                    {
                        GUILayout.BeginHorizontal();
                        if (GUILayout.Button("All", EditorStyles.miniButtonMid))
                        {
                            foreach (var item in listItem)
                                item.Selected = true;
                            ChangeSelection(-1);
                        }

                        if (GUILayout.Button("None", EditorStyles.miniButtonMid))
                        {
                            foreach (var item in listItem)
                                item.Selected = false;
                            ChangeSelection(-2);
                        }

                        GUILayout.Space(15);
                        if (GUILayout.Button(IconClose, EditorStyles.miniButtonMid))
                            editorWindow.Close();
                        //if (Event.current.type == EventType.Repaint) Debug.Log($"Button {GUILayoutUtility.GetLastRect()}");
                        GUILayout.EndHorizontal();
                    }

                    scroller = GUILayout.BeginScrollView(scroller, false, false);


                    for (var index = 0; index < listItem.Count; index++)
                    {
                        var item = listItem[index];
                        if (MultiSelection)
                        {
                            var select = GUILayout.Toggle(item.Selected, item.Caption);
                            //if (Event.current.type == EventType.Repaint) Debug.Log($"Toggle {GUILayoutUtility.GetLastRect()}");
                            if (select != item.Selected)
                            {
                                item.Selected = select;
                                ChangeSelection(index);
                            }
                        }
                        else
                        {
                            var styleRow = index == SelectedIndex && !MultiSelection ? styleboldLabel : styleLabel;
                            GUILayout.Label(item.Caption, styleRow);

                            //if (Event.current.type == EventType.Repaint) Debug.Log($"Label {GUILayoutUtility.GetLastRect()}");
                            if (Event.current.type == EventType.MouseDown)
                                if (GUILayoutUtility.GetLastRect().Contains(Event.current.mousePosition))
                                    ChangeSelection(index);
                        }
                    }

                    GUILayout.EndScrollView();
                }
                catch (Exception ex)
                {
                    MidiPlayerGlobal.ErrorDetail(ex);
                }
            }

            private void ChangeSelection(int index)
            {
                if (!MultiSelection)
                    SelectedIndex = index; // update also SelectedLabel
                Count();
                //Debug.Log($"Selected {SelectedIndex} '{SelectedLabel}'");
                if (OnSelect != null) OnSelect(index);
                if (!MultiSelection)
                    editorWindow.Close();
            }
        }
    }
}
#endif