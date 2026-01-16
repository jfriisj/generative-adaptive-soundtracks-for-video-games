#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;

namespace MidiPlayerTK
{
    /// <summary>
    ///     @brief
    ///     Window editor for the setup of MPTK
    /// </summary>
    [ExecuteAlways]
    public partial class MidiFileSetupWindow : EditorWindow
    {
        private const int BUTTON_WIDTH = 150;
        private const int BUTTON_SHORT_WIDTH = 50;
        private const int BUTTON_HEIGHT = 18;
        private const float ESPACE = 5;
        private static MidiEditorLib midiPlayerLib;

        private static MidiFileSetupWindow window;

        private static Vector2 scrollPosMidiFile = Vector2.zero;
        private static Vector2 scrollPosAnalyze = Vector2.zero;
        private static Vector2 scrollPosStat = Vector2.zero;

        private static readonly float widthLeft = 500;
        private static float widthRight;

        private static float heightList;
        private static readonly float titleHeight = 18; //label title above list

        private static readonly int itemHeight = 25;

        private static readonly float xpostitlebox = 2;
        private static readonly float ypostitlebox = 5;

        public static CustomStyle myStyle;

        private static bool AutoMidiPlay;
        private static int ModeDisplay;
        private static int IndexEditItem;
        private static int IndexKeyItem;
        private static Rect listMidiVisibleRect;

        private List<MPTKGui.StyleItem> ColumnFiles;

        private void Awake()
        {
            //Debug.Log($"Awake");
            midiPlayerLib = new MidiEditorLib("MidiEditorPlayer");
            IndexEditItem = -1;
            IndexKeyItem = -1;
            //InitPlayer();
        }

        private void OnEnable()
        {
            //Debug.Log($"OnEnable");
            EditorApplication.playModeStateChanged += ChangePlayModeState;
            CompilationPipeline.compilationStarted += CompileStarted;
        }

        private void OnDestroy()
        {
            EditorApplication.playModeStateChanged -= ChangePlayModeState;
            CompilationPipeline.compilationStarted -= CompileStarted;
            if (midiPlayerLib != null) //strangely, this property can be null when window is close
                midiPlayerLib.DestroyMidiObject();
            //else
            //    Debug.LogWarning("MidiPlayerEditor is null");
        }

        private void OnGUI()
        {
            try
            {
                MPTKGui.LoadSkinAndStyle();
                // Skin must defined at each OnGUI cycle (certainly a global GUI variable)
                GUI.skin = MPTKGui.MaestroSkin;
                GUI.skin.settings.cursorColor = Color.white;
                GUI.skin.settings.cursorFlashSpeed = 0f;

                float startx = 5;
                float starty = 7;

                CheckShortCut();

                InitGUI();

                //if (myStyle == null)                    myStyle = new CustomStyle();
                GUI.Box(new Rect(0, 0, window.position.width, window.position.height), "", MPTKGui.styleWindow);

                var content = new GUIContent
                    { text = "Setup your MIDI files - Add, view, play, remove, ...", tooltip = "" };
                EditorGUI.LabelField(new Rect(startx, starty, 500, itemHeight), content, MPTKGui.styleBold);

                content = new GUIContent { text = "Doc & Contact", tooltip = "Get some help" };
                var rect = new Rect(window.position.size.x - BUTTON_WIDTH - 5, starty, BUTTON_WIDTH, BUTTON_HEIGHT);
                if (GUI.Button(rect, content, MPTKGui.Button))
                    PopupWindow.Show(rect, new AboutMPTK());

                starty += BUTTON_HEIGHT + ESPACE;

                widthRight = window.position.size.x - widthLeft - 2 * ESPACE - startx;
                heightList = window.position.size.y - 3 * ESPACE - starty;


                ShowListMidiFiles(startx, starty, widthLeft, heightList);
                //if (!autoPlay)
                //    ShowMidiAnalyse(startx + widthLeft + ESPACE, starty, widthRight, heightList);
                //else
                DisplayMidi(startx + widthLeft + ESPACE, starty, widthRight, heightList);
            }
            catch (ExitGUIException)
            {
            }
            catch (Exception /*ex*/)
            {
                //         MidiPlayerGlobal.ErrorDetail(ex);
            }
        }

