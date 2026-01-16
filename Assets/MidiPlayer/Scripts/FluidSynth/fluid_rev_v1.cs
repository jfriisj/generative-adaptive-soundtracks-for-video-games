/*

  Freeverb

  Written by Jezar at Dreampoint, June 2000
  http://www.dreampoint.co.uk
  This code is public domain

  Translated to C by Peter Hanappe, Mai 2001
*/

using UnityEngine;

/***************************************************************
 *
 *                           REVERB
 */

/* Denormalising:
 *
 * According to music-dsp thread 'Denormalise', Pentium processors
 * have a hardware 'feature', that is of interest here, related to
 * numeric underflow.  We have a recursive filter. The output decays
 * exponentially, if the input stops.  So the numbers get smaller and
 * smaller... At some point, they reach 'denormal' level.  This will
 * lead to drastic spikes in the CPU load.  The effect was reproduced
 * with the reverb - sometimes the average load over 10 s doubles!!.
 *
 * The 'undenormalise' macro fixes the problem: As soon as the number
 * is close enough to denormal level, the macro forces the number to
 * 0.0f.  The original macro is:
 *
 * #define undenormalise(sample) if(((*(unsigned int*)&sample)&0x7f800000)==0) sample=0.0f
 *
 * This will zero out a number when it reaches the denormal level.
 * Advantage: Maximum dynamic range Disadvantage: We'll have to check
 * every sample, expensive.  The alternative macro comes from a later
 * mail from Jon Watte. It will zap a number before it reaches
 * denormal level. Jon suggests to run it once per block instead of
 * every sample.
 */
namespace MidiPlayerTK
{
    public class fluid_revmodelV1
    {
        /* Denormalising part II:
         *
         * Another method fixes the problem cheaper: Use a small DC-offset in
         * the filter calculations.  Now the signals converge not against 0,
         * but against the offset.  The constant offset is invisible from the
         * outside world (i.e. it does not appear at the output.  There is a
         * very small turn-on transient response, which should not cause
         * problems.
         */


        //#define DC_OFFSET 0
        private const float DC_OFFSET = 1e-8f;

        private const int numcombs = 8;
        private const int numallpasses = 4;
        private const float fixedgain = 0.015f;
        private const float scalewet = 3.0f;
        private const float scaledamp = 1.0f;
        private const float scaleroom = 0.28f;
        private const float offsetroom = 0.7f;
        private const float initialroom = 0.5f;
        private const float initialdamp = 0.2f;
        private const int initialwet = 1;
        private const int initialdry = 0;
        private const int initialwidth = 1;
        private const int stereospread = 23;

        /*
         These values assume 44.1KHz sample rate
         they will probably be OK for 48KHz sample rate
         but would need scaling for 96KHz (or other) sample rates.
         The values were obtained by listening tests.
         */
        private const int combtuningL1 = 1116;
        private const int combtuningR1 = 1116 + stereospread;
        private const int combtuningL2 = 1188;
        private const int combtuningR2 = 1188 + stereospread;
        private const int combtuningL3 = 1277;
        private const int combtuningR3 = 1277 + stereospread;
        private const int combtuningL4 = 1356;
        private const int combtuningR4 = 1356 + stereospread;
        private const int combtuningL5 = 1422;
        private const int combtuningR5 = 1422 + stereospread;
        private const int combtuningL6 = 1491;
        private const int combtuningR6 = 1491 + stereospread;
        private const int combtuningL7 = 1557;
        private const int combtuningR7 = 1557 + stereospread;
        private const int combtuningL8 = 1617;
        private const int combtuningR8 = 1617 + stereospread;
        private const int allpasstuningL1 = 556;
        private const int allpasstuningR1 = 556 + stereospread;
        private const int allpasstuningL2 = 441;
        private const int allpasstuningR2 = 441 + stereospread;
        private const int allpasstuningL3 = 341;
        private const int allpasstuningR3 = 341 + stereospread;
        private const int allpasstuningL4 = 225;
        private const int allpasstuningR4 = 225 + stereospread;

        /* Allpass filters */
        private readonly fluid_allpass[] allpassL;

        private readonly fluid_allpass[] allpassR;

        /* Buffers for the allpasses */
        private readonly float[] bufallpassL1;
        private readonly float[] bufallpassL2;
        private readonly float[] bufallpassL3;
        private readonly float[] bufallpassL4;
        private readonly float[] bufallpassR1;
        private readonly float[] bufallpassR2;
        private readonly float[] bufallpassR3;
        private readonly float[] bufallpassR4;

