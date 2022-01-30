using System.Collections.Generic;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Weapon.Ranged.Ammunition.Components
{
    /// <summary>
    /// Used to load certain ranged weapons quickly
    /// </summary>
    [RegisterComponent, ComponentProtoName("SpeedLoader")]
    public class SpeedLoaderComponent : Component
    {
        [DataField("caliber")] public BallisticCaliber Caliber = BallisticCaliber.Unspecified;
        public int Capacity => _capacity;
        [DataField("capacity")]
        private int _capacity = 6;

        public Container AmmoContainer = default!;
        public Stack<EntityUid> SpawnedAmmo = new();
        public int UnspawnedCount;

        public int AmmoLeft => SpawnedAmmo.Count + UnspawnedCount;

        [DataField("fillPrototype", customTypeSerializer:typeof(PrototypeIdSerializer<EntityPrototype>))]
        public string? FillPrototype;
    }
}
