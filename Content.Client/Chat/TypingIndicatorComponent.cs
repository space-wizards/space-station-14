using System;
using Robust.Shared.GameObjects;

namespace Content.Client.Chat
{
    /// <summary>
    /// Client-side component that each client will ensure onto
    /// all other player characters.
    /// The use of a typing indicator is optional, decided per user.
    /// However, each client must render them if they are asked to.
    /// </summary>
    [RegisterComponent]
    public class TypingIndicatorComponent : Component
    {
        public override string Name => "TypingIndicator";
        public bool IsVisible { get; set; }
    }
}
