using System;
using Robust.Shared.GameObjects;

namespace Content.Client.Chat
{
    [RegisterComponent]
    public class TypingIndicatorComponent : Component
    {
        public override string Name => "TypingIndicator";
        public TimeSpan TimeAtTyping { get; set; }
    }
}
