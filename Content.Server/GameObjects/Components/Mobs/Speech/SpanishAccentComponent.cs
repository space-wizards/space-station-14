using Microsoft.CodeAnalysis.Operations;
using Microsoft.EntityFrameworkCore.Internal;
using Robust.Shared.GameObjects;
using System;
using System.Collections.Generic;
using System.Text;

namespace Content.Server.GameObjects.Components.Mobs.Speech
{
    [RegisterComponent]
    public class SpanishAccentComponent : Component, IAccentComponent
    {
        public override string Name => "SpanishAccent";

        public string Accentuate(string message)
        {
            var msg = message;
            for (var i = 0; i < message.Length; i++)
            {
                var c = message[i];
                if (c == '?')
                {
                    var index = FindSentenceStart(message, i);
                    if (index != i)
                        msg = msg.Insert(index, "¿");
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
