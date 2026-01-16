using System;
using System.Collections.Generic;
using UnityEngine;

namespace MidiPlayerTK
{
    public class MovingAverage
    {
        private int sampleAccumulator;
        private readonly Queue<int> samples;
        private readonly int windowSize = 50;

        public MovingAverage()
        {
            sampleAccumulator = 0;
            samples = new Queue<int>();
        }

        public MovingAverage(int size)
        {
            sampleAccumulator = 0;
            samples = new Queue<int>();
            windowSize = size;
        }

        public int Count => samples.Count;

        public int Average
        {
            get
            {
                try
                {
                    if (samples != null && samples.Count > 0)
                        return sampleAccumulator / samples.Count;
                    return 0;
                }
                catch (Exception ex)
                {
                    // Strange, samples null did not trigger an exception :-(
                    Debug.LogException(ex);
                }

                return 0;
            }
        }

        /// <summary>
        ///     @brief
        ///     Computes a new windowed average each time a new sample arrives
        /// </summary>
        /// <param name="newSample"></param>
        public void Add(int newSample)
        {
            // Add a new sample
            sampleAccumulator += newSample;
            samples.Enqueue(newSample);

            if (samples.Count > windowSize)
                // Remove the older
                sampleAccumulator -= samples.Dequeue();
        }
    }
}