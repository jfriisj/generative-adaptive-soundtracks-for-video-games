//#define MPTK_PRO

using System;
using System.Collections.Generic;
using MidiPlayerTK;
using UnityEngine;

namespace DemoMPTK
{
    public class GUISelectSoundFont : MonoBehaviour
    {
        public static List<MPTKListItem> SoundFonts;
        private static PopupListItem PopSoundFont;
        private static int selectedSf;
        private static string sfLocalName;

        private static void SoundFontChanged(object tag, int midiindex, int indexList)
        {
#if MPTK_PRO
            Debug.Log($"SoundFontChanged index:{midiindex}, load {(PopSoundFont.Option ? "global" : "local")}.");
            if (PopSoundFont.Option)
            {
                // Change the selected SoundFont defined in MPTK.
                sfLocalName = null;
                MidiPlayerGlobal.MPTK_SelectSoundFont(MidiPlayerGlobal.MPTK_ListSoundFont[midiindex]);
            }
            else
            {
                // Use a SoundFont defined in MPTK, the default MPTK soundfont is not changed.
                sfLocalName = MidiPlayerGlobal.MPTK_ListSoundFont[midiindex];
            }

            // Apply soundfont switch to all MidiSynth available on the scene
            MidiSynth[] MidiSynths = FindObjectsByType<MidiSynth>(FindObjectsSortMode.None);
            foreach (MidiSynth midiSynth in MidiSynths)
                midiSynth.MPTK_SoundFont.Load(sfLocalName);

            selectedSf = midiindex;
#else
            Debug.Log("Can't change SoundFont selected with MPTK Free version");
#endif
        }

        public static void Display(Vector2 scrollerWindow, CustomStyle myStyle, float width)
        {
            SoundFonts = new List<MPTKListItem>();
            if (MidiPlayerGlobal.MPTK_ListSoundFont == null) return;
            foreach (var name in MidiPlayerGlobal.MPTK_ListSoundFont)
            {
#if MPTK_PRO
                if (PopSoundFont != null && sfLocalName==null)
                {
                    if (MidiPlayerGlobal.CurrentMidiSet != null && MidiPlayerGlobal.CurrentMidiSet.ActiveSounFontInfo != null && name == MidiPlayerGlobal.CurrentMidiSet.ActiveSounFontInfo.Name)
                        selectedSf = SoundFonts.Count;
                }
                else
                {
                    // selectedSf = last value
                }
#else
                if (MidiPlayerGlobal.CurrentMidiSet != null &&
                    MidiPlayerGlobal.CurrentMidiSet.ActiveSounFontInfo != null &&
                    name == MidiPlayerGlobal.CurrentMidiSet.ActiveSounFontInfo.Name)
                    selectedSf = SoundFonts.Count;
#endif
                SoundFonts.Add(new MPTKListItem { Index = SoundFonts.Count, Label = name });
            }

            if (PopSoundFont == null)
                PopSoundFont = new PopupListItem
                {
                    Title = "Select A SoundFont",
                    OnSelect = SoundFontChanged,
                    ColCount = 1,
                    ColWidth = 600,
                    Option = true // Change default soundfont
                };

            if (SoundFonts != null)
            {
                PopSoundFont.Draw(SoundFonts, selectedSf, myStyle,
                    new GUIContent("Global", "Change default soundfont and apply choice."));
                GUILayout.BeginHorizontal(myStyle.BacgDemosMedium, GUILayout.Width(width));
                //GUILayout.Space(20);
                float height = 30;
                if (MidiPlayerGlobal.ImSFCurrent != null)
                {
                    if (MidiPlayerGlobal.ImSFCurrent.LiveSF)
                    {
                        GUILayout.Label("Live SoundFont: " + MidiPlayerGlobal.ImSFCurrent.SoundFontName,
                            myStyle.TitleLabel2, GUILayout.Height(height));
                    }
                    else
                    {
                        // Display popup with SF list
                        var name = sfLocalName != null
                            ? sfLocalName + " (internal)"
                            : MidiPlayerGlobal.MPTK_ListSoundFont[selectedSf] + " (default)";
                        GUILayout.Label("Select a SoundFont:", myStyle.TitleLabel3, GUILayout.Width(80));
                        if (GUILayout.Button(name, GUILayout.Width(300), GUILayout.Height(height)))
                            PopSoundFont.Show = !PopSoundFont.Show;
                    }

                    GUILayout.Space(10);
                    GUILayout.Label(string.Format("Load Time:{0} s    Samples:{1} s    Count Presets:{2}   Samples:{3}",
                            Math.Round(MidiPlayerGlobal.MPTK_TimeToLoadSoundFont.TotalSeconds, 3),
                            Math.Round(MidiPlayerGlobal.MPTK_TimeToLoadWave.TotalSeconds, 3),
                            MidiPlayerGlobal.MPTK_CountPresetLoaded,
                            MidiPlayerGlobal.MPTK_CountWaveLoaded),
                        myStyle.TitleLabel3, GUILayout.Height(height));
                }
                else
                {
                    GUILayout.Label("No SoundFont loaded", myStyle.TitleLabel2, GUILayout.Height(height));
                }

                GUILayout.EndHorizontal();

                PopSoundFont.PositionWithScroll(ref scrollerWindow);
            }
            else
            {
                GUILayout.Label("No SoundFont found");
            }
        }
    }
}