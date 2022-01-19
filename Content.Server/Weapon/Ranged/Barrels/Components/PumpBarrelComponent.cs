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
    [RegisterComponent, NetworkedComponent, ComponentProtoName("PumpBarrel")]
    public sealed class PumpBarrelComponent : ServerRangedBarrelComponent, ISerializationHooks
    {
        public override int ShotsLeft
        {
            get
            {
                var chamberCount = _chamberContainer.ContainedEntity != null ? 1 : 0;
                return chamberCount + _spawnedAmmo.Count + _unspawnedCount;
            }
        }

        private const int DefaultCapacity = 6;
        [DataField("capacity")]
        public override int Capacity { get; } = DefaultCapacity;

        // Even a point having a chamber? I guess it makes some of the below code cleaner
        private ContainerSlot _chamberContainer = default!;
        private Stack<EntityUid> _spawnedAmmo = new(DefaultCapacity - 1);
        private Container _ammoContainer = default!;

        [ViewVariables]
        [DataField("caliber")]
        public BallisticCaliber Caliber = BallisticCaliber.Unspecified;

        [ViewVariables]
        [DataField("fillPrototype", customTypeSerializer:typeof(PrototypeIdSerializer<EntityPrototype>))]
        private string? _fillPrototype;

        [ViewVariables]
        private int _unspawnedCount;

        [DataField("manualCycle")]
        private bool _manualCycle = true;

        // Sounds
        [DataField("soundCycle")]
        private SoundSpecifier _soundCycle = new SoundPathSpecifier("/Audio/Weapons/Guns/Cock/sf_rifle_cock.ogg");

        [DataField("soundInsert")]
        private SoundSpecifier _soundInsert = new SoundPathSpecifier("/Audio/Weapons/Guns/MagIn/bullet_insert.ogg");

        void ISerializationHooks.AfterDeserialization()
        {
            _spawnedAmmo = new Stack<EntityUid>(Capacity - 1);
        }
    }
}
