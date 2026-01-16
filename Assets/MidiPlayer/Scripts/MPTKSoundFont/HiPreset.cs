namespace MidiPlayerTK
{
    /// <summary>
    ///     @brief
    ///     Preset from a ImSoundFont
    /// </summary>
    public class HiPreset
    {
        /// <summary>
        ///     @brief
        ///     the bank number
        /// </summary>
        public int Bank;

        public uint Genre; /* Not used (preserved) */

        public HiZone GlobalZone;

        /// <summary>
        ///     @brief
        ///     unique item id (see int note above)
        /// </summary>
        public int ItemId;

        public uint Libr; /* Not used (preserved) */
        public uint Morph; /* Not used (preserved) */
        public string Name;
        public int Num;
        public HiZone[] Zone;

        public string Description()
        {
            return string.Format(" {0,3:000} {1}", Num, Name);
        }
    }
}