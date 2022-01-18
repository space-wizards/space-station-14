using System;
using Content.Shared.Damage;
using Content.Shared.Sound;
using Content.Shared.Weapons.Ranged.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Timing;
using Robust.Shared.ViewVariables;

namespace Content.Server.Weapon.Ranged
{
    [RegisterComponent]
    public sealed class ServerRangedWeaponComponent : SharedRangedWeaponComponent
    {
        private TimeSpan _lastFireTime;

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("clumsyCheck")]
        public bool ClumsyCheck { get; set; } = true;

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("clumsyExplodeChance")]
        public float ClumsyExplodeChance { get; set; } = 0.5f;

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("canHotspot")]
        private bool _canHotspot = true;

        [DataField("clumsyWeaponHandlingSound")]
        private SoundSpecifier _clumsyWeaponHandlingSound = new SoundPathSpecifier("/Audio/Items/bikehorn.ogg");

        [DataField("clumsyWeaponShotSound")]
        private SoundSpecifier _clumsyWeaponShotSound = new SoundPathSpecifier("/Audio/Weapons/Guns/Gunshots/bang.ogg");

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("clumsyDamage")]
        public DamageSpecifier? ClumsyDamage;
    }
}
