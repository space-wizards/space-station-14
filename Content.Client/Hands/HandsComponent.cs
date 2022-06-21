using Content.Shared.Hands.Components;
using Robust.Client.Graphics;


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

        public HandsGui? Gui { get; set; }

        /// <summary>
        ///     Data about the current sprite layers that the hand is contributing to the owner entity. Used for sprite in-hands.
        /// </summary>
        public readonly Dictionary<HandLocation, HashSet<string>> RevealedLayers = new();

        /// <summary>
        ///     This dictionary specifies the direction and hand-location dependent in-hand layer draw depths.
        /// </summary>
        /// <remarks>
        ///     Used to ensure that the left/right in hands are drawn in the correct order when a user is facing left or right.
        /// </remarks>
        [DataField("inHandDrawDepths")]
        public readonly Dictionary<HandLocation, Dictionary<RSI.State.Direction, int>> HandLayerDrawDepth = new()
        {
            { HandLocation.Left, new()
                {
                    { RSI.State.Direction.East, -1000 }, // the hand is on the side facing away from the user. This should probably be the lowest layer.
                    { RSI.State.Direction.North, -1000 } // the owner is facing northwards, so their arms, legs, hair, etc should all block the view of the item.
                }
            },
            { HandLocation.Right, new()
                {
                    { RSI.State.Direction.West, -1000 },
                    { RSI.State.Direction.North, -1000 }
                }
            },
            { HandLocation.Middle, new() { { RSI.State.Direction.North, -1000 } } }
        };

        [DataField("defaultDrawDepth")]
        public readonly int DefaultDrawDepth = -1;
    }
}
