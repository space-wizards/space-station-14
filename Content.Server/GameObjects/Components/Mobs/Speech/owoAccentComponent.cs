using Robust.Shared.GameObjects;
using System;
using System.Collections.Generic;
using System.Text;

namespace Content.Server.GameObjects.Components.Mobs.Speech
{
    [RegisterComponent]
    public class OwOAccentComponent : Component, IAccentComponent
    {
        public override string Name => "OwOAccent";

        public string Accentuate(string message)
        {
            var arr = message.ToCharArray();
            Array.Reverse(arr);
            return new string(arr);
        }
    }
}
