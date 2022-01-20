using Content.Shared.Hands.Components;
using Robust.Shared.Analyzers;
using Robust.Shared.GameObjects;
using System.Collections.Generic;

namespace Content.Client.Hands
{
    [RegisterComponent]
    [ComponentReference(typeof(SharedHandsComponent))]
    [Friend(typeof(HandsSystem))]
    public class HandsComponent : SharedHandsComponent
    {
        public HandsGui? Gui { get; set; }

        /// <summary>
        ///     Data about the current sprite layers that the hand is contributing to the owner entity. Used for sprite in-hands.
        /// </summary>
        public readonly Dictionary<HandLocation, HashSet<string>> RevealedLayers = new();
    }
}
