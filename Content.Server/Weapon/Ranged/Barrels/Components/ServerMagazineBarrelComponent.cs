using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Content.Server.Hands.Components;
using Content.Server.Items;
using Content.Server.Weapon.Ranged.Ammunition.Components;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Sound;
using Content.Shared.Weapons.Ranged;
using Content.Shared.Weapons.Ranged.Barrels.Components;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;

namespace Content.Server.Weapon.Ranged.Barrels.Components
{
    [RegisterComponent]
    [NetworkedComponent()]
#pragma warning disable 618
    public sealed class ServerMagazineBarrelComponent : ServerRangedBarrelComponent, IUse, IInteractUsing, IExamine
#pragma warning restore 618
    {
        [Dependency] private readonly IEntityManager _entities = default!;

        public override string Name => "MagazineBarrel";

        [ViewVariables]
        private ContainerSlot _chamberContainer = default!;
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
                if (_chamberContainer.ContainedEntity != null)
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
        private string? _magFillPrototype;

        public bool BoltOpen
        {
            get => _boltOpen;
            set
            {
                if (_boltOpen == value)
                {
                    return;
                }

                if (value)
                {
                    TryEjectChamber();
                    SoundSystem.Play(Filter.Pvs(Owner), _soundBoltOpen.GetSound(), Owner, AudioParams.Default.WithVolume(-2));
                }
                else
                {
                    TryFeedChamber();
                    SoundSystem.Play(Filter.Pvs(Owner), _soundBoltClosed.GetSound(), Owner, AudioParams.Default.WithVolume(-2));
                }

                _boltOpen = value;
                UpdateAppearance();
                Dirty();
            }
        }
        private bool _boltOpen = true;

        [DataField("autoEjectMag")]
        private bool _autoEjectMag;
        // If the bolt needs to be open before we can insert / remove the mag (i.e. for LMGs)
        public bool MagNeedsOpenBolt => _magNeedsOpenBolt;
        [DataField("magNeedsOpenBolt")]
        private bool _magNeedsOpenBolt = default;

        private AppearanceComponent? _appearanceComponent;

        // Sounds
        [DataField("soundBoltOpen", required: true)]
        private SoundSpecifier _soundBoltOpen = default!;
        [DataField("soundBoltClosed", required: true)]
        private SoundSpecifier _soundBoltClosed = default!;
        [DataField("soundRack", required: true)]
        private SoundSpecifier _soundRack = default!;
        [DataField("soundMagInsert", required: true)]
        private SoundSpecifier _soundMagInsert = default!;
        [DataField("soundMagEject", required: true)]
        private SoundSpecifier _soundMagEject = default!;
        [DataField("soundAutoEject")]
        private SoundSpecifier _soundAutoEject = new SoundPathSpecifier("/Audio/Weapons/Guns/EmptyAlarm/smg_empty_alarm.ogg");

        private List<MagazineType> GetMagazineTypes()
        {
            var types = new List<MagazineType>();

            foreach (MagazineType mag in Enum.GetValues(typeof(MagazineType)))
            {
                if ((_magazineTypes & mag) != 0)
                {
                    types.Add(mag);
                }
            }

            return types;
        }

        public override ComponentState GetComponentState()
        {
            (int, int)? count = null;
            if (MagazineContainer.ContainedEntity is {Valid: true} magazine &&
                _entities.TryGetComponent(magazine, out RangedMagazineComponent? rangedMagazineComponent))
            {
                count = (rangedMagazineComponent.ShotsLeft, rangedMagazineComponent.Capacity);
            }

            return new MagazineBarrelComponentState(
                _chamberContainer.ContainedEntity != null,
                FireRateSelector,
                count,
                SoundGunshot.GetSound());
        }

        protected override void Initialize()
        {
            base.Initialize();

            if (_entities.TryGetComponent(Owner, out AppearanceComponent? appearanceComponent))
            {
                _appearanceComponent = appearanceComponent;
            }

            _chamberContainer = Owner.EnsureContainer<ContainerSlot>($"{Name}-chamber");
            MagazineContainer = Owner.EnsureContainer<ContainerSlot>($"{Name}-magazine", out var existing);

            if (!existing && _magFillPrototype != null)
            {
                var magEntity = _entities.SpawnEntity(_magFillPrototype, _entities.GetComponent<TransformComponent>(Owner).Coordinates);
                MagazineContainer.Insert(magEntity);
            }
            Dirty();
        }

        protected override void Startup()
        {
            base.Startup();
            UpdateAppearance();
        }

        public override EntityUid? PeekAmmo()
        {
            return BoltOpen ? null : _chamberContainer.ContainedEntity;
        }

        public override EntityUid? TakeProjectile(EntityCoordinates spawnAt)
        {
            if (BoltOpen)
            {
                return null;
            }
            var entity = _chamberContainer.ContainedEntity;

            Cycle();

            return entity != null ? EntitySystem.Get<GunSystem>().TakeBullet(_entities.GetComponent<AmmoComponent>(entity.Value), spawnAt) : null;
        }

        private void Cycle(bool manual = false)
        {
            if (BoltOpen)
            {
                return;
            }

            TryEjectChamber();

            TryFeedChamber();

            if (_chamberContainer.ContainedEntity == null && !BoltOpen)
            {
                SoundSystem.Play(Filter.Pvs(Owner), _soundBoltOpen.GetSound(), Owner, AudioParams.Default.WithVolume(-5));

                if (Owner.TryGetContainer(out var container))
                {
                    Owner.PopupMessage(container.Owner, Loc.GetString("server-magazine-barrel-component-cycle-bolt-open"));
                }
                BoltOpen = true;
                return;
            }

            if (manual)
            {
                SoundSystem.Play(Filter.Pvs(Owner), _soundRack.GetSound(), Owner, AudioParams.Default.WithVolume(-2));
            }

            Dirty();
            UpdateAppearance();
        }

        private void UpdateAppearance()
        {
            _appearanceComponent?.SetData(BarrelBoltVisuals.BoltOpen, BoltOpen);
            _appearanceComponent?.SetData(MagazineBarrelVisuals.MagLoaded, MagazineContainer.ContainedEntity != null);
            _appearanceComponent?.SetData(AmmoVisuals.AmmoCount, ShotsLeft);
            _appearanceComponent?.SetData(AmmoVisuals.AmmoMax, Capacity);
        }

        public bool UseEntity(UseEntityEventArgs eventArgs)
        {
            // Behavior:
            // If bolt open just close it
            // If bolt closed then cycle
            //     If we cycle then get next round
            //         If no more round then open bolt

            if (BoltOpen)
            {
                SoundSystem.Play(Filter.Pvs(Owner), _soundBoltClosed.GetSound(), Owner, AudioParams.Default.WithVolume(-5));
                Owner.PopupMessage(eventArgs.User, Loc.GetString("server-magazine-barrel-component-use-entity-bolt-closed"));
                BoltOpen = false;
                return true;
            }

            // Could play a rack-slide specific sound here if you're so inclined (if the chamber is empty but rounds are available)

            Cycle(true);
            return true;
        }

        public bool TryEjectChamber()
        {
            if (_chamberContainer.ContainedEntity is {Valid: true} chamberEntity)
            {
                if (!_chamberContainer.Remove(chamberEntity))
                {
                    return false;
                }
                var ammoComponent = _entities.GetComponent<AmmoComponent>(chamberEntity);
                if (!ammoComponent.Caseless)
                {
                    EjectCasing(chamberEntity);
                }
                return true;
            }
            return false;
        }

        public bool TryFeedChamber()
        {
            if (_chamberContainer.ContainedEntity != null)
            {
                return false;
            }

            // Try and pull a round from the magazine to replace the chamber if possible
            var magazine = MagazineContainer.ContainedEntity;

            if (_entities.GetComponentOrNull<RangedMagazineComponent>(magazine)?.TakeAmmo() is not {Valid: true} nextRound)
            {
                return false;
            }

            _chamberContainer.Insert(nextRound);

            if (_autoEjectMag && magazine != null && _entities.GetComponent<RangedMagazineComponent>(magazine.Value).ShotsLeft == 0)
            {
                SoundSystem.Play(Filter.Pvs(Owner), _soundAutoEject.GetSound(), Owner, AudioParams.Default.WithVolume(-2));

                MagazineContainer.Remove(magazine.Value);
#pragma warning disable 618
                SendNetworkMessage(new MagazineAutoEjectMessage());
#pragma warning restore 618
            }
            return true;
        }

        public void RemoveMagazine(EntityUid user)
        {
            var mag = MagazineContainer.ContainedEntity;

            if (mag == null)
            {
                return;
            }

            if (MagNeedsOpenBolt && !BoltOpen)
            {
                Owner.PopupMessage(user, Loc.GetString("server-magazine-barrel-component-remove-magazine-bolt-closed"));
                return;
            }

            MagazineContainer.Remove(mag.Value);
            SoundSystem.Play(Filter.Pvs(Owner), _soundMagEject.GetSound(), Owner, AudioParams.Default.WithVolume(-2));

            if (_entities.TryGetComponent(user, out HandsComponent? handsComponent))
            {
                handsComponent.PutInHandOrDrop(_entities.GetComponent<ItemComponent>(mag.Value));
            }

            Dirty();
            UpdateAppearance();
        }

        public bool CanInsertMagazine(EntityUid user, EntityUid magazine, bool quiet = true)
        {
            if (!_entities.TryGetComponent(magazine, out RangedMagazineComponent? magazineComponent))
            {
                return false;
            }

            if ((MagazineTypes & magazineComponent.MagazineType) == 0)
            {
                if (!quiet)
                    Owner.PopupMessage(user, Loc.GetString("server-magazine-barrel-component-interact-using-wrong-magazine-type"));
                return false;
            }

            if (magazineComponent.Caliber != _caliber)
            {
                if (!quiet)
                    Owner.PopupMessage(user, Loc.GetString("server-magazine-barrel-component-interact-using-wrong-caliber"));
                return false;
            }

            if (_magNeedsOpenBolt && !BoltOpen)
            {
                if (!quiet)
                    Owner.PopupMessage(user, Loc.GetString("server-magazine-barrel-component-interact-using-bolt-closed"));
                return false;
            }

            if (MagazineContainer.ContainedEntity == null)
            {
                return true;
            }

            if (!quiet)
                Owner.PopupMessage(user, Loc.GetString("server-magazine-barrel-component-interact-using-already-holding-magazine"));
            return false;
        }

        public void InsertMagazine(EntityUid user, EntityUid magazine)
        {
            SoundSystem.Play(Filter.Pvs(Owner), _soundMagInsert.GetSound(), Owner, AudioParams.Default.WithVolume(-2));
            Owner.PopupMessage(user, Loc.GetString("server-magazine-barrel-component-interact-using-success"));
            MagazineContainer.Insert(magazine);
            Dirty();
            UpdateAppearance();
        }

        public async Task<bool> InteractUsing(InteractUsingEventArgs eventArgs)
        {
            if (CanInsertMagazine(eventArgs.User, eventArgs.Using, quiet: false))
            {
                InsertMagazine(eventArgs.User, eventArgs.Using);
                return true;
            }

            // Insert 1 ammo
            if (_entities.TryGetComponent(eventArgs.Using, out AmmoComponent? ammoComponent))
            {
                if (!BoltOpen)
                {
                    Owner.PopupMessage(eventArgs.User, Loc.GetString("server-magazine-barrel-component-interact-using-ammo-bolt-closed"));
                    return false;
                }

                if (ammoComponent.Caliber != _caliber)
                {
                    Owner.PopupMessage(eventArgs.User, Loc.GetString("server-magazine-barrel-component-interact-using-wrong-caliber"));
                    return false;
                }

                if (_chamberContainer.ContainedEntity == null)
                {
                    Owner.PopupMessage(eventArgs.User, Loc.GetString("server-magazine-barrel-component-interact-using-ammo-success"));
                    _chamberContainer.Insert(eventArgs.Using);
                    Dirty();
                    UpdateAppearance();
                    return true;
                }

                Owner.PopupMessage(eventArgs.User, Loc.GetString("server-magazine-barrel-component-interact-using-ammo-full"));
                return false;
            }

            return false;
        }

        public override void Examine(FormattedMessage message, bool inDetailsRange)
        {
            base.Examine(message, inDetailsRange);

            message.AddMarkup("\n" + Loc.GetString("server-magazine-barrel-component-on-examine", ("caliber", Caliber)));

            foreach (var magazineType in GetMagazineTypes())
            {
                message.AddMarkup("\n" + Loc.GetString("server-magazine-barrel-component-on-examine-magazine-type", ("magazineType", magazineType)));
            }
        }
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
