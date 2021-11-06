using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Robust.Shared.GameObjects;

namespace Content.Client.Chat
{
    [RegisterComponent]
    public class TypingIndicatorComponent : Component
    {
        public override string Name => "TypingIndicator";
        public bool Enabled { get; set; }
    }
}
