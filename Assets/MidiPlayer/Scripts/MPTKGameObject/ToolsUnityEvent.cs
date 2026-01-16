using System;
using System.Collections.Generic;
using UnityEngine.Events;

namespace MidiPlayerTK
{
    [Serializable]
    public class EventMidiClass : UnityEvent<MPTKEvent>
    {
    }

    [Serializable]
    public class EventNotesMidiClass : UnityEvent<List<MPTKEvent>>
    {
    }

    [Serializable]
    public class EventSynthClass : UnityEvent<string>
    {
    }

    [Serializable]
    public class EventStartMidiClass : UnityEvent<string>
    {
    }

    [Serializable]
    public class EventEndMidiClass : UnityEvent<string, EventEndMidiEnum>
    {
    }

    [Serializable]
    public static class ToolsUnityEvent
    {
        public static bool HasPersistantEvent(this EventMidiClass evt)
        {
            if (evt != null && evt.GetPersistentEventCount() > 0 &&
                !string.IsNullOrEmpty(evt.GetPersistentMethodName(0)))
                return true;
            return false;
        }

        public static bool HasPersistantEvent(this UnityEvent evt)
        {
            if (evt != null && evt.GetPersistentEventCount() > 0 &&
                !string.IsNullOrEmpty(evt.GetPersistentMethodName(0)))
                return true;
            return false;
        }

        public static bool HasPersistantEvent(this EventNotesMidiClass evt)
        {
            if (evt != null && evt.GetPersistentEventCount() > 0 &&
                !string.IsNullOrEmpty(evt.GetPersistentMethodName(0)))
                return true;
            return false;
        }

        public static bool HasPersistantEvent(this EventStartMidiClass evt)
        {
            if (evt != null && evt.GetPersistentEventCount() > 0 &&
                !string.IsNullOrEmpty(evt.GetPersistentMethodName(0)))
                return true;
            return false;
        }

        public static bool HasPersistantEvent(this EventEndMidiClass evt)
        {
            if (evt != null && evt.GetPersistentEventCount() > 0 &&
                !string.IsNullOrEmpty(evt.GetPersistentMethodName(0)))
                return true;
            return false;
        }

        public static bool HasPersistantEvent(this EventSynthClass evt)
        {
            if (evt != null && evt.GetPersistentEventCount() > 0 &&
                !string.IsNullOrEmpty(evt.GetPersistentMethodName(0)))
                return true;
            return false;
        }
    }
}