        //        private void OnLostFocus()
        //        {
        //#if UNITY_2017_1_OR_NEWER
        //            // Trig an  error before v2017...
        //            if (Application.isPlaying)
        //            {
        //                window.Close();
        //            }
        //#endif
        //        }

        private void OnFocus()
        {
            // Load description of available soundfont
            try
            {
                //Debug.Log("OnFocus");
                Init();
                MidiPlayerGlobal.InitPath();
                ToolsEditor.LoadMidiSet();
                ToolsEditor.CheckMidiSet();
                AssetDatabase.Refresh();
            }
            catch (Exception ex)
            {
                MidiPlayerGlobal.ErrorDetail(ex);
            }
        }

        // % (ctrl on Windows, cmd on macOS), # (shift), & (alt).
        [MenuItem(Constant.MENU_MAESTRO + "/Midi File Setup &M", false, 10)]
        public static void Init()
        {
            // Get existing open window or if none, make a new one:
            try
            {
                if (window == null)
                {
                    //window = ScriptableObject.CreateInstance(typeof(MidiFileSetupWindow)) as MidiFileSetupWindow;
                    //window = GetWindow<MidiFileSetupWindow>(true, "MIDI File Setup");
                    window = GetWindow<MidiFileSetupWindow>(false, "MIDI DB " + Constant.version);

                    if (window == null) return;
                    window.minSize = new Vector2(828, 100);
                    window.Show();
                    //window.titleContent = new GUIContent("MIDI File Setup");
                    //Debug.Log($"Init {window.position} name:{window.name}");
                }
            }
            catch (Exception /*ex*/)
            {
                //MidiPlayerGlobal.ErrorDetail(ex); 
            }
        }

        private void ChangePlayModeState(PlayModeStateChange state)
        {
            //Debug.Log(">>> LogPlayModeState MidiEditorWindow" + state);
            if (state == PlayModeStateChange.ExitingEditMode) Close(); // call OnDestroy
            //Debug.Log("<<< LogPlayModeState MidiEditorWindow" + state);
        }

        private void CompileStarted(object obj)
        {
            // Don't appreciate recompilation when window is open
            Close(); // call OnDestroy
        }

        private void CheckShortCut()
        {
            try
            {
                var selected_index = IndexEditItem;
                var e = Event.current;
                if (e.type == EventType.KeyDown && IndexEditItem >= 0)
                    //Debug.Log("Ev.KeyDown: " + e);
                    if (e.keyCode == KeyCode.Space || e.keyCode == KeyCode.DownArrow || e.keyCode == KeyCode.UpArrow ||
                        e.keyCode == KeyCode.End || e.keyCode == KeyCode.Home)
                    {
                        if (e.keyCode == KeyCode.Space)
                        {
                            AutoMidiPlay = !AutoMidiPlay;
                            if (!AutoMidiPlay)
                            {
                                midiPlayerLib.MidiPlayer.MPTK_Stop();
                            }
                            else
                            {
                                LoadMidiFileSelected(IndexEditItem, true);
                                PlayMidiFileSelected();
                            }
                        }

                        if (e.keyCode == KeyCode.End)
                            selected_index = MidiPlayerGlobal.CurrentMidiSet.MidiFiles.Count - 1;

                        if (e.keyCode == KeyCode.Home)
                            selected_index = 0;

                        if (e.keyCode == KeyCode.DownArrow)
                        {
                            selected_index++;
                            if (selected_index >= MidiPlayerGlobal.CurrentMidiSet.MidiFiles.Count)
                                selected_index = 0;
                        }

                        if (e.keyCode == KeyCode.UpArrow)
                        {
                            selected_index--;
                            if (selected_index < 0)
                                selected_index = MidiPlayerGlobal.CurrentMidiSet.MidiFiles.Count - 1;
                        }

                        if (selected_index != -1 && selected_index != IndexEditItem) IndexKeyItem = selected_index;
                        // 'cause an exception in GUILayout.BeginHorizontal more later
                        //RefreshDisplayMidi();
                        // interesting post
                        // https://forum.unity.com/threads/unexplained-guilayout-mismatched-issue-is-it-a-unity-bug-or-a-miss-understanding.158375/
                        //SetMidiSelectedVisible();
                        //GUI.changed = true;
                        //Repaint();
                    }

                if (Event.current.type == EventType.Layout)
                    // interesting post
                    // https://forum.unity.com/threads/unexplained-guilayout-mismatched-issue-is-it-a-unity-bug-or-a-miss-understanding.158375/
                    // So each frame Unity starts with a Layout, does some Events(this is where your button code is triggered) and a Repaint.
                    // However the number and type of controls in the Repaint doesn't match the Layout and thats the problem.
                    if (IndexKeyItem != -1)
                    {
                        IndexEditItem = IndexKeyItem;
                        IndexKeyItem = -1;
                        RefreshDisplayMidi();
                        SetMidiSelectedVisible();
                        GUI.changed = true;
                        Repaint();
                    }
                //Done...
            }
            catch (Exception ex)
            {
                Debug.Log(ex);
            }
        }

