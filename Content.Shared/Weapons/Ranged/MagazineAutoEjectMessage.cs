using System;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Shared.Weapons.Ranged
{
    /// <summary>
    /// This is sent if the MagazineBarrel AutoEjects the magazine
    /// </summary>
    [Serializable, NetSerializable]
#pragma warning disable 618
    public sealed class MagazineAutoEjectMessage : ComponentMessage {}
#pragma warning restore 618
}