        /* Buffers for the combs */
        private readonly float[] bufcombL1;
        private readonly float[] bufcombL2;
        private readonly float[] bufcombL3;
        private readonly float[] bufcombL4;
        private readonly float[] bufcombL5;
        private readonly float[] bufcombL6;
        private readonly float[] bufcombL7;
        private readonly float[] bufcombL8;
        private readonly float[] bufcombR1;
        private readonly float[] bufcombR2;
        private readonly float[] bufcombR3;
        private readonly float[] bufcombR4;
        private readonly float[] bufcombR5;
        private readonly float[] bufcombR6;
        private readonly float[] bufcombR7;
        private readonly float[] bufcombR8;

        /*
         The following are all declared inline
         to remove the need for dynamic allocation
         with its subsequent error-checking messiness
        */
        /* Comb filters */
        private readonly fluid_comb[] combL = new fluid_comb[numcombs];
        private readonly fluid_comb[] combR = new fluid_comb[numcombs];
        private float damp;

        private readonly int FLUID_BUFSIZE;
        private readonly float gain;

        //struct _fluid_revmodel_t

        private float roomsize;
        private float wet, wet1, wet2;
        private float width;

        public fluid_revmodelV1(float sample_rate, int bufsize)
        {
            FLUID_BUFSIZE = bufsize;

            combL = new fluid_comb[numcombs];
            combR = new fluid_comb[numcombs];
            for (var i = 0; i < numcombs; i++)
            {
                combL[i] = new fluid_comb();
                combR[i] = new fluid_comb();
            }

            /* Allpass filters */
            allpassL = new fluid_allpass[numallpasses];
            allpassR = new fluid_allpass[numallpasses];
            for (var i = 0; i < numallpasses; i++)
            {
                allpassL[i] = new fluid_allpass();
                allpassR[i] = new fluid_allpass();
            }

            /* Buffers for the combs */
            bufcombL1 = new float[combtuningL1];
            bufcombR1 = new float[combtuningR1];
            bufcombL2 = new float[combtuningL2];
            bufcombR2 = new float[combtuningR2];
            bufcombL3 = new float[combtuningL3];
            bufcombR3 = new float[combtuningR3];
            bufcombL4 = new float[combtuningL4];
            bufcombR4 = new float[combtuningR4];
            bufcombL5 = new float[combtuningL5];
            bufcombR5 = new float[combtuningR5];
            bufcombL6 = new float[combtuningL6];
            bufcombR6 = new float[combtuningR6];
            bufcombL7 = new float[combtuningL7];
            bufcombR7 = new float[combtuningR7];
            bufcombL8 = new float[combtuningL8];
            bufcombR8 = new float[combtuningR8];
            /* Buffers for the allpasses */
            bufallpassL1 = new float[allpasstuningL1];
            bufallpassR1 = new float[allpasstuningR1];
            bufallpassL2 = new float[allpasstuningL2];
            bufallpassR2 = new float[allpasstuningR2];
            bufallpassL3 = new float[allpasstuningL3];
            bufallpassR3 = new float[allpasstuningR3];
            bufallpassL4 = new float[allpasstuningL4];
            bufallpassR4 = new float[allpasstuningR4];
            /* Tie the components to their buffers */
            combL[0].fluid_comb_setbuffer(bufcombL1, combtuningL1);
            combR[0].fluid_comb_setbuffer(bufcombR1, combtuningR1);
            combL[1].fluid_comb_setbuffer(bufcombL2, combtuningL2);
            combR[1].fluid_comb_setbuffer(bufcombR2, combtuningR2);
            combL[2].fluid_comb_setbuffer(bufcombL3, combtuningL3);
            combR[2].fluid_comb_setbuffer(bufcombR3, combtuningR3);
            combL[3].fluid_comb_setbuffer(bufcombL4, combtuningL4);
            combR[3].fluid_comb_setbuffer(bufcombR4, combtuningR4);
            combL[4].fluid_comb_setbuffer(bufcombL5, combtuningL5);
            combR[4].fluid_comb_setbuffer(bufcombR5, combtuningR5);
            combL[5].fluid_comb_setbuffer(bufcombL6, combtuningL6);
            combR[5].fluid_comb_setbuffer(bufcombR6, combtuningR6);
            combL[6].fluid_comb_setbuffer(bufcombL7, combtuningL7);
            combR[6].fluid_comb_setbuffer(bufcombR7, combtuningR7);
            combL[7].fluid_comb_setbuffer(bufcombL8, combtuningL8);
            combR[7].fluid_comb_setbuffer(bufcombR8, combtuningR8);
            allpassL[0].fluid_allpass_setbuffer(bufallpassL1, allpasstuningL1);
            allpassR[0].fluid_allpass_setbuffer(bufallpassR1, allpasstuningR1);
            allpassL[1].fluid_allpass_setbuffer(bufallpassL2, allpasstuningL2);
            allpassR[1].fluid_allpass_setbuffer(bufallpassR2, allpasstuningR2);
            allpassL[2].fluid_allpass_setbuffer(bufallpassL3, allpasstuningL3);
            allpassR[2].fluid_allpass_setbuffer(bufallpassR3, allpasstuningR3);
            allpassL[3].fluid_allpass_setbuffer(bufallpassL4, allpasstuningL4);
            allpassR[3].fluid_allpass_setbuffer(bufallpassR4, allpasstuningR4);
            /* Set default values */
            allpassL[0].fluid_allpass_setfeedback(0.5f);
            allpassR[0].fluid_allpass_setfeedback(0.5f);
            allpassL[1].fluid_allpass_setfeedback(0.5f);
            allpassR[1].fluid_allpass_setfeedback(0.5f);
            allpassL[2].fluid_allpass_setfeedback(0.5f);
            allpassR[2].fluid_allpass_setfeedback(0.5f);
            allpassL[3].fluid_allpass_setfeedback(0.5f);
            allpassR[3].fluid_allpass_setfeedback(0.5f);

            /* set values manually, since calling set functions causes update
               and all values should be initialized for an update */
            roomsize = initialroom * scaleroom + offsetroom;
            damp = initialdamp * scaledamp;
            wet = initialwet * scalewet;
            width = initialwidth;
            gain = fixedgain;

            /* now its okay to update reverb */
            fluid_revmodel_update();

            /* Clear all buffers */
            fluid_revmodel_init();
        }

