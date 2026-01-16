using System;
using System.IO;

namespace MPTK.NAudio.Midi
{
    /// <summary>
    ///     @brief
    ///     Represents a MIDI note on event
    /// </summary>
    public class NoteOnEvent : NoteEvent
    {
        private NoteEvent offEvent;

        /// <summary>
        ///     @brief
        ///     Reads a new Note On event from a stream of MIDI data
        /// </summary>
        /// <param name="br">Binary reader on the MIDI data stream</param>
        public NoteOnEvent(BinaryReader br)
            : base(br)
        {
        }

        /// <summary>
        ///     @brief
        ///     Creates a NoteOn event with specified parameters
        /// </summary>
        /// <param name="absoluteTime">Absolute time of this event</param>
        /// <param name="channel">MIDI channel number</param>
        /// <param name="noteNumber">MIDI note number</param>
        /// <param name="velocity">MIDI note velocity</param>
        /// <param name="duration">MIDI note duration</param>
        public NoteOnEvent(long absoluteTime, int channel, int noteNumber,
            int velocity, int duration)
            : base(absoluteTime, channel, MidiCommandCode.NoteOn, noteNumber, velocity)
        {
            OffEvent = new NoteEvent(absoluteTime, channel, MidiCommandCode.NoteOff,
                noteNumber, 0);
            NoteLength = duration;
        }

        /// <summary>
        ///     @brief
        ///     The associated Note off event
        /// </summary>
        public NoteEvent OffEvent
        {
            get => offEvent;
            set
            {
                if (!IsNoteOff(value)) throw new ArgumentException("OffEvent must be a valid MIDI note off event");
                if (value.NoteNumber != NoteNumber)
                    throw new ArgumentException("Note Off Event must be for the same note number");
                if (value.Channel != Channel)
                    throw new ArgumentException("Note Off Event must be for the same channel");
                offEvent = value;
            }
        }

        /// <summary>
        ///     @brief
        ///     Get or set the Note Number, updating the off event at the same time
        /// </summary>
        public override int NoteNumber
        {
            get => base.NoteNumber;
            set
            {
                base.NoteNumber = value;
                if (OffEvent != null) OffEvent.NoteNumber = NoteNumber;
            }
        }

        /// <summary>
        ///     @brief
        ///     Get or set the channel, updating the off event at the same time
        /// </summary>
        public override int Channel
        {
            get => base.Channel;
            set
            {
                base.Channel = value;
                if (OffEvent != null) OffEvent.Channel = Channel;
            }
        }

        /// <summary>
        ///     @brief
        ///     The duration of this note
        /// </summary>
        /// <remarks>
        ///     There must be a note off event
        /// </remarks>
        public int NoteLength
        {
            get
            {
                // TBN Change : manage offevent null
                if (offEvent == null)
                    return 0;
                return (int)(offEvent.AbsoluteTime - AbsoluteTime);
            }
            set
            {
                if (value < 0)
                    // TBN Change : manage offevent null
                    //throw new ArgumentException("NoteLength must be 0 or greater");
                    offEvent = null;
                else
                    offEvent.AbsoluteTime = AbsoluteTime + value;
            }
        }

        /// <summary>
        ///     @brief
        ///     Creates a deep clone of this MIDI event.
        /// </summary>
        public override MidiEvent Clone()
        {
            return new NoteOnEvent(AbsoluteTime, Channel, NoteNumber, Velocity, NoteLength);
        }

        /// <summary>
        ///     @brief
        ///     Calls base class export first, then exports the data
        ///     specific to this event
        ///     <seealso cref="MidiEvent.Export">MidiEvent.Export</seealso>
        /// </summary>
        public override string ToString()
        {
            if (Velocity == 0 && OffEvent == null)
                return string.Format("{0} (Note Off)",
                    base.ToString());
            return string.Format("{0} Len: {1}",
                base.ToString(),
                OffEvent == null ? "?" : NoteLength.ToString());
        }
    }
}