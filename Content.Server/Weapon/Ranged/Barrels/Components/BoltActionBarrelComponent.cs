using System.Collections.Generic;
using System.Threading.Tasks;
using Content.Server.Weapon.Ranged.Ammunition.Components;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Sound;
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
    /// <summary>
    /// Shotguns mostly
    /// </summary>
    [RegisterComponent]
    [NetworkedComponent()]
#pragma warning disable 618
    public sealed class BoltActionBarrelComponent : ServerRangedBarrelComponent, IUse, IInteractUsing, IMapInit
#pragma warning restore 618
    {
        // Originally I had this logic shared with PumpBarrel and used a couple of variables to control things
        // but it felt a lot messier to play around with, especially when adding verbs

        public override string Name => "BoltActionBarrel";

        public override int ShotsLeft
        {
            get
            {
                var chamberCount = _chamberContainer.ContainedEntity != null ? 1 : 0;
                return chamberCount + _spawnedAmmo.Count + _unspawnedCount;
            }
        }
        public override int Capacity => _capacity;
        [DataField("capacity")]
        private int _capacity = 6;

        private ContainerSlot _chamberContainer = default!;
        private Stack<EntityUid> _spawnedAmmo = default!;
        private Container _ammoContainer = default!;

        [ViewVariables]
        [DataField("caliber")]
        private BallisticCaliber _caliber = BallisticCaliber.Unspecified;

        [ViewVariables]
        [DataField("fillPrototype", customTypeSerializer:typeof(PrototypeIdSerializer<EntityPrototype>))]
        private string? _fillPrototype;
        [ViewVariables]
        private int _unspawnedCount;

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
        private bool _boltOpen;
        [DataField("autoCycle")]
        private bool _autoCycle;

        private AppearanceComponent? _appearanceComponent;

        // Sounds
        [DataField("soundCycle")]
        private SoundSpecifier _soundCycle = new SoundPathSpecifier("/Audio/Weapons/Guns/Cock/sf_rifle_cock.ogg");
        [DataField("soundBoltOpen")]
        private SoundSpecifier _soundBoltOpen = new SoundPathSpecifier("/Audio/Weapons/Guns/Bolt/rifle_bolt_open.ogg");
        [DataField("soundBoltClosed")]
        private SoundSpecifier _soundBoltClosed = new SoundPathSpecifier("/Audio/Weapons/Guns/Bolt/rifle_bolt_closed.ogg");
        [DataField("soundInsert")]
        private SoundSpecifier _soundInsert = new SoundPathSpecifier("/Audio/Weapons/Guns/MagIn/bullet_insert.ogg");

        void IMapInit.MapInit()
        {
            if (_fillPrototype != null)
            {
                _unspawnedCount += Capacity;
                if (_unspawnedCount > 0)
                {
                    _unspawnedCount--;
                    var chamberEntity = Entities.SpawnEntity(_fillPrototype, Entities.GetComponent<TransformComponent>(Owner).Coordinates);
                    _chamberContainer.Insert(chamberEntity);
                }
            }
            UpdateAppearance();
        }

        public override ComponentState GetComponentState()
        {
            (int, int)? count = (ShotsLeft, Capacity);
            var chamberedExists = _chamberContainer.ContainedEntity != null;
            // (Is one chambered?, is the bullet spend)
            var chamber = (chamberedExists, false);

            if (chamberedExists && Entities.TryGetComponent<AmmoComponent?>(_chamberContainer.ContainedEntity!.Value, out var ammo))
            {
                chamber.Item2 = ammo.Spent;
            }

            return new BoltActionBarrelComponentState(
                chamber,
                FireRateSelector,
                count,
                SoundGunshot.GetSound());
        }

        protected override void Initialize()
        {
            // TODO: Add existing ammo support on revolvers
            base.Initialize();
            _spawnedAmmo = new Stack<EntityUid>(_capacity - 1);
            _ammoContainer = ContainerHelpers.EnsureContainer<Container>(Owner, $"{Name}-ammo-container", out var existing);

            if (existing)
            {
                foreach (var entity in _ammoContainer.ContainedEntities)
                {
                    _spawnedAmmo.Push(entity);
                    _unspawnedCount--;
                }
            }

            _chamberContainer = ContainerHelpers.EnsureContainer<ContainerSlot>(Owner, $"{Name}-chamber-container");

            if (Entities.TryGetComponent(Owner, out AppearanceComponent? appearanceComponent))
            {
                _appearanceComponent = appearanceComponent;
            }

            _appearanceComponent?.SetData(MagazineBarrelVisuals.MagLoaded, true);
            Dirty();
            UpdateAppearance();
        }

        private void UpdateAppearance()
        {
            _appearanceComponent?.SetData(BarrelBoltVisuals.BoltOpen, BoltOpen);
            _appearanceComponent?.SetData(AmmoVisuals.AmmoCount, ShotsLeft);
            _appearanceComponent?.SetData(AmmoVisuals.AmmoMax, Capacity);
        }

        public override EntityUid? PeekAmmo()
        {
            return _chamberContainer.ContainedEntity;
        }

        public override EntityUid? TakeProjectile(EntityCoordinates spawnAt)
        {
            if (_autoCycle)
            {
                Cycle();
            }
            else
            {
                Dirty();
            }

            if (_chamberContainer.ContainedEntity is not {Valid: true} chamberEntity) return null;

            var ammoComponent = Entities.GetComponentOrNull<AmmoComponent>(chamberEntity);

            return ammoComponent == null ? null : EntitySystem.Get<GunSystem>().TakeBullet(ammoComponent, spawnAt);
        }

        protected override bool WeaponCanFire()
        {
            if (!base.WeaponCanFire())
            {
                return false;
            }

            return !BoltOpen && _chamberContainer.ContainedEntity != null;
        }

        private void Cycle(bool manual = false)
        {
            TryEjectChamber();
            TryFeedChamber();

            if (_chamberContainer.ContainedEntity == null && manual)
            {
                BoltOpen = true;
                if (Owner.TryGetContainer(out var container))
                {
                    Owner.PopupMessage(container.Owner, Loc.GetString("bolt-action-barrel-component-bolt-opened"));
                }
                return;
            }
            else
            {
                SoundSystem.Play(Filter.Pvs(Owner), _soundCycle.GetSound(), Owner, AudioParams.Default.WithVolume(-2));
            }

            Dirty();
            UpdateAppearance();
        }

        public bool TryInsertBullet(EntityUid user, EntityUid ammo)
        {
            if (!Entities.TryGetComponent(ammo, out AmmoComponent? ammoComponent))
            {
                return false;
            }

            if (ammoComponent.Caliber != _caliber)
            {
                Owner.PopupMessage(user, Loc.GetString("bolt-action-barrel-component-try-insert-bullet-wrong-caliber"));
                return false;
            }

            if (!BoltOpen)
            {
                Owner.PopupMessage(user, Loc.GetString("bolt-action-barrel-component-try-insert-bullet-bolt-closed"));
                return false;
            }

            if (_chamberContainer.ContainedEntity == null)
            {
                _chamberContainer.Insert(ammo);
                SoundSystem.Play(Filter.Pvs(Owner), _soundInsert.GetSound(), Owner, AudioParams.Default.WithVolume(-2));
                Dirty();
                UpdateAppearance();
                return true;
            }

            if (_ammoContainer.ContainedEntities.Count < Capacity - 1)
            {
                _ammoContainer.Insert(ammo);
                _spawnedAmmo.Push(ammo);
                SoundSystem.Play(Filter.Pvs(Owner), _soundInsert.GetSound(), Owner, AudioParams.Default.WithVolume(-2));
                Dirty();
                UpdateAppearance();
                return true;
            }

            Owner.PopupMessage(user, Loc.GetString("bolt-action-barrel-component-try-insert-bullet-no-room"));

            return false;
        }

        public bool UseEntity(UseEntityEventArgs eventArgs)
        {
            if (BoltOpen)
            {
                BoltOpen = false;
                Owner.PopupMessage(eventArgs.User, Loc.GetString("bolt-action-barrel-component-bolt-closed"));
                return true;
            }

            Cycle(true);

            return true;
        }

        public async Task<bool> InteractUsing(InteractUsingEventArgs eventArgs)
        {
            return TryInsertBullet(eventArgs.User, eventArgs.Using);
        }

        private bool TryEjectChamber()
        {
            if (_chamberContainer.ContainedEntity is {Valid: true} chambered)
            {
                if (!_chamberContainer.Remove(chambered))
                {
                    return false;
                }
                if (!Entities.GetComponent<AmmoComponent>(chambered).Caseless)
                {
                    EjectCasing(chambered);
                }
                return true;
            }
            return false;
        }

        private bool TryFeedChamber()
        {
            if (_chamberContainer.ContainedEntity != null)
            {
                return false;
            }
            if (_spawnedAmmo.TryPop(out var next))
            {
                _ammoContainer.Remove(next);
                _chamberContainer.Insert(next);
                return true;
            }
            else if (_unspawnedCount > 0)
            {
                _unspawnedCount--;
                var ammoEntity = Entities.SpawnEntity(_fillPrototype, Entities.GetComponent<TransformComponent>(Owner).Coordinates);
                _chamberContainer.Insert(ammoEntity);
                return true;
            }
            return false;
        }

        public override void Examine(FormattedMessage message, bool inDetailsRange)
        {
            base.Examine(message, inDetailsRange);

            message.AddMarkup("\n" + Loc.GetString("bolt-action-barrel-component-on-examine", ("caliber", _caliber)));
        }
    }
}
