using UnityEngine;

namespace MidiPlayerTK
{
    public class AudioVisualizer : MonoBehaviour
    {
        public int sampleSize = 1024;
        public float[] samples;
        public LineRenderer lineRenderer;
        private int currentIndex;

        private float[] timeData;
        private float timeInterval;

        private void Start()
        {
            samples = new float[sampleSize];
            timeData = new float[sampleSize];
            lineRenderer.positionCount = sampleSize;
            timeInterval = 1.0f / AudioSettings.outputSampleRate;

            lineRenderer.startWidth = 0.1f;
            lineRenderer.endWidth = 0.1f;
            lineRenderer.useWorldSpace = false;
        }

        private void Update()
        {
            // lineRenderer.positionCount = currentIndex;
            for (var i = 0; i < sampleSize; i++)
            {
                var x = timeData[i] * 100;
                var y = samples[i] * 100;
                lineRenderer.SetPosition(i, new Vector3(x, y, 0));
            }
        }

        private void OnAudioFilterRead(float[] data, int channels)
        {
            Debug.Log($"{currentIndex} {data.Length} {channels}");
            if (samples == null || timeData == null) return;

            for (var i = 0; i < data.Length; i += channels)
            {
                var average = 0f;
                // Compute the average of all channels
                for (var j = 0; j < channels; j++) average += data[i + j];
                average /= channels;
                samples[currentIndex] = average;
                timeData[currentIndex] = currentIndex * timeInterval;
                currentIndex = (currentIndex + 1) % sampleSize;
            }
        }
    }
}