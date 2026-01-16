using System.IO;
using System.Text;
using MPTK.NAudio.Utils;

namespace MPTK.NAudio.Midi
{
    /// <summary>
    ///     @brief
    ///     Represents a MIDI text event
    /// </summary>
    public class TextEvent : MetaEvent
    {
        /// <summary>
        ///     @brief
        ///     Reads a new text event from a MIDI stream
        /// </summary>
        /// <param name="br">The MIDI stream</param>
        /// <param name="length">The data length</param>
        public TextEvent(BinaryReader br, int length, bool extendedText = false)
        {
            if (extendedText) // TBN
            {
                Text = Encoding.UTF8.GetString(br.ReadBytes(length));
            }
            else
            {
                Encoding byteEncoding = ByteEncoding.Instance;
                Text = byteEncoding.GetString(br.ReadBytes(length));
            }
        }

        /// <summary>
        ///     @brief
        ///     Creates a new TextEvent
        /// </summary>
        /// <param name="text">The text in this type</param>
        /// <param name="metaEventType">
        ///     MetaEvent type (must be one that is
        ///     associated with text data)
        /// </param>
        /// <param name="absoluteTime">Absolute time of this event</param>
        /// <param name="extendedText">Text encoding to UTF8, default false</param>
        public TextEvent(string text, MetaEventType metaEventType, long absoluteTime, bool extendedText = false) // TBN
            : base(metaEventType, text.Length, absoluteTime)
        {
            this.Text = text;
            this.extendedText = extendedText; // TBN
        }

        /// <summary>
        ///     @brief
        ///     The contents of this text event
        /// </summary>
        public string Text
        {
            get;
            set;
            //metaDataLength = text.Length;
        }

        /// <summary>
        ///     @brief
        ///     Creates a deep clone of this MIDI event.
        /// </summary>
        public override MidiEvent Clone()
        {
            return (TextEvent)MemberwiseClone();
        }

        /// <summary>
        ///     @brief
        ///     Describes this MIDI text event
        /// </summary>
        /// <returns>A string describing this event</returns>
        public override string ToString()
        {
            return string.Format("{0} {1}", base.ToString(), Text);
        }

        /// <summary>
        ///     @brief
        ///     Calls base class export first, then exports the data
        ///     specific to this event
        ///     <seealso cref="MidiEvent.Export">MidiEvent.Export</seealso>
        /// </summary>
        public override void Export(ref long absoluteTime, BinaryWriter writer)
        {
            byte[] encoded;
            if (extendedText) // TBN
            {
                encoded = Encoding.UTF8.GetBytes(Text);
            }
            else
            {
                Encoding byteEncoding = ByteEncoding.Instance;
                encoded = byteEncoding.GetBytes(Text);
            }

            metaDataLength = encoded.Length;

            base.Export(ref absoluteTime, writer);
            writer.Write(encoded);
        }
    }
}