        /// <summary>
        ///     @brief
        ///     Display, add, remove Midi file
        /// </summary>
        /// <param name="startX"></param>
        /// <param name="startY"></param>
        private void ShowListMidiFiles(float startX, float startY, float width, float height)
        {
            try
            {
                if (ColumnFiles == null)
                {
                    ColumnFiles = new List<MPTKGui.StyleItem>();
                    ColumnFiles.Add(new MPTKGui.StyleItem { Width = 60, Caption = "Index", Offset = 0f });
                    ColumnFiles.Add(new MPTKGui.StyleItem { Width = 406, Caption = "MIDI Name", Offset = -1f });
                    //ColumnFiles.Add(new ToolsGUI.DefineColumn() { Width = 70, Caption = "Read", PositionCaption = 0f });
                    //ColumnFiles.Add(new ToolsGUI.DefineColumn() { Width = 60, Caption = "Remove", PositionCaption = -9f });
                }

                GUI.Box(new Rect(startX, startY, width, height), "", MPTKGui.stylePanelGrayLight);

                float localstartX = 0;
                float localstartY = 0;

                var content = new GUIContent
                {
                    text = MidiPlayerGlobal.CurrentMidiSet.MidiFiles == null ||
                           MidiPlayerGlobal.CurrentMidiSet.MidiFiles.Count == 0
                        ? "No MIDI file"
                        : "Midi files available",
                    tooltip = ""
                };

                localstartX += xpostitlebox;
                localstartY += ypostitlebox;
                GUI.Label(new Rect(startX + localstartX + 5, startY + localstartY, 160, titleHeight), content,
                    MPTKGui.styleBold);

                var searchMidi =
                    EditorGUI.TextField(
                        new Rect(startX + localstartX + 5 + 110 + ESPACE, startY + localstartY - 2, 200, titleHeight),
                        "Search in list:", MPTKGui.TextField);
                if (!string.IsNullOrEmpty(searchMidi))
                {
                    var index = MidiPlayerGlobal.CurrentMidiSet.MidiFiles.FindIndex(s =>
                        s.ToLower().Contains(searchMidi.ToLower()));
                    if (index >= 0)
                    {
                        IndexEditItem = index;
                        SetMidiSelectedVisible();
                        RefreshDisplayMidi();
                    }
                }

                // Help
                if (GUI.Button(
                        new Rect(startX + localstartX + 5 + 110 + 300 + ESPACE, startY + localstartY - 2, 70,
                            BUTTON_HEIGHT), MPTKGui.IconHelp, MPTKGui.Button))
                    Application.OpenURL("https://paxstellar.fr/setup-mptk-add-midi-files-v2/");

                localstartY += titleHeight;

                // Bt remove
                if (IndexEditItem >= 0 && IndexEditItem < MidiPlayerGlobal.CurrentMidiSet.MidiFiles.Count)
                    if (GUI.Button(
                            new Rect(startX + localstartX + width - 50, startY + localstartY - 1, 40, BUTTON_HEIGHT),
                            new GUIContent(MPTKGui.IconDeleteRed,
                                $"Remove {MidiPlayerGlobal.CurrentMidiSet.MidiFiles[IndexEditItem]}"),
                            MPTKGui.Button))
                        if (EditorUtility.DisplayDialog(
                                "Remove Midi File",
                                $"Remove {MidiPlayerGlobal.CurrentMidiSet.MidiFiles[IndexEditItem]} ?",
                                "ok", "cancel"))
                        {
                            DeleteResource(
                                MidiLoad.BuildOSPath(MidiPlayerGlobal.CurrentMidiSet.MidiFiles[IndexEditItem]));
                            AssetDatabase.Refresh();
                            ToolsEditor.LoadMidiSet();
                            ToolsEditor.CheckMidiSet();
                            AssetDatabase.Refresh();
                        }

                float btWidth = 100;
                var posx = startX + localstartX + ESPACE;
                var posy = startY + localstartY;
                if (GUI.Button(new Rect(posx, posy, btWidth, BUTTON_HEIGHT), "Add a Midi File", MPTKGui.Button))
                    AddMidifile();
                posx += btWidth + ESPACE;

                btWidth = 120;
                if (GUI.Button(new Rect(posx, posy, btWidth, BUTTON_HEIGHT), "Add From Folder", MPTKGui.Button))
                    AddMidiFromFolder();
                posx += btWidth + ESPACE;

                btWidth = 100;
                if (GUI.Button(new Rect(posx, posy, btWidth, BUTTON_HEIGHT), "Open Folder", MPTKGui.Button))
                    Application.OpenURL("file://" + PathToDBMidi());
                posx += btWidth + ESPACE;

                localstartY += BUTTON_HEIGHT + ESPACE;

                // Draw title list box
                GUI.Box(new Rect(startX + localstartX + ESPACE, startY + localstartY, width - 35, itemHeight), "",
                    MPTKGui.styleListTitle);
                var boxX = startX + localstartX + ESPACE;
                foreach (var column in ColumnFiles)
                {
                    GUI.Label(new Rect(boxX + column.Offset, startY + localstartY, column.Width, itemHeight),
                        column.Caption, MPTKGui.styleListTitle);
                    boxX += column.Width;
                }

                localstartY += itemHeight + ESPACE;

                if (MidiPlayerGlobal.CurrentMidiSet.MidiFiles != null)
                {
                    listMidiVisibleRect = new Rect(startX + localstartX, startY + localstartY - 6, width - 10,
                        height - localstartY);
                    var listMidiContentRect = new Rect(0, 0, width - 35,
                        MidiPlayerGlobal.CurrentMidiSet.MidiFiles.Count * itemHeight + 5);

                    scrollPosMidiFile = GUI.BeginScrollView(listMidiVisibleRect, scrollPosMidiFile, listMidiContentRect,
                        false, true);
                    //Debug.Log($"scrollPosMidiFile:{scrollPosMidiFile.y} listVisibleRect:{listMidiVisibleRect.height} listContentRect:{listMidiContentRect.height}");
                    float boxY = 0;

                    // Loop on each midi
                    // -----------------
                    for (var i = 0; i < MidiPlayerGlobal.CurrentMidiSet.MidiFiles.Count; i++)
                    {
                        boxX = 5;

                        // MIDI name
                        if (GUI.Button(new Rect(ESPACE, boxY, width - 35, itemHeight),
                                MidiPlayerGlobal.CurrentMidiSet.MidiFiles[i],
                                IndexEditItem == i ? MPTKGui.styleListRowSelected : MPTKGui.styleListRow))
                        {
                            IndexEditItem = i;
                            try
                            {
                                RefreshDisplayMidi();
                            }
                            catch (Exception)
                            {
                            }
                        }

                        // col 0 - Type
                        var colw = ColumnFiles[0].Width;
                        EditorGUI.LabelField(new Rect(boxX, boxY + 0, colw, itemHeight - 0), i.ToString(),
                            MPTKGui.styleListRowCenter);
                        boxX += colw;

                        // col 1 - Name
                        //colw = columnSF[1].Width;
                        //content = new GUIContent() { text = MidiPlayerGlobal.CurrentMidiSet.MidiFiles[i], tooltip = MidiPlayerGlobal.CurrentMidiSet.MidiFiles[i] };
                        //EditorGUI.LabelField(new Rect(boxX + 5, boxY + 2, colw, itemHeight - 5), content, MPTKGui.styleLabelLeft);
                        //boxX += colw;

                        // col 2 - Select
                        //colw = columnSF[2].Width;
                        //if (GUI.Button(new Rect(boxX, boxY + 3, 30, buttonHeight), new GUIContent(buttonIconView, "Read Midi events")))
                        //{
                        //    IndexEditItem = i;
                        //    ReadEvents();
                        //}
                        //boxX += colw;

                        // col 3 - remove
                        //colw = columnSF[2].Width;
                        //if (GUI.Button(new Rect(boxX, boxY + 3, 30, itemHeight), EditorTools.IconDeleteRed))
                        //{
                        //    DeleteResource(MidiLoad.BuildOSPath(MidiPlayerGlobal.CurrentMidiSet.MidiFiles[i]));
                        //    AssetDatabase.Refresh();
                        //    ToolsEditor.LoadMidiSet();
                        //    ToolsEditor.CheckMidiSet();
                        //    AssetDatabase.Refresh();
                        //}
                        boxX += colw;

                        boxY += itemHeight - 1;
                    }

                    GUI.EndScrollView();
                }
            }
            catch (Exception ex)
            {
                MidiPlayerGlobal.ErrorDetail(ex);
            }
        }

