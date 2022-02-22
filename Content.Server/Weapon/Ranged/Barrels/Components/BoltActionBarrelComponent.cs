using System.Collections.Generic;
using Content.Server.Weapon.Ranged.Ammunition.Components;
using Content.Shared.Sound;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.ViewVariables;

namespace Content.Server.Weapon.Ranged.Barrels.Components
{
    /// <summary>
    /// Shotguns mostly
    /// </summary>
    [RegisterComponent, NetworkedComponent, ComponentReference(typeof(ServerRangedBarrelComponent))]
    public sealed class BoltActionBarrelComponent : ServerRangedBarrelComponent
    {
        // Originally I had this logic shared with PumpBarrel and used a couple of variables to control things
        // but it felt a lot messier to play around with, especially when adding verbs

        public override int ShotsLeft
        {
            get
            {
                var chamberCount = ChamberContainer.ContainedEntity != null ? 1 : 0;
                return chamberCount + SpawnedAmmo.Count + UnspawnedCount;
            }
        }
        public override int Capacity => _capacity;

        [DataField("capacity")]
        internal int _capacity = 6;

        public ContainerSlot ChamberContainer = default!;
        public Stack<EntityUid> SpawnedAmmo = default!;
        public Container AmmoContainer = default!;

        [ViewVariables]
        [DataField("caliber")]
        public BallisticCaliber Caliber = BallisticCaliber.Unspecified;

        [ViewVariables]
        [DataField("fillPrototype", customTypeSerializer:typeof(PrototypeIdSerializer<EntityPrototype>))]
        public string? FillPrototype;

        [ViewVariables]
        public int UnspawnedCount;

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
                    SoundSystem.Play(Filter.Pvs(Owner), _soundBoltOpen.GetSound(), Owner, AudioParams.Default.WithVolume(-2));
                }
                else
                {
                    gunSystem.TryFeedChamber(this);
                    SoundSystem.Play(Filter.Pvs(Owner), _soundBoltClosed.GetSound(), Owner, AudioParams.Default.WithVolume(-2));
                }

                _boltOpen = value;
                gunSystem.UpdateBoltAppearance(this);
                Dirty();
            }
        }
        private bool _boltOpen;

        [DataField("autoCycle")] public bool AutoCycle;

        // Sounds
        [DataField("soundCycle")] public SoundSpecifier SoundCycle = new SoundPathSpecifier("/Audio/Weapons/Guns/Cock/sf_rifle_cock.ogg");
        [DataField("soundBoltOpen")]
        private SoundSpecifier _soundBoltOpen = new SoundPathSpecifier("/Audio/Weapons/Guns/Bolt/rifle_bolt_open.ogg");
        [DataField("soundBoltClosed")]
        private SoundSpecifier _soundBoltClosed = new SoundPathSpecifier("/Audio/Weapons/Guns/Bolt/rifle_bolt_closed.ogg");
        [DataField("soundInsert")] public SoundSpecifier SoundInsert = new SoundPathSpecifier("/Audio/Weapons/Guns/MagIn/bullet_insert.ogg");
    }
}
