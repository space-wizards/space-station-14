using System;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Shared.GameObjects.Components.Weapons.Ranged
{
    [Serializable, NetSerializable]
    public class BallisticMagazineWeaponComponentState : ComponentState
    {
        /// <summary>
        ///     True if a bullet is chambered.
        /// </summary>
        public bool Chambered { get; }

        /// <summary>
        ///     Count of bullets in the magazine.
        /// </summary>
        /// <remarks>
        ///     Null if no magazine is inserted.
        /// </remarks>
        public (int count, int max)? MagazineCount { get; }

        public BallisticMagazineWeaponComponentState(bool chambered, (int count, int max)? magazineCount) : base(ContentNetIDs.BALLISTIC_MAGAZINE_WEAPON)
        {
            Chambered = chambered;
            MagazineCount = magazineCount;
        }
    }

    // BMW is "Ballistic Magazine Weapon" here.
    /// <summary>
    ///     Fired server -> client when the magazine in a Ballistic Magazine Weapon got auto-ejected.
    /// </summary>
    [Serializable, NetSerializable]
    public sealed class BmwComponentAutoEjectedMessage : ComponentMessage
    {

    }
}
