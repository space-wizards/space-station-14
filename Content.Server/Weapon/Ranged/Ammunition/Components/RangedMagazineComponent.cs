using System.Collections.Generic;
using Content.Server.Weapon.Ranged.Barrels.Components;
using Content.Shared.Popups;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Weapon.Ranged.Ammunition.Components
{
    [RegisterComponent]
    public sealed class RangedMagazineComponent : Component
    {
        public readonly Stack<EntityUid> SpawnedAmmo = new();
        public Container AmmoContainer = default!;

        public int ShotsLeft => SpawnedAmmo.Count + UnspawnedCount;
        public int Capacity => _capacity;
        [DataField("capacity")]
        private int _capacity = 20;

        public MagazineType MagazineType => _magazineType;
        [DataField("magazineType")]
        private MagazineType _magazineType = MagazineType.Unspecified;
        public BallisticCaliber Caliber => _caliber;
        [DataField("caliber")]
        private BallisticCaliber _caliber = BallisticCaliber.Unspecified;

        // If there's anything already in the magazine
        [DataField("fillPrototype", customTypeSerializer:typeof(PrototypeIdSerializer<EntityPrototype>))]
        public string? FillPrototype;

        // By default the magazine won't spawn the entity until needed so we need to keep track of how many left we can spawn
        // Generally you probablt don't want to use this
        public int UnspawnedCount;
    }
}
