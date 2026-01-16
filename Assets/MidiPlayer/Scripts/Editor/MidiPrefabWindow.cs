#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

namespace MidiPlayerTK
{
    // http://trinary.tech/category/mec/mec-pro/

    /// <summary>
    ///     @brief
    ///     Window editor for the setup of MPTK
    /// </summary>

    // ensure class initializer is called whenever scripts recompile
    [ExecuteAlways]
    public class MidiPrefabWindow : EditorWindow
    {
        private static MidiPrefabWindow window;
        private static readonly string nameSequencer = "MidiPrefabEditor";
        private MidiFilePlayer midiFilePlayer;

        private void OnEnable()
        {
            Debug.Log(">>> OnEnable..." + nameSequencer);

            EditorApplication.playModeStateChanged += LogPlayModeState;

            MidiPlayerGlobal.InitPath();
            ToolsEditor.LoadMidiSet();

            var prefab = Resources.Load<MidiFilePlayer>("PrefabsEditor/MidiPlayer");
            midiFilePlayer = Instantiate(prefab);
            midiFilePlayer.name = nameSequencer;
            //MidiPlayerGlobal midiPlayerGlobal = midiFilePlayer.gameObject.GetComponentInChildren<MidiPlayerGlobal>();
            var midiPlayerGlobal = FindFirstObjectByType<MidiPlayerGlobal>();
            if (midiPlayerGlobal == null)
                Debug.Log("Not found MidiPlayerGlobal");
            else
                Debug.Log("Found MidiPlayerGlobal");

            //string globalInst = MidiPlayerGlobal.Instance == null ? "No global instance" : "global instance exist";
            //Debug.Log($"MidiPlayerGlobal.Instance: {globalInst}");
            midiPlayerGlobal.InitInstance();

            //MidiPlayerGlobal global = midiFilePlayer.gameObject.AddComponent<MidiPlayerGlobal>();
            //global.InitInstance();
            MidiPlayerGlobal.OnEventPresetLoaded.AddListener(SoundFontIsReadyEvent);
            //MidiPlayerGlobal.MPTK_LoadLiveSF("file://" + MidiPlayerGlobal.CurrentMidiSet.ActiveSounFontInfo.SF2Path, -1, -1, false);
            MidiPlayerGlobal.LoadCurrentSF();
            //GameObject.Find("Button" + i).transform.parent = refParent.transform.parent;

            Debug.Log("<<< OnEnable..." + nameSequencer);
        }

        private void OnDestroy()
        {
            DestroyMidiObject();
        }

        private void OnGUI()
        {
            try
            {
                MPTKGui.LoadSkinAndStyle();

                //if (window == null)
                //{
                //    Init();
                //}
                if (midiFilePlayer == null)
                {
                }

                GUILayout.BeginHorizontal(GUILayout.Width(300));
                GUILayout.Label("Midi", MPTKGui.styleBold, GUILayout.Width(150));
                if (GUILayout.Button("Play", GUILayout.Width(100)))
                {
                    //MidiPlayerGlobal.MPTK_SoundFontLoaded = true;
                    // Select a MIDI from the MIDI DB (with exact name)
                    //midiFilePlayer.MPTK_MidiName = "Bach - Fugue"; 
                    //midiFilePlayer.MPTK_MidiName = "All Night Long";
                    midiFilePlayer.MPTK_MidiIndex = 11;
                    // Play the MIDI file
                    midiFilePlayer.MPTK_Play();
                }

                if (GUILayout.Button("Stop", GUILayout.Width(100))) midiFilePlayer.MPTK_Stop();
                GUILayout.EndHorizontal();
            }
            //catch (ExitGUIException) { }
            catch (Exception ex)
            {
                Debug.Log(ex.Message);
                //MidiPlayerGlobal.ErrorDetail(ex);
            }
        }

        private void OnInspectorUpdate()
        {
            // Repaint();
        }

        // % (ctrl on Windows, cmd on macOS), # (shift), & (alt).
        //[MenuItem(Constant.MENU_MAESTRO + "/Midi Prefab", false, 1)]
        public static void Init()
        {
            // Get existing open window or if none, make a new one:
            try
            {
                //GameObject seq = GameObject.Find(nameSequencer);
                //if (seq != null)
                //    return;

                window = CreateInstance(typeof(MidiPrefabWindow)) as MidiPrefabWindow;
                window.Show();
                window.titleContent = new GUIContent("MIDI Prefab Window");
                window.minSize = new Vector2(300, 200);
                Debug.Log($"Init name:{window.name} at {window.position}");
            }
            catch (Exception /*ex*/)
            {
                //MidiPlayerGlobal.ErrorDetail(ex);
            }
        }

        public void SoundFontIsReadyEvent()
        {
            Debug.LogFormat("End loading SF '{0}', MPTK is ready to play", MidiPlayerGlobal.ImSFCurrent.SoundFontName);
            Debug.Log("   Time To Load SoundFont: " +
                      Math.Round(MidiPlayerGlobal.MPTK_TimeToLoadSoundFont.TotalSeconds, 3) + " second");
            Debug.Log("   Time To Load Samples: " + Math.Round(MidiPlayerGlobal.MPTK_TimeToLoadWave.TotalSeconds, 3) +
                      " second");
            Debug.Log("   Presets Loaded: " + MidiPlayerGlobal.MPTK_CountPresetLoaded);
            Debug.Log("   Samples Loaded: " + MidiPlayerGlobal.MPTK_CountWaveLoaded);
        }

        private void LogPlayModeState(PlayModeStateChange state)
        {
            Debug.Log(">>> LogPlayModeState " + state);
            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                DestroyMidiObject();
                Close();
            }

            Debug.Log("<<< LogPlayModeState " + state);
        }

        private void DestroyMidiObject()
        {
            Debug.Log(">>> DestroyMidiObject ... Application.isPlaying:" + Application.isPlaying);
            if (midiFilePlayer != null)
            {
                Debug.Log($"     Stop MidiPlayer {midiFilePlayer.IdSynth}");
                //midiFilePlayer.MPTK_Stop();
                //midiFilePlayer.MPTK_StopSynth();
                //midiFilePlayer.CoreAudioSource.Stop();
                if (Application.isPlaying)
                    Destroy(midiFilePlayer);
                else
                    DestroyImmediate(midiFilePlayer, true);
            }

            midiFilePlayer = null;
            Debug.Log("<<< DestroyMidiObject ... " + Application.isPlaying);
        }
    }
}
#endif