        /// <summary>
        ///     @brief
        ///     Display analyse of midifile
        /// </summary>
        /// <param name="startX"></param>
        /// <param name="startY"></param>
        private void DisplayMidi(float startX, float startY, float width, float height)
        {
            float nextAreaY;
            // Begin area MIDI player commands
            // --------------------------
            float heightCommand;
            if (ModeDisplay == 0)
                heightCommand = HEIGHT_PLAYER_CMD + 75;
            else if (ModeDisplay == 1)
                heightCommand = HEIGHT_PLAYER_CMD + 56;
            else
                heightCommand = HEIGHT_PLAYER_CMD;

            GUILayout.BeginArea(new Rect(startX, startY, width, heightCommand), MPTKGui.stylePanelGrayLight);
            nextAreaY = startY + heightCommand + AREA_SPACE;

            try // Begin Player control group ----- First line common -----
            {
                GUILayout.BeginHorizontal();

                if (MidiPlayerGlobal.CurrentMidiSet == null ||
                    MidiPlayerGlobal.CurrentMidiSet.ActiveSounFontInfo == null)
                {
                    GUILayout.Label(MidiPlayerGlobal.ErrorNoSoundFont, MPTKGui.LabelBoldCentered);
                }
                else if (MidiPlayerGlobal.CurrentMidiSet.ActiveSounFontInfo.PatchCount == 0)
                {
                    GUILayout.Label(MidiPlayerGlobal.ErrorNoPreset, MPTKGui.LabelBoldCentered);
                }
                else if (IndexEditItem < 0)
                {
                    GUILayout.Label("\nSelect a MIDI file on the left panel.", MPTKGui.LabelBoldCentered);
                }
                else
                {
                    var modeChanged = false;
                    if (GUILayout.Button(MPTKGui.IconTabPrev, MPTKGui.Button, GUILayout.Width(32),
                            GUILayout.Height(24)))
                    {
                        ModeDisplay--;
                        if (ModeDisplay < 0) ModeDisplay = 2;
                        modeChanged = true;
                    }

                    ;
                    if (GUILayout.Button(MPTKGui.IconTabNext, MPTKGui.Button, GUILayout.Width(32),
                            GUILayout.Height(24)))
                    {
                        ModeDisplay++;
                        if (ModeDisplay > 2) ModeDisplay = 0;
                        modeChanged = true;
                    }

                    ;
                    var smode = ModeDisplay == 0 ? "Player" : ModeDisplay == 1 ? "Raw" : "Stat";
                    GUILayout.Label($"Mode Display {smode}", MPTKGui.LabelBoldCentered, GUILayout.Width(130),
                        GUILayout.Height(24));
                    if (modeChanged)
                        RefreshDisplayMidi();

                    string titleMidi;
                    if (ModeDisplay == 0)
                        titleMidi = midiPlayerLib.MidiPlayer.MPTK_IsPaused ? "Paused" :
                            midiPlayerLib.MidiPlayer.MPTK_IsPlaying ? "Playing" : "Loaded";
                    else
                        titleMidi = "Raw MIDI";
                    //  {MidiPlayerEditor.MidiPlayer.CoreAudioSource.isPlaying}
                    titleMidi += $": {IndexEditItem} - {MidiPlayerGlobal.CurrentMidiSet.MidiFiles[IndexEditItem]}";
                    GUILayout.Label(titleMidi, MPTKGui.LabelGray, GUILayout.Height(24));
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"{ex}");
            }
            finally
            {
                GUILayout.EndHorizontal();
            }

            if (IndexEditItem >= 0)
                try // 2nd command line
                {
                    if (ModeDisplay == 0)
                        ShowMidiPlayerCommand();
                    else if (ModeDisplay == 1)
                        ShowMidiRawCommand();
                    //else if (ModeDisplay == 2)
                    //     ; // no command line
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"{ex}");
                }

