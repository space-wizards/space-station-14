using System;
using Content.Server.Weapon.Ranged.Ammunition.Components;
using Content.Shared.Sound;
using Content.Shared.Weapons.Ranged.Barrels.Components;
using Robust.Shared.Analyzers;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.ViewVariables;

namespace Content.Server.Weapon.Ranged.Barrels.Components
{
    [RegisterComponent, NetworkedComponent, ComponentReference(typeof(ServerRangedBarrelComponent))]
    public sealed class RevolverBarrelComponent : ServerRangedBarrelComponent, ISerializationHooks
    {
        [ViewVariables]
        [DataField("caliber")]
        public BallisticCaliber Caliber = BallisticCaliber.Unspecified;

        public Container AmmoContainer = default!;

        [ViewVariables]
        public int CurrentSlot;

        public override int Capacity => AmmoSlots.Length;

        [DataField("capacity")]
        private int _serializedCapacity = 6;

        [DataField("ammoSlots", readOnly: true)]
        public EntityUid?[] AmmoSlots = Array.Empty<EntityUid?>();

        public override int ShotsLeft => AmmoContainer.ContainedEntities.Count;

        [ViewVariables]
        [DataField("fillPrototype", customTypeSerializer:typeof(PrototypeIdSerializer<EntityPrototype>))]
        public string? FillPrototype;

        [ViewVariables]
        public int UnspawnedCount;

        // Sounds
        [DataField("soundEject")]
        public SoundSpecifier SoundEject = new SoundPathSpecifier("/Audio/Weapons/Guns/MagOut/revolver_magout.ogg");

        [DataField("soundInsert")]
        public SoundSpecifier SoundInsert = new SoundPathSpecifier("/Audio/Weapons/Guns/MagIn/revolver_magin.ogg");

        [DataField("soundSpin")]
        public SoundSpecifier SoundSpin = new SoundPathSpecifier("/Audio/Weapons/Guns/Misc/revolver_spin.ogg");

        void ISerializationHooks.BeforeSerialization()
        {
            _serializedCapacity = AmmoSlots.Length;
        }

        void ISerializationHooks.AfterDeserialization()
        {
            AmmoSlots = new EntityUid?[_serializedCapacity];
        }
    }
}
