using UnityEngine;

namespace DemoMVPSwitchScene
{
    public class NextMidiFile : MonoBehaviour
    {
        private void Awake()
        {
        }

        // Start is called before the first frame update
        private void Start()
        {
        }

        // Update is called once per frame
        private void Update()
        {
        }

        public void NextMidi()
        {
            if (LoadMidiFilePlayer.Instance.midiFilePlayer != null)
            {
                // Access to the MidiFilePlayer from the static class and its singleton instance
                LoadMidiFilePlayer.Instance.midiFilePlayer.MPTK_Next();
                Debug.Log($"Next MIDI file: {LoadMidiFilePlayer.Instance.midiFilePlayer.MPTK_MidiName}");
            }
            else
            {
                Debug.LogWarning("No MidiFilePlayer found");
            }
        }
    }
}