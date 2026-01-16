#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;

namespace MidiPlayerTK
{
    public class MessagesEditor
    {
        private readonly List<Message> Messages;

        public MessagesEditor()
        {
            Messages = new List<Message>();
        }

        public void Add(string Text, MessageType Type = MessageType.Info, int LenghtMs = 5000)
        {
            Messages.Add(new Message { Text = Text, Start = DateTime.Now, Type = Type, LenghtMs = LenghtMs });
        }

        public void Display()
        {
            for (var i = 0; i < Messages.Count;)
            {
                EditorGUILayout.HelpBox(Messages[i].Text, Messages[i].Type, true);
                if (Messages[i].Start.AddMilliseconds(Messages[i].LenghtMs) < DateTime.Now)
                    Messages.RemoveAt(i);
                else
                    i++;
            }
        }

        public class Message
        {
            public int LenghtMs;
            public DateTime Start;
            public string Text;
            public MessageType Type;
        }
    }
}
#endif