using System;
using System.Collections.Generic;
using System.Text;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components;

namespace Content.Server.GameObjects.Components
{
    [RegisterComponent]
    public class ListeningComponent : Component
    {
        public override string Name => "Listening";

        public void HeardSpeech(string speech)
        {
            Console.WriteLine($"Heard Speech. String given: {speech}");
        }

    }
}
