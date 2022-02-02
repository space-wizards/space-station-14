using System;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Shared.Weapons.Ranged
{
    /// <summary>
    /// This is sent if the MagazineBarrel AutoEjects the magazine
    /// </summary>
    [Serializable, NetSerializable]
    public sealed class MagazineAutoEjectEvent : EntityEventArgs
    {
        public EntityUid Uid;
    }
}
