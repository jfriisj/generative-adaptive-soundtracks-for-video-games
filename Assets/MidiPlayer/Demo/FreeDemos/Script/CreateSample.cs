using UnityEngine;

namespace DemoMPTK
{
    public class CreateSample : MonoBehaviour
    {
        //public int position = 0;
        public int samplerate = 44100;
        public int sampleCount = 1000;
        public int sampleChannel = 1;
        public float frequency = 440;

        // Read all the samples from the clip and half the gain
        private void Start()
        {
            //AudioSource audioSource = GetComponent<AudioSource>();
            var myClip = AudioClip.Create("MySinusoid", sampleCount, sampleChannel, samplerate, false);
            var samples = new float[sampleCount * sampleChannel];
            //myClip.GetData(samples, 0);

            //float[] samples = new float[audioSource.clip.samples * audioSource.clip.channels];
            //audioSource.clip.GetData(samples, 0);

            for (var i = 0; i < samples.Length; ++i) samples[i] = 1f; // (float)i / (float)samples.Length;

            myClip.SetData(samples, 0);
            var path = "unitySample2.wav";
            SavWav.Save(path, myClip);
        }

        private void Start_buildWhenRunning()
        {
            Debug.Log("Create AudioClip");
            var myClip = AudioClip.Create("MySinusoid", samplerate * 2, 1, samplerate, true,
                OnAudioRead /*, OnAudioSetPosition*/);

            var aud = GetComponent<AudioSource>();
            aud.clip = myClip;
            aud.Play();
        }

        private void OnAudioRead(float[] data)
        {
            var count = 0;
            while (count < data.Length)
            {
                data[count] = 1f; // Mathf.Sin(2 * Mathf.PI * frequency * position / samplerate);
                //position++;
                count++;
            }
        }

        //void OnAudioSetPosition(int newPosition)
        //{
        //    position = newPosition;
        //}
    }
}