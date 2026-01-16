using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

//using MPTK.NAudio.Utils;

namespace MPTK.NAudio.Midi
{
    /// <summary>
    ///     @brief
    ///     Class able to read a MIDI file
    /// </summary>
    public class MidiFile
    {
        //private ushort tracks;
        private ushort deltaTicksPerQuarterNote;

        private readonly bool extendedText; // TBN
        private ushort fileFormat;
        private readonly bool strictChecking;

        // TBN Change
        /// <summary>
        ///     @brief
        ///     Opens a MIDI file for creating from a MidiEventCollection
        /// </summary>
        /// <param name="filename">Name of MIDI file</param>
        public MidiFile(int pfileFormat, int pdeltaTicksPerQuarterNote, bool extendedText = false)
        {
            fileFormat = (ushort)pfileFormat;
            deltaTicksPerQuarterNote = (ushort)pdeltaTicksPerQuarterNote;
            Events = new MidiEventCollection(fileFormat, deltaTicksPerQuarterNote);
            this.extendedText = extendedText; // TBN
        }

        /// <summary>
        ///     @brief
        ///     Opens a MIDI file for reading
        /// </summary>
        /// <param name="filename">Name of MIDI file</param>
        public MidiFile(string filename)
            : this(filename, false)
        {
        }

        /// <summary>
        ///     @brief
        ///     Opens a MIDI file for reading
        /// </summary>
        /// <param name="filename">Name of MIDI file</param>
        /// <param name="strictChecking">If true will error on non-paired note events</param>
        public MidiFile(string filename, bool strictChecking, bool extendedText = false)
        {
            this.strictChecking = strictChecking;
            this.extendedText = extendedText; // TBN

            var br = new BinaryReader(File.OpenRead(filename));
            using (br)
            {
                MidiBinaryReader(br, strictChecking);
            }
        }


        /// <summary>
        ///     @brief
        ///     Opens a MIDI file for reading from an array of bytes
        /// </summary>
        /// <param name="bytes">contains with midi data</param>
        /// <param name="strictChecking">If true will error on non-paired note events</param>
        public MidiFile(byte[] bytes, bool strictChecking, bool extendedText = false) // TBN
        {
            this.strictChecking = strictChecking;
            this.extendedText = extendedText; // TBN

            var mr = new MemoryStream(bytes);
            using (mr)
            {
                var br = new BinaryReader(mr);
                using (br)
                {
                    MidiBinaryReader(br, strictChecking);
                }
            }
        }

        /// <summary>
        ///     @brief
        ///     MIDI File format
        /// </summary>
        public int FileFormat => fileFormat;


        /// <summary>
        ///     @brief
        ///     The collection of events in this MIDI file
        /// </summary>
        public MidiEventCollection Events { get; private set; }

        /// <summary>
        ///     @brief
        ///     Number of tracks in this MIDI file
        /// </summary>
        public int Tracks => Events.Tracks;

        /// <summary>
        ///     @brief
        ///     Delta Ticks Per Beat Note
        /// </summary>
        public int DeltaTicksPerQuarterNote => deltaTicksPerQuarterNote;

        private void MidiBinaryReader(BinaryReader br, bool strictChecking)
        {
            var chunkHeader = Encoding.UTF8.GetString(br.ReadBytes(4));
            if (chunkHeader != "MThd") throw new FormatException("Not a MIDI file - header chunk missing");
            var chunkSize = SwapUInt32(br.ReadUInt32());

            if (chunkSize != 6) throw new FormatException("Unexpected header chunk length");
            // 0 = single track, 1 = multi-track synchronous, 2 = multi-track asynchronous
            fileFormat = SwapUInt16(br.ReadUInt16());
            int tracks = SwapUInt16(br.ReadUInt16());
            deltaTicksPerQuarterNote = SwapUInt16(br.ReadUInt16());

            Events = new MidiEventCollection(fileFormat == 0 ? 0 : 1, deltaTicksPerQuarterNote);
            for (var n = 0; n < tracks; n++) Events.AddTrack();

            long absoluteTime = 0;

            for (var track = 0; track < tracks; track++)
            {
                if (fileFormat == 1) absoluteTime = 0;
                chunkHeader = Encoding.UTF8.GetString(br.ReadBytes(4));
                if (chunkHeader != "MTrk")
                    if (strictChecking)
                        throw new FormatException("Invalid chunk header");
                chunkSize = SwapUInt32(br.ReadUInt32());

                var startPos = br.BaseStream.Position;
                MidiEvent me = null;
                var outstandingNoteOns = new List<NoteOnEvent>();
                while (br.BaseStream.Position < startPos + chunkSize)
                {
                    me = MidiEvent.ReadNextEvent(br, me, extendedText); // TBN
                    absoluteTime += me.DeltaTime;
                    //UnityEngine.Debug.Log(me.DeltaTime);
                    me.AbsoluteTime = absoluteTime;
                    Events[track].Add(me);
                    if (me.CommandCode == MidiCommandCode.NoteOn)
                    {
                        var ne = (NoteEvent)me;
                        if (ne.Velocity > 0)
                            outstandingNoteOns.Add((NoteOnEvent)ne);
                        else
                            // don't remove the note offs, even though
                            // they are annoying
                            // events[track].Remove(me);
                            FindNoteOn(ne, outstandingNoteOns);
                    }
                    else if (me.CommandCode == MidiCommandCode.NoteOff)
                    {
                        FindNoteOn((NoteEvent)me, outstandingNoteOns);
                    }
                    else if (me.CommandCode == MidiCommandCode.MetaEvent)
                    {
                        var metaEvent = (MetaEvent)me;
                        if (metaEvent.MetaEventType == MetaEventType.EndTrack)
                            //break;
                            // some dodgy MIDI files have an event after end track
                            if (strictChecking)
                                if (br.BaseStream.Position < startPos + chunkSize)
                                    // TBN Change throw new FormatException(String.Format("End Track event was not the last MIDI event on track {0}", track));
                                    Debug.LogWarning(string.Format(
                                        "NAudio - MidiFile - End Track event was not the last MIDI event on track {0}",
                                        track));
                    }
                }

                if (outstandingNoteOns.Count > 0)
                    if (strictChecking)
                        // TBN Change throw new FormatException(String.Format("Note ons without note offs {0} (file format {1})", outstandingNoteOns.Count, fileFormat));
                        Debug.LogWarning(string.Format(
                            "NAudio - MidiFile - Note ons without note offs {0} (file format {1})",
                            outstandingNoteOns.Count, fileFormat));

                if (br.BaseStream.Position != startPos + chunkSize)
                    if (strictChecking)
                        // TBN Change throw new FormatException(String.Format("Read too far {0}+{1}!={2}", chunkSize, startPos, br.BaseStream.Position));
                        Debug.LogWarning(string.Format("NAudio - MidiFile - Read too far {0}+{1}!={2}", chunkSize,
                            startPos, br.BaseStream.Position));
            }
        }

