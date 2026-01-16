namespace MidiPlayerTK
{
    /*
     * envelope data
     */
    public class fluid_env_data
    {
        public float coeff;
        public uint count;
        public float incr;
        public float max;
        public float min;

        public override string ToString()
        {
            return string.Format("count:{0} coeff:{1} incr:{2} min:{3} max:{4}", count, coeff, incr, min, max);
        }
    }

    /* Indices for envelope tables */
    public enum fluid_voice_envelope_index
    {
        FLUID_VOICE_ENVDELAY,
        FLUID_VOICE_ENVATTACK,
        FLUID_VOICE_ENVHOLD,
        FLUID_VOICE_ENVDECAY,
        FLUID_VOICE_ENVSUSTAIN,
        FLUID_VOICE_ENVRELEASE,
        FLUID_VOICE_ENVFINISHED,
        FLUID_VOICE_ENVLAST
    }
}