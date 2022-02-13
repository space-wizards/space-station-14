using System.Collections.Generic;
using Robust.Shared.Analyzers;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Weapon.Ranged.Ammunition.Components
{
    /// <summary>
    /// Stores ammo and can quickly transfer ammo into a magazine.
    /// </summary>
    [RegisterComponent]
    [Friend(typeof(GunSystem))]
    public sealed class AmmoBoxComponent : Component
    {
        [DataField("caliber")]
        public BallisticCaliber Caliber = BallisticCaliber.Unspecified;

        [DataField("capacity")]
        public int Capacity
        {
            get => _capacity;
            set
            {
                _capacity = value;
                SpawnedAmmo = new Stack<EntityUid>(value);
            }
        }

        private int _capacity = 30;

        public int AmmoLeft => SpawnedAmmo.Count + UnspawnedCount;
        public Stack<EntityUid> SpawnedAmmo = new();

        /// <summary>
        /// Container that holds any instantiated ammo.
        /// </summary>
        public Container AmmoContainer = default!;

        /// <summary>
        /// How many more deferred entities can be spawned. We defer these to avoid instantiating the entities until needed for performance reasons.
        /// </summary>
        public int UnspawnedCount;

        /// <summary>
        /// The prototype of the ammo to be retrieved when getting ammo.
        /// </summary>
        [DataField("fillPrototype", customTypeSerializer:typeof(PrototypeIdSerializer<EntityPrototype>))]
        public string? FillPrototype;
    }
}