            GUILayout.EndArea();

            if (IndexEditItem >= 0)
            {
                if (ModeDisplay == 0)
                    ShowMidiPlayerEvent(startX, width, height, nextAreaY);
                else if (ModeDisplay == 1)
                    ShowMidiRawEvent(startX, width, height, nextAreaY);
                else if (ModeDisplay == 2)
                    ShowMidiStat(startX, width, height, nextAreaY);
            }

            // End MIDI events list
        }

        private void RefreshDisplayMidi()
        {
            //Debug.Log("RefreshDisplayMidi ...");

            if (ModeDisplay == 0)
            {
                LoadMidiFileSelected(IndexEditItem, true);
                if (AutoMidiPlay)
                    PlayMidiFileSelected();
            }
            else if (ModeDisplay == 1)
            {
                midiPlayerLib.MidiPlayer.MPTK_Stop();
                ReadRawMidiEvents();
            }
            else if (ModeDisplay == 2)
            {
                midiPlayerLib.MidiPlayer.MPTK_Stop();
                CalculateStat();
            }
        }

        /// <summary>
        ///     @brief
        ///     Add a new Midi file from desktop
        /// </summary>
        private static void AddMidifile()
        {
            try
            {
                var selectedFile = EditorUtility.OpenFilePanelWithFilters(
                    "Open and import MIDI file", ToolsEditor.lastDirectoryMidi,
                    new[] { "MIDI files", "mid,midi", "Karaoke files", "kar", "All", "*" });
                if (!string.IsNullOrEmpty(selectedFile))
                {
                    // selectedFile contins also the folder 
                    ToolsEditor.lastDirectoryMidi = Path.GetDirectoryName(selectedFile);
                    InsertMidiFIle(selectedFile);
                }

                AssetDatabase.Refresh();
                ToolsEditor.LoadMidiSet();
                ToolsEditor.CheckMidiSet();
                AssetDatabase.Refresh();
                window.RefreshDisplayMidi();
            }
            catch (Exception ex)
            {
                MidiPlayerGlobal.ErrorDetail(ex);
            }
        }


