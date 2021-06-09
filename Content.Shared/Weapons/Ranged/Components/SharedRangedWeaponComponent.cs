#nullable enable
using System;
using Content.Shared.NetIDs;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Serialization;

namespace Content.Shared.Weapons.Ranged.Components
{
    public abstract class SharedRangedWeaponComponent : Component
    {
        // Each RangedWeapon should have a RangedWeapon component +
        // some kind of RangedBarrelComponent (this dictates what ammo is retrieved).
        public override string Name => "RangedWeapon";
        public override uint? NetID => ContentNetIDs.RANGED_WEAPON;
    }

    [Serializable, NetSerializable]
    public sealed class RangedWeaponComponentState : ComponentState
    {
        public FireRateSelector FireRateSelector { get; }

        public RangedWeaponComponentState(
            FireRateSelector fireRateSelector
            ) : base(ContentNetIDs.RANGED_WEAPON)
        {
            FireRateSelector = fireRateSelector;
        }
    }

    /// <summary>
    /// A component message raised when the weapon is fired at a position on the map.
    /// </summary>
    [Serializable, NetSerializable]
    public sealed class FirePosComponentMessage : ComponentMessage
    {
        /// <summary>
        /// If this is not invalid, the target position is relative to the grid.
        /// Otherwise, it is a map position.
        /// </summary>
        public GridId TargetGrid { get; }

        /// <summary>
        /// If Target Grid is not invalid, this is relative to the grid, otherwise
        /// it is a map position.
        /// </summary>
        public Vector2 TargetPosition { get; }

        /// <summary>
        /// Constructs a new instance of <see cref="FirePosComponentMessage"/>.
        /// </summary>
        /// <param name="targetGrid">The grid that the target position is on, if any.</param>
        /// <param name="targetPosition">Target position relative to the grid, or a map position if the grid is invalid.</param>
        public FirePosComponentMessage(GridId targetGrid, Vector2 targetPosition)
        {
            TargetGrid = targetGrid;
            TargetPosition = targetPosition;
        }
    }
}
