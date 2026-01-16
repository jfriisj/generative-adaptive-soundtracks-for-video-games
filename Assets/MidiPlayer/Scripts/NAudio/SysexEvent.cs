using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace MPTK.NAudio.Midi
{
    /// <summary>
    ///     @brief
    ///     Represents a MIDI sysex message
    /// </summary>
    public class SysexEvent : MidiEvent
    {
        private byte[] data;
        //private int length;

        /// <summary>
        ///     @brief
        ///     Reads a sysex message from a MIDI stream
        /// </summary>
        /// <param name="br">Stream of MIDI data</param>
        /// <returns>a new sysex message</returns>
        public static SysexEvent ReadSysexEvent(BinaryReader br)
        {
            var se = new SysexEvent();
            //se.length = ReadVarInt(br);
            //se.data = br.ReadBytes(se.length);

            var sysexData = new List<byte>();
            var loop = true;
            while (loop)
            {
                byte b;
                try //MPTK V2.85 add try catch in case of sysex without 0xF7
                {
                    b = br.ReadByte();
                    if (b == 0xF7)
                        loop = false;
                    else
                        sysexData.Add(b);
                }
                catch (Exception /*ex*/)
                {
                    //UnityEngine.Debug.Log(ex.Message);
                    loop = false;
                }
            }

            se.data = sysexData.ToArray();

            return se;
        }

        /// <summary>
        ///     @brief
        ///     Creates a deep clone of this MIDI event.
        /// </summary>
        public override MidiEvent Clone()
        {
            object retData = null;
            if (data != null)
                retData = data.Clone();
            return new SysexEvent { data = (byte[])retData };
        }

        /// <summary>
        ///     @brief
        ///     Describes this sysex message
        /// </summary>
        /// <returns>A string describing the sysex message</returns>
        public override string ToString()
        {
            var sb = new StringBuilder();
            foreach (var b in data) sb.AppendFormat("{0:X2} ", b);
            return string.Format("{0} Sysex: {1} bytes\r\n{2}", AbsoluteTime, data.Length, sb);
        }

        /// <summary>
        ///     @brief
        ///     Calls base class export first, then exports the data
        ///     specific to this event
        ///     <seealso cref="MidiEvent.Export">MidiEvent.Export</seealso>
        /// </summary>
        public override void Export(ref long absoluteTime, BinaryWriter writer)
        {
            base.Export(ref absoluteTime, writer);
            //WriteVarInt(writer,length);
            //writer.Write(data, 0, data.Length);
            writer.Write(data, 0, data.Length);
            writer.Write((byte)0xF7);
        }
    }
}