namespace MidiPlayerTK
{
    /// <summary>
    ///     @brief
    ///     Instrument from a fluid_inst_t
    /// </summary>
    public class HiInstrument
    {
        public HiZone GlobalZone;

        /// <summary>
        ///     @brief
        ///     unique item id (see int note above)
        /// </summary>
        public int ItemId;

        public string Name;
        public HiZone[] Zone;
    }
}