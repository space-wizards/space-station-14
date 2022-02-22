using System.Collections.Generic;
using Content.Server.Weapon.Ranged.Ammunition.Components;
using Content.Shared.Sound;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.ViewVariables;

namespace Content.Server.Weapon.Ranged.Barrels.Components
{
    /// <summary>
    /// Bolt-action rifles
    /// </summary>
    [RegisterComponent, NetworkedComponent, ComponentReference(typeof(ServerRangedBarrelComponent))]
    public sealed class PumpBarrelComponent : ServerRangedBarrelComponent, ISerializationHooks
    {
        public override int ShotsLeft
        {
            get
            {
                var chamberCount = ChamberContainer.ContainedEntity != null ? 1 : 0;
                return chamberCount + SpawnedAmmo.Count + UnspawnedCount;
            }
        }

        private const int DefaultCapacity = 6;
        [DataField("capacity")]
        public override int Capacity { get; } = DefaultCapacity;

        // Even a point having a chamber? I guess it makes some of the below code cleaner
        public ContainerSlot ChamberContainer = default!;
        public Stack<EntityUid> SpawnedAmmo = new(DefaultCapacity - 1);
        public Container AmmoContainer = default!;

        [ViewVariables]
        [DataField("caliber")]
        public BallisticCaliber Caliber = BallisticCaliber.Unspecified;

        [ViewVariables]
        [DataField("fillPrototype", customTypeSerializer:typeof(PrototypeIdSerializer<EntityPrototype>))]
        public string? FillPrototype;

        [ViewVariables] public int UnspawnedCount;

        [DataField("manualCycle")] public bool ManualCycle = true;

        // Sounds
        [DataField("soundCycle")] public SoundSpecifier SoundCycle = new SoundPathSpecifier("/Audio/Weapons/Guns/Cock/sf_rifle_cock.ogg");

        [DataField("soundInsert")] public SoundSpecifier SoundInsert = new SoundPathSpecifier("/Audio/Weapons/Guns/MagIn/bullet_insert.ogg");

        void ISerializationHooks.AfterDeserialization()
        {
            SpawnedAmmo = new Stack<EntityUid>(Capacity - 1);
        }
    }
}
