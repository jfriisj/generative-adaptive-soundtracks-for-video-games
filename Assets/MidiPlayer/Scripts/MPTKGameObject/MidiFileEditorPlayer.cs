//#define MPTK_PRO

#define DEBUG_START_MIDIx
using UnityEngine;

namespace MidiPlayerTK
{
    // required to instanciate in edit mode: Awake() and Start() are executed when this class is instanciated
    [ExecuteAlways]
    public class MidiFileEditorPlayer : MidiFilePlayer
    {
        private new void Awake()
        {
            MPTK_CorePlayer = true;
            base.Awake();
        }
    }
}