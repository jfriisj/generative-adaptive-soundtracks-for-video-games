using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace MidiPlayerTK
{
    /// <summary>
    ///     @brief
    ///     ImBank of an ImSoundFont
    /// </summary>
    public class ImBank
    {
        public int BankNumber;

        //public ImPreset[] Presets;
        public HiPreset[] defpresets;

        [XmlIgnore] public string Description = "DEPRECATED";

        [XmlIgnore] public int PatchCount;

        public List<string> GetDescription()
        {
            var description = new List<string>();
            try
            {
                if (defpresets != null)
                    foreach (var preset in defpresets)
                        if (preset != null)
                            description.Add(string.Format("[{0:000}] {1}", preset.Num, preset.Name));
            }
            catch (Exception ex)
            {
                MidiPlayerGlobal.ErrorDetail(ex);
            }

            return description;
        }
    }
}