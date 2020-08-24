using Microsoft.CodeAnalysis.Operations;
using Microsoft.EntityFrameworkCore.Internal;
using Robust.Shared.GameObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Content.Server.GameObjects.Components.Mobs.Speech
{
    [RegisterComponent]
    public class SpanishAccentComponent : Component, IAccentComponent
    {
        public override string Name => "SpanishAccent";

        public string Accentuate(string message)
        {
            // Insert E before every S
            message = InsertS(message);
            // Insert
            message = ReplaceQuestionMark(message);
            return message;
        }

        private static string InsertS(string message)
        {
            // Find a sentence that begins with S and add a
            var msg = message; // We make a working copy of the message
            var offset = 0; // As we may insert some additional chars, we need to offset the index accordingly
            for (var i = 0; i < message.Length; i++)
            {
                var c = message[i];
                var newWord = false;
                if (c == ' ')
                {
                    newWord = true;
                    i++;
                    if (i >= message.Length)
                        break;
                }

                // We are (most likely) at the beginning of a new word
                if (newWord || i == 0)
                {
                    c = message[i];
                    if (c == 's' || c == 'S')
                    {
                        // Remove the s
                        msg = msg.Remove(i + offset, 1);
                        // Insert the es/Es
                        msg = msg.Insert(i + offset, c == 's' ? "es" : "Es");
                        offset++;
                    }
                }
            }
            return msg;
        }

        private static string ReplaceQuestionMark(string message)
        {
            var msg = message;
            var offset = 0;
            for (var i = 0; i < message.Length; i++)
            {
                var c = message[i];
                if (c == '?')
                {
                    var index = FindSentenceStart(message, i);
                    if (index != i)
                    {
                        msg = msg.Insert(index + offset, "¿");
                        offset++;
                    }
                }
            }
            return msg;
        }
        private static int FindSentenceStart(string message, int ending)
        {
            for (var i = ending - 1; i >= 0; i--)
            {
                var c = message[i];
                if (c == '.' || c == ';' || c == '!' || c == '?')
                {
                    do
                    {
                        i++;
                    } while (message[i] == ' ');
                    return i;
                }
            }
            return 0;
        }
    }
}
