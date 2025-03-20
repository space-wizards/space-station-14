using Content.Shared.FixedPoint;
using Robust.Shared.Map;

namespace Content.Server.Chemistry.Components
{
    [RegisterComponent]
    public sealed partial class VaporComponent : Component
    {
        public const string SolutionName = "vapor";

        /// <summary>
        /// Stores data on the previously reacted tile. We only want to do reaction checks once per tile.
        /// </summary>
        public TileRef PreviousTileRef;

        /// <summary>
        /// Percentage of the solution that is reacted with the TileReaction.
        /// Ex: 0.5 = 50% reacts.
        /// </summary>
        [DataField]
        public float TransferAmountPercentage;

        [DataField]
        public bool Active;
    }
}
