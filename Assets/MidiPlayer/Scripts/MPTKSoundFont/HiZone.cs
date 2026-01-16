namespace MidiPlayerTK
{
    /// <summary>
    ///     @brief
    ///     Cover fluid_inst_zone_t and fluid_preset_zone_t
    /// </summary>
    public class HiZone
    {
        public HiGen[] genE;

        //public fluid_gen_t[] gen;
        public HiGen[] gens; // gen in FS 2.3 

        // V2.89.0 removed (not used)  Instrument defined in this zone (only for preset zone)
        // public HiInstrument Instrument;

        //public string Name;
        //public fluid_sample_t sample;
        /// <summary>
        ///     @brief
        ///     Index to the sample (only for instrument zone)
        /// </summary>
        public int Index;

        /// <summary>
        ///     @brief
        ///     unique item id (see int note above)
        /// </summary>
        public int ItemId;

        public int KeyHi;
        public int KeyLo;

        public HiMod[] mods; /* List of modulators */
        public int VelHi;
        public int VelLo;


        public HiZone()
        {
            //sample = null;
            Index = -1;
            KeyLo = 0;
            KeyHi = 128;
            VelLo = 0;
            VelHi = 128;
        }
    }
}