        private void FindNoteOn(NoteEvent offEvent, List<NoteOnEvent> outstandingNoteOns)
        {
            var found = false;
            foreach (var noteOnEvent in outstandingNoteOns)
                if (noteOnEvent.Channel == offEvent.Channel && noteOnEvent.NoteNumber == offEvent.NoteNumber)
                {
                    noteOnEvent.OffEvent = offEvent;
                    outstandingNoteOns.Remove(noteOnEvent);
                    found = true;
                    break;
                }

            if (!found)
                if (strictChecking)
                    // TBN Change throw new FormatException(String.Format("Got an off without an on {0}", offEvent));
                    Debug.LogWarning(string.Format("NAudio - MidiFile - Got an off without an on {0}", offEvent));
        }

        private static uint SwapUInt32(uint i)
        {
            return ((i & 0xFF000000) >> 24) | ((i & 0x00FF0000) >> 8) | ((i & 0x0000FF00) << 8) |
                   ((i & 0x000000FF) << 24);
        }

        private static ushort SwapUInt16(ushort i)
        {
            return (ushort)(((i & 0xFF00) >> 8) | ((i & 0x00FF) << 8));
        }

        /// <summary>
        ///     @brief
        ///     Describes the MIDI file
        /// </summary>
        /// <returns>A string describing the MIDI file and its events</returns>
        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.AppendFormat("Format {0}, Tracks {1}, Delta Ticks Per Quarter Note {2}\r\n",
                fileFormat, Tracks, deltaTicksPerQuarterNote);
            for (var n = 0; n < Tracks; n++)
                foreach (var midiEvent in Events[n])
                    sb.AppendFormat("{0}\r\n", midiEvent);

            return sb.ToString();
        }

        /// <summary>
        ///     @brief
        ///     Exports a MIDI file
        /// </summary>
        /// <param name="filename">Filename to export to</param>
        /// <param name="events">Events to export</param>
        public static void Export(string filename, MidiEventCollection events)
        {
            if (events.MidiFileType == 0 && events.Tracks > 1)
                throw new ArgumentException("Can't export more than one track to a type 0 file");
            using (var writer = new BinaryWriter(File.Create(filename)))
            {
                writer.Write(Encoding.UTF8.GetBytes("MThd"));
                writer.Write(SwapUInt32(6)); // chunk size
                writer.Write(SwapUInt16((ushort)events.MidiFileType));
                writer.Write(SwapUInt16((ushort)events.Tracks));
                writer.Write(SwapUInt16((ushort)events.DeltaTicksPerQuarterNote));

                for (var track = 0; track < events.Tracks; track++)
                {
                    var eventList = events[track];

                    writer.Write(Encoding.UTF8.GetBytes("MTrk"));
                    var trackSizePosition = writer.BaseStream.Position;
                    writer.Write(SwapUInt32(0));

                    var absoluteTime = events.StartAbsoluteTime;

                    // use a stable sort to preserve ordering of MIDI events whose 
                    // absolute times are the same
                    MergeSort.Sort(eventList, new MidiEventComparer());
                    if (eventList.Count > 0)
                        // TBN Change - error if no end track
                        if (!MidiEvent.IsEndTrack(eventList[eventList.Count - 1]))
                            Debug.Log(
                                $"Exporting track {track} with a missing end track, tick {eventList[eventList.Count - 1].AbsoluteTime}");
                    foreach (var midiEvent in eventList) midiEvent.Export(ref absoluteTime, writer);

                    var trackChunkLength = (uint)(writer.BaseStream.Position - trackSizePosition) - 4;
                    writer.BaseStream.Position = trackSizePosition;
                    writer.Write(SwapUInt32(trackChunkLength));
                    writer.BaseStream.Position += trackChunkLength;
                }
            }
        }
    }
}