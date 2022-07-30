using Content.Client.Hands.Systems;
using Content.Client.Hands.UI;
using Content.Shared.Hands.Components;

namespace Content.Client.Hands
{
    [RegisterComponent]
    [ComponentReference(typeof(SharedHandsComponent))]
    [Access(typeof(HandsSystem))]
    public sealed class HandsComponent : SharedHandsComponent
    {
        /// <summary>
        ///     Whether or not to add in-hand sprites for held items. Some entities (e.g., drones) don't want these.
        /// </summary>
        [DataField("showInHands")]
        public bool ShowInHands = true;

        /// <summary>
        ///     Data about the current sprite layers that the hand is contributing to the owner entity. Used for sprite in-hands.
        /// </summary>
        public readonly Dictionary<HandLocation, HashSet<string>> RevealedLayers = new();
    }
}