        public float Roomsize
        {
            get => roomsize;
            set
            {
                roomsize = Mathf.Clamp(value, 0f, 1f);
                fluid_revmodel_update();
            }
        }

        public float Damp
        {
            get => damp;
            set
            {
                damp = Mathf.Clamp(value, 0f, 1f);
                fluid_revmodel_update();
            }
        }

        public float Level
        {
            get => fluid_revmodel_getlevel();
            set => fluid_revmodel_setlevel(value);
        }

        public float Width
        {
            get => width;
            set
            {
                width = value;
                fluid_revmodel_update();
            }
        }

        public void fluid_revmodel_init()
        {
            int i;
            for (i = 0; i < numcombs; i++)
            {
                combL[i].fluid_comb_init();
                combR[i].fluid_comb_init();
            }

            for (i = 0; i < numallpasses; i++)
            {
                allpassL[i].fluid_allpass_init();
                allpassR[i].fluid_allpass_init();
            }
        }

        public void fluid_revmodel_reset()
        {
            fluid_revmodel_init();
        }

        public void fluid_revmodel_processreplace(float[] inp, float[] left_out, float[] right_out)
        {
            int i, k = 0;
            float outL, outR, input;

            for (k = 0; k < FLUID_BUFSIZE; k++)
            {
                outL = outR = 0;

                /* The original Freeverb code expects a stereo signal and 'input'
                 * is set to the sum of the left and right input sample. Since
                 * this code works on a mono signal, 'input' is set to twice the
                 * input sample. */
                input = (2 * inp[k] + DC_OFFSET) * gain;

                /* Accumulate comb filters in parallel */
                for (i = 0; i < numcombs; i++)
                {
                    //combL[i].fluid_comb_process(input, outL);
                    var _tmp = combL[i].buffer[combL[i].bufidx];
                    combL[i].filterstore = _tmp * combL[i].damp2 + combL[i].filterstore * combL[i].damp1;
                    combL[i].buffer[combL[i].bufidx] = input + combL[i].filterstore * combL[i].feedback;
                    if (++combL[i].bufidx >= combL[i].bufsize) combL[i].bufidx = 0;
                    outL += _tmp;

                    //combR[i].fluid_comb_process(input, outR);
                    _tmp = combR[i].buffer[combR[i].bufidx];
                    combR[i].filterstore = _tmp * combR[i].damp2 + combR[i].filterstore * combR[i].damp1;
                    combR[i].buffer[combR[i].bufidx] = input + combR[i].filterstore * combR[i].feedback;
                    if (++combR[i].bufidx >= combR[i].bufsize) combR[i].bufidx = 0;
                    outR += _tmp;
                }

                /* Feed through allpasses in series */
                for (i = 0; i < numallpasses; i++)
                {
                    //fluid_allpass_process(allpassL[i], outL);
                    float output;
                    float bufout;
                    bufout = allpassL[i].buffer[allpassL[i].bufidx];
                    output = bufout - outL;
                    allpassL[i].buffer[allpassL[i].bufidx] = outL + bufout * allpassL[i].feedback;
                    if (++allpassL[i].bufidx >= allpassL[i].bufsize) allpassL[i].bufidx = 0;
                    outL = output;

                    //fluid_allpass_process(allpassR[i], outR);
                    bufout = allpassR[i].buffer[allpassR[i].bufidx];
                    output = bufout - outR;
                    allpassR[i].buffer[allpassR[i].bufidx] = outR + bufout * allpassR[i].feedback;
                    if (++allpassR[i].bufidx >= allpassR[i].bufsize) allpassR[i].bufidx = 0;
                    outR = output;
                }

                /* Remove the DC offset */
                outL -= DC_OFFSET;
                outR -= DC_OFFSET;

                /* Calculate output REPLACING anything already there */
                left_out[k] = outL * wet1 + outR * wet2;
                right_out[k] = outR * wet1 + outL * wet2;
            }
        }

