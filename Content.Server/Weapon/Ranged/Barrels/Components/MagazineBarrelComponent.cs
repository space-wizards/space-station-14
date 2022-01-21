using System;
using Content.Server.Weapon.Ranged.Ammunition.Components;
using Content.Shared.Sound;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.IoC;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.ViewVariables;

namespace Content.Server.Weapon.Ranged.Barrels.Components
{
    [RegisterComponent, NetworkedComponent, ComponentProtoName("MagazineBarrel"), ComponentReference(typeof(ServerRangedBarrelComponent))]
    public sealed class MagazineBarrelComponent : ServerRangedBarrelComponent
    {
        [Dependency] private readonly IEntityManager _entities = default!;

        [ViewVariables] public ContainerSlot ChamberContainer = default!;
        [ViewVariables] public bool HasMagazine => MagazineContainer.ContainedEntity != null;
        public ContainerSlot MagazineContainer = default!;

        [ViewVariables] public MagazineType MagazineTypes => _magazineTypes;
        [DataField("magazineTypes")]
        private MagazineType _magazineTypes = default;
        [ViewVariables] public BallisticCaliber Caliber => _caliber;
        [DataField("caliber")]
        private BallisticCaliber _caliber = BallisticCaliber.Unspecified;

        public override int ShotsLeft
        {
            get
            {
                var count = 0;
                if (ChamberContainer.ContainedEntity != null)
                {
                    count++;
                }

                if (MagazineContainer.ContainedEntity is {Valid: true} magazine)
                {
                    count += _entities.GetComponent<RangedMagazineComponent>(magazine).ShotsLeft;
                }

                return count;
            }
        }

        public override int Capacity
        {
            get
            {
                // Chamber
                var count = 1;
                if (MagazineContainer.ContainedEntity is {Valid: true} magazine)
                {
                    count += _entities.GetComponent<RangedMagazineComponent>(magazine).Capacity;
                }

                return count;
            }
        }

        [DataField("magFillPrototype", customTypeSerializer:typeof(PrototypeIdSerializer<EntityPrototype>))]
        public string? MagFillPrototype;

        public bool BoltOpen
        {
            get => _boltOpen;
            set
            {
                if (_boltOpen == value)
                {
                    return;
                }

                var gunSystem = EntitySystem.Get<GunSystem>();

                if (value)
                {
                    gunSystem.TryEjectChamber(this);
                    SoundSystem.Play(Filter.Pvs(Owner), SoundBoltOpen.GetSound(), Owner, AudioParams.Default.WithVolume(-2));
                }
                else
                {
                    gunSystem.TryFeedChamber(this);
                    SoundSystem.Play(Filter.Pvs(Owner), SoundBoltClosed.GetSound(), Owner, AudioParams.Default.WithVolume(-2));
                }

                _boltOpen = value;
                gunSystem.UpdateMagazineAppearance(this);
                Dirty(_entities);
            }
        }
        private bool _boltOpen = true;

        [DataField("autoEjectMag")] public bool AutoEjectMag;
        // If the bolt needs to be open before we can insert / remove the mag (i.e. for LMGs)
        public bool MagNeedsOpenBolt => _magNeedsOpenBolt;
        [DataField("magNeedsOpenBolt")]
        private bool _magNeedsOpenBolt = default;

        // Sounds
        [DataField("soundBoltOpen", required: true)]
        public SoundSpecifier SoundBoltOpen = default!;
        [DataField("soundBoltClosed", required: true)]
        public SoundSpecifier SoundBoltClosed = default!;
        [DataField("soundRack", required: true)]
        public SoundSpecifier SoundRack = default!;
        [DataField("soundMagInsert", required: true)]
        public SoundSpecifier SoundMagInsert = default!;
        [DataField("soundMagEject", required: true)]
        public SoundSpecifier SoundMagEject = default!;
        [DataField("soundAutoEject")] public SoundSpecifier SoundAutoEject = new SoundPathSpecifier("/Audio/Weapons/Guns/EmptyAlarm/smg_empty_alarm.ogg");
    }

    [Flags]
    public enum MagazineType
    {
        Unspecified = 0,
        LPistol = 1 << 0, // Placeholder?
        Pistol = 1 << 1,
        HCPistol = 1 << 2,
        Smg = 1 << 3,
        SmgTopMounted = 1 << 4,
        Rifle = 1 << 5,
        IH = 1 << 6, // Placeholder?
        Box = 1 << 7,
        Pan = 1 << 8,
        Dart = 1 << 9, // Placeholder
        CalicoTopMounted = 1 << 10,
    }
}