        /// <summary>
        ///     @brief
        ///     Add Midi files from a folder
        /// </summary>
        private static void AddMidiFromFolder()
        {
            try
            {
                var selectedFolder =
                    EditorUtility.OpenFolderPanel("Import MIDI from a folder", ToolsEditor.lastDirectoryMidi, "");
                if (!string.IsNullOrEmpty(selectedFolder))
                {
                    ToolsEditor.lastDirectoryMidi = Path.GetDirectoryName(selectedFolder);
                    var files = Directory.GetFiles(selectedFolder);
                    foreach (var file in files)
                        if (file.EndsWith(".mid") || file.EndsWith(".midi"))
                            InsertMidiFIle(file);
                }

                AssetDatabase.Refresh();
                ToolsEditor.LoadMidiSet();
                ToolsEditor.CheckMidiSet();
                AssetDatabase.Refresh();
            }
            catch (Exception ex)
            {
                MidiPlayerGlobal.ErrorDetail(ex);
            }
        }

        private static string PathToDBMidi()
        {
            // Build path to midi folder 
            var pathMidiFile = Path.Combine(Application.dataPath, MidiPlayerGlobal.PathToMidiFile);

            if (!Directory.Exists(pathMidiFile))
                Directory.CreateDirectory(pathMidiFile);
            return pathMidiFile;
        }

