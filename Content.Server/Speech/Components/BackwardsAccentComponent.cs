using System;
using Robust.Shared.GameObjects;

namespace Content.Server.Speech.Components
{
    [RegisterComponent]
    public class BackwardsAccentComponent : Component, IAccentComponent
    {
        public override string Name => "BackwardsAccent";

        public string Accentuate(string message)
        {
            var arr = message.ToCharArray();
            Array.Reverse(arr);
            return new string(arr);
        }
    }
}