        public void fluid_revmodel_processmix(float[] inp, float[] left_out, float[] right_out)
        {
            int i, k = 0;
            float outL, outR, input;

            for (k = 0; k < FLUID_BUFSIZE; k++)
            {
                outL = outR = 0;

                /* The original Freeverb code expects a stereo signal and 'input'
                 * is set to the sum of the left and right input sample. Since
                 * this code works on a mono signal, 'input' is set to twice the
                 * input sample. */
                input = (2 * inp[k] + DC_OFFSET) * gain;

                /* Accumulate comb filters in parallel */
                for (i = 0; i < numcombs; i++)
                {
                    //combL[i].fluid_comb_process(input, outL);
                    var _tmp = combL[i].buffer[combL[i].bufidx];
                    combL[i].filterstore = _tmp * combL[i].damp2 + combL[i].filterstore * combL[i].damp1;
                    combL[i].buffer[combL[i].bufidx] = input + combL[i].filterstore * combL[i].feedback;
                    if (++combL[i].bufidx >= combL[i].bufsize) combL[i].bufidx = 0;
                    outL += _tmp;

                    //combR[i].fluid_comb_process(input, outR);
                    _tmp = combR[i].buffer[combR[i].bufidx];
                    combR[i].filterstore = _tmp * combR[i].damp2 + combR[i].filterstore * combR[i].damp1;
                    combR[i].buffer[combR[i].bufidx] = input + combR[i].filterstore * combR[i].feedback;
                    if (++combR[i].bufidx >= combR[i].bufsize) combR[i].bufidx = 0;
                    outR += _tmp;
                }

                /* Feed through allpasses in series */
                for (i = 0; i < numallpasses; i++)
                {
                    //fluid_allpass_process(allpassL[i], outL);
                    float output;
                    float bufout;
                    bufout = allpassL[i].buffer[allpassL[i].bufidx];
                    output = bufout - outL;
                    allpassL[i].buffer[allpassL[i].bufidx] = outL + bufout * allpassL[i].feedback;
                    if (++allpassL[i].bufidx >= allpassL[i].bufsize) allpassL[i].bufidx = 0;
                    outL = output;

                    //fluid_allpass_process(allpassR[i], outR);
                    bufout = allpassR[i].buffer[allpassR[i].bufidx];
                    output = bufout - outR;
                    allpassR[i].buffer[allpassR[i].bufidx] = outR + bufout * allpassR[i].feedback;
                    if (++allpassR[i].bufidx >= allpassR[i].bufsize) allpassR[i].bufidx = 0;
                    outR = output;
                }

                /* Remove the DC offset */
                outL -= DC_OFFSET;
                outR -= DC_OFFSET;

                /* Calculate output MIXING with anything already there */
                left_out[k] += outL * wet1 + outR * wet2;
                right_out[k] += outR * wet1 + outL * wet2;
            }
        }