        public static void InsertMidiFIle(string selectedFile, string nameMidiFile = null)
        {
            // Build path to midi folder 
            var pathMidiFile = PathToDBMidi();

            MidiLoad midifile;
            try
            {
                midifile = new MidiLoad();

                var ok = true;
                using (Stream sfFile = new FileStream(selectedFile, FileMode.Open, FileAccess.Read))
                {
                    var data = new byte[sfFile.Length];
                    sfFile.Read(data, 0, (int)sfFile.Length);
                    ok = midifile.MPTK_Load(data);
                }

                if (!ok)
                {
                    EditorUtility.DisplayDialog("MIDI Not Loaded",
                        "Try to open " + selectedFile + "\nbut this file seems not a valid MIDI file", "ok");
                    return;
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarningFormat("{0} {1}", selectedFile, ex.Message);
                return;
            }

            var filename = nameMidiFile == null ? Path.GetFileNameWithoutExtension(selectedFile) : nameMidiFile;
            //foreach (char c in filename) Debug.Log(string.Format("{0} {1}", c, (int)c));
            foreach (var i in Path.GetInvalidFileNameChars()) filename = filename.Replace(i, '_');
            var filenameToSave = Path.Combine(pathMidiFile, filename + MidiPlayerGlobal.ExtensionMidiFile);

            filenameToSave = filenameToSave.Replace('(', '_');
            filenameToSave = filenameToSave.Replace(')', '_');
            filenameToSave = filenameToSave.Replace('#', '_');
            filenameToSave = filenameToSave.Replace('$', '_');

            // Create a copy of the midi file in MPTK resources
            File.Copy(selectedFile, filenameToSave, true);

            if (MidiPlayerGlobal.CurrentMidiSet.MidiFiles == null)
                MidiPlayerGlobal.CurrentMidiSet.MidiFiles = new List<string>();

            // Add midi file to the list
            var midiname = Path.GetFileNameWithoutExtension(filename);
            if (MidiPlayerGlobal.CurrentMidiSet.MidiFiles.FindIndex(s => s == midiname) < 0)
            {
                MidiPlayerGlobal.CurrentMidiSet.MidiFiles.Add(midiname);
                MidiPlayerGlobal.CurrentMidiSet.MidiFiles.Sort();
                MidiPlayerGlobal.CurrentMidiSet.Save();
            }

            IndexEditItem = MidiPlayerGlobal.CurrentMidiSet.MidiFiles.FindIndex(s => s == midiname);

            SetMidiSelectedVisible();

            Debug.Log($"MIDI file '{midiname}' added with success, " +
                      $"Index: {IndexEditItem}, " +
                      $"Duration: {midifile.MPTK_DurationMS / 1000f} second, " +
                      $"Track count:{midifile.MPTK_TrackCount}, " +
                      $"Initial Tempo:{midifile.MPTK_InitialTempo}"
            );
        }

        private static void SetMidiSelectedVisible()
        {
            if (MidiPlayerGlobal.CurrentMidiSet.MidiFiles.Count > 0)
                if (IndexEditItem >= 0 && IndexEditItem < MidiPlayerGlobal.CurrentMidiSet.MidiFiles.Count)
                {
                    float contentHeight = MidiPlayerGlobal.CurrentMidiSet.MidiFiles.Count * itemHeight;
                    scrollPosMidiFile.y = contentHeight *
                                          (IndexEditItem / (float)MidiPlayerGlobal.CurrentMidiSet.MidiFiles.Count) -
                                          listMidiVisibleRect.height / 2f;
                }
        }

        private static void DeleteResource(string filepath)
        {
            try
            {
                Debug.Log("Delete " + filepath);
                File.Delete(filepath);
                // delete also meta
                var meta = filepath + ".meta";
                Debug.Log("Delete " + meta);
                File.Delete(meta);
            }
            catch (Exception ex)
            {
                MidiPlayerGlobal.ErrorDetail(ex);
            }
        }
    }
}
#endif