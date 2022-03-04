using Content.Shared.Hands.Components;
using Robust.Client.Graphics;

namespace Content.Client.Hands
{
    [RegisterComponent]
    [ComponentReference(typeof(SharedHandsComponent))]
    [Friend(typeof(HandsSystem))]
    public sealed class HandsComponent : SharedHandsComponent
    {
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
                    { RSI.State.Direction.East, -1000 }, // the hand is on the side facing away from the user
                    { RSI.State.Direction.North, -1000 } // behind hair and large clothing while looking north
                }
            },
            {
                HandLocation.Right,
                new()
                {
                    { RSI.State.Direction.West, -1000 }, // the hand is on the side facing away from the user
                    { RSI.State.Direction.North, -1000 } // behind hair and large clothing while looking north
                }
            },
            { HandLocation.Middle, new()
                {
                    { RSI.State.Direction.North, -1000 },
                    { RSI.State.Direction.East, -1 }, // behind the right-hand, but still on top of the rest of the player/clothing layers.
                    { RSI.State.Direction.West, -1 },
                }
            },
        };
    }
}