        public void fluid_revmodel_update()
        {
            /* Recalculate internal values after parameter change */
            int i;

            wet1 = wet * (width / 2 + 0.5f);
            wet2 = wet * ((1 - width) / 2);

            for (i = 0; i < numcombs; i++)
            {
                combL[i].feedback = roomsize;
                combR[i].feedback = roomsize;
            }

            for (i = 0; i < numcombs; i++)
            {
                //fluid_comb_setdamp(&combL[i], damp);
                combL[i].damp1 = damp;
                combL[i].damp2 = 1 - damp;

                //fluid_comb_setdamp(&combR[i], damp);
                combR[i].damp1 = damp;
                combR[i].damp2 = 1 - damp;
            }
        }

        public void fluid_revmodel_set(int set, float proomsize, float pdamping, float pwidth, float plevel)
        {
            Roomsize = proomsize;
            Damp = pdamping;
            width = pwidth;
            Level = plevel;

            /* updates internal parameters */
            fluid_revmodel_update();
        }

        /*
         The following get/set functions are not inlined, because
         speed is never an issue when calling them, and also
         because as you develop the reverb model, you may
         wish to take dynamic action when they are called.
        */
        public void fluid_revmodel_setroomsize(float value)
        {
            /*   fluid_clip(value, 0.0f, 1.0f); */
            roomsize = value * scaleroom + offsetroom;
            fluid_revmodel_update();
        }

        public float fluid_revmodel_getroomsize()
        {
            return (roomsize - offsetroom) / scaleroom;
        }

        public void fluid_revmodel_setdamp(float value)
        {
            /*   fluid_clip(value, 0.0f, 1.0f); */
            damp = value * scaledamp;
            fluid_revmodel_update();
        }

        public float fluid_revmodel_getdamp()
        {
            return damp / scaledamp;
        }

        public void fluid_revmodel_setlevel(float value)
        {
            if (value < 0f)
                value = 0f;
            else if (value > 1f)
                value = 1f;
            wet = value * scalewet;
            fluid_revmodel_update();
        }

        public float fluid_revmodel_getlevel()
        {
            return wet / scalewet;
        }

        public void fluid_revmodel_setwidth(float value)
        {
            /*   fluid_clip(value, 0.0f, 1.0f); */
            width = value;
            fluid_revmodel_update();
        }

        public float fluid_revmodel_getwidth()
        {
            return width;
        }
        //#define DC_OFFSET 0.001f

        public class fluid_allpass
        {
            public float[] buffer;
            public int bufidx;
            public int bufsize;
            public float feedback;

            public void fluid_allpass_setbuffer(float[] buf, int size)
            {
                bufidx = 0;
                buffer = buf;
                bufsize = size;
            }

            public void fluid_allpass_init()
            {
                int i;
                var len = bufsize;
                var buf = buffer;
                for (i = 0; i < len; i++) buf[i] = DC_OFFSET; /* this is not 100 % correct. */
            }

            public void fluid_allpass_setfeedback(float val)
            {
                feedback = val;
            }

            public float fluid_allpass_getfeedback()
            {
                return feedback;
            }
        }

        public class fluid_comb
        {
            public float[] buffer;
            public int bufidx;
            public int bufsize;
            public float damp1;
            public float damp2;
            public float feedback;
            public float filterstore;

            public void fluid_comb_setbuffer(float[] buf, int size)
            {
                filterstore = 0;
                bufidx = 0;
                buffer = buf;
                bufsize = size;
            }

            public void fluid_comb_init()
            {
                int i;
                var buf = buffer;
                var len = bufsize;
                for (i = 0; i < len; i++) buf[i] = DC_OFFSET; /* This is not 100 % correct. */
            }

            public void fluid_comb_setdamp(float val)
            {
                damp1 = val;
                damp2 = 1 - val;
            }

            public float fluid_comb_getdamp()
            {
                return damp1;
            }

            public void fluid_comb_setfeedback(float val)
            {
                feedback = val;
            }

            public float fluid_comb_getfeedback()
            {
                return feedback;
            }
        }
    }
}