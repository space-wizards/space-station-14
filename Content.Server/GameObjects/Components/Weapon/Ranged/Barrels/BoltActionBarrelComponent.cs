using System.Collections.Generic;
using System.Threading.Tasks;
using Content.Server.GameObjects.Components.Weapon.Ranged.Ammunition;
using Content.Shared.GameObjects;
using Content.Shared.GameObjects.Components.Weapons.Ranged.Barrels;
using Content.Shared.GameObjects.EntitySystems;
using Content.Shared.GameObjects.Verbs;
using Content.Shared.Interfaces;
using Content.Shared.Interfaces.GameObjects.Components;
using Robust.Server.GameObjects;
using Robust.Server.GameObjects.Components.Container;
using Robust.Server.GameObjects.EntitySystems;
using Robust.Server.Interfaces.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.Map;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Weapon.Ranged.Barrels
{
    /// <summary>
    /// Shotguns mostly
    /// </summary>
    [RegisterComponent]
    public sealed class BoltActionBarrelComponent : ServerRangedBarrelComponent, IMapInit, IExamine
    {
        // Originally I had this logic shared with PumpBarrel and used a couple of variables to control things
        // but it felt a lot messier to play around with, especially when adding verbs

        public override string Name => "BoltActionBarrel";
        public override uint? NetID => ContentNetIDs.BOLTACTION_BARREL;

        public override int ShotsLeft
        {
            get
            {
                var chamberCount = _chamberContainer.ContainedEntity != null ? 1 : 0;
                return chamberCount + _spawnedAmmo.Count + _unspawnedCount;
            }
        }
        public override int Capacity => _capacity;
        private int _capacity;

        private ContainerSlot _chamberContainer;
        private Stack<IEntity> _spawnedAmmo;
        private Container _ammoContainer;

        [ViewVariables]
        private BallisticCaliber _caliber;

        [ViewVariables]
        private string _fillPrototype;
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

                var soundSystem = EntitySystem.Get<AudioSystem>();

                if (value)
                {
                    TryEjectChamber();
                    if (_soundBoltOpen != null)
                    {
                        soundSystem.PlayAtCoords(_soundBoltOpen, Owner.Transform.Coordinates, AudioParams.Default.WithVolume(-2));
                    }
                }
                else
                {
                    TryFeedChamber();
                    if (_soundBoltClosed != null)
                    {
                        soundSystem.PlayAtCoords(_soundBoltClosed, Owner.Transform.Coordinates, AudioParams.Default.WithVolume(-2));
                    }
                }

                _boltOpen = value;
                UpdateAppearance();
                Dirty();
            }
        }
        private bool _boltOpen;
        private bool _autoCycle;

        private AppearanceComponent _appearanceComponent;
        // Sounds
        private string _soundCycle;
        private string _soundBoltOpen;
        private string _soundBoltClosed;
        private string _soundInsert;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
            serializer.DataField(ref _caliber, "caliber", BallisticCaliber.Unspecified);
            serializer.DataField(ref _capacity, "capacity", 6);
            serializer.DataField(ref _fillPrototype, "fillPrototype", null);
            serializer.DataField(ref _autoCycle, "autoCycle", false);

            serializer.DataField(ref _soundCycle, "soundCycle", "/Audio/Weapons/Guns/Cock/sf_rifle_cock.ogg");
            serializer.DataField(ref _soundBoltOpen, "soundBoltOpen", "/Audio/Weapons/Guns/Bolt/rifle_bolt_open.ogg");
            serializer.DataField(ref _soundBoltClosed, "soundBoltClosed", "/Audio/Weapons/Guns/Bolt/rifle_bolt_closed.ogg");
            serializer.DataField(ref _soundInsert, "soundInsert", "/Audio/Weapons/Guns/MagIn/bullet_insert.ogg");
        }

        void IMapInit.MapInit()
        {
            if (_fillPrototype != null)
            {
                _unspawnedCount += Capacity;
                if (_unspawnedCount > 0)
                {
                    _unspawnedCount--;
                    var chamberEntity = Owner.EntityManager.SpawnEntity(_fillPrototype, Owner.Transform.Coordinates);
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
            if (chamberedExists && _chamberContainer.ContainedEntity.TryGetComponent<AmmoComponent>(out var ammo))
            {
                chamber.Item2 = ammo.Spent;
            }

            return new BoltActionBarrelComponentState(
                chamber,
                FireRateSelector,
                count,
                SoundGunshot);
        }

        public override void Initialize()
        {
            // TODO: Add existing ammo support on revolvers
            base.Initialize();
            _spawnedAmmo = new Stack<IEntity>(_capacity - 1);
            _ammoContainer = ContainerManagerComponent.Ensure<Container>($"{Name}-ammo-container", Owner, out var existing);

            if (existing)
            {
                foreach (var entity in _ammoContainer.ContainedEntities)
                {
                    _spawnedAmmo.Push(entity);
                    _unspawnedCount--;
                }
            }

            _chamberContainer = ContainerManagerComponent.Ensure<ContainerSlot>($"{Name}-chamber-container", Owner);

            if (Owner.TryGetComponent(out AppearanceComponent appearanceComponent))
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

        public override IEntity PeekAmmo()
        {
            return _chamberContainer.ContainedEntity;
        }

        public override IEntity TakeProjectile(EntityCoordinates spawnAtGrid, MapCoordinates spawnAtMap)
        {
            var chamberEntity = _chamberContainer.ContainedEntity;
            if (_autoCycle)
            {
                Cycle();
            }
            else
            {
                Dirty();
            }

            return chamberEntity?.GetComponent<AmmoComponent>().TakeBullet(spawnAtGrid, spawnAtMap);
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
                if (ContainerHelpers.TryGetContainer(Owner, out var container))
                {
                    Owner.PopupMessage(container.Owner, Loc.GetString("Bolt opened"));
                }
                return;
            }
            else
            {
                if (_soundCycle != null)
                {
                    EntitySystem.Get<AudioSystem>().PlayAtCoords(_soundCycle, Owner.Transform.Coordinates, AudioParams.Default.WithVolume(-2));
                }
            }

            Dirty();
            UpdateAppearance();
        }

        public bool TryInsertBullet(IEntity user, IEntity ammo)
        {
            if (!ammo.TryGetComponent(out AmmoComponent ammoComponent))
            {
                return false;
            }

            if (ammoComponent.Caliber != _caliber)
            {
                Owner.PopupMessage(user, Loc.GetString("Wrong caliber"));
                return false;
            }

            if (!BoltOpen)
            {
                Owner.PopupMessage(user, Loc.GetString("Bolt isn't open"));
                return false;
            }

            if (_chamberContainer.ContainedEntity == null)
            {
                _chamberContainer.Insert(ammo);
                if (_soundInsert != null)
                {
                    EntitySystem.Get<AudioSystem>().PlayAtCoords(_soundInsert, Owner.Transform.Coordinates, AudioParams.Default.WithVolume(-2));
                }
                Dirty();
                UpdateAppearance();
                return true;
            }

            if (_ammoContainer.ContainedEntities.Count < Capacity - 1)
            {
                _ammoContainer.Insert(ammo);
                _spawnedAmmo.Push(ammo);
                if (_soundInsert != null)
                {
                    EntitySystem.Get<AudioSystem>().PlayAtCoords(_soundInsert, Owner.Transform.Coordinates, AudioParams.Default.WithVolume(-2));
                }
                Dirty();
                UpdateAppearance();
                return true;
            }

            Owner.PopupMessage(user, Loc.GetString("No room"));

            return false;
        }

        public override bool UseEntity(UseEntityEventArgs eventArgs)
        {
            if (BoltOpen)
            {
                BoltOpen = false;
                Owner.PopupMessage(eventArgs.User, Loc.GetString("Bolt closed"));
                return true;
            }

            Cycle(true);

            return true;
        }

        public override async Task<bool> InteractUsing(InteractUsingEventArgs eventArgs)
        {
            return TryInsertBullet(eventArgs.User, eventArgs.Using);
        }

        private bool TryEjectChamber()
        {
            var chamberedEntity = _chamberContainer.ContainedEntity;
            if (chamberedEntity != null)
            {
                if (!_chamberContainer.Remove(chamberedEntity))
                {
                    return false;
                }
                if (!chamberedEntity.GetComponent<AmmoComponent>().Caseless)
                {
                    EjectCasing(chamberedEntity);
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
                var ammoEntity = Owner.EntityManager.SpawnEntity(_fillPrototype, Owner.Transform.Coordinates);
                _chamberContainer.Insert(ammoEntity);
                return true;
            }
            return false;
        }

        public override void Examine(FormattedMessage message, bool inDetailsRange)
        {
            base.Examine(message, inDetailsRange);

            message.AddMarkup(Loc.GetString("\nIt uses [color=white]{0}[/color] ammo.", _caliber));
        }

        [Verb]
        private sealed class OpenBoltVerb : Verb<BoltActionBarrelComponent>
        {
            protected override void GetData(IEntity user, BoltActionBarrelComponent component, VerbData data)
            {
                if (!ActionBlockerSystem.CanInteract(user))
                {
                    data.Visibility = VerbVisibility.Invisible;
                    return;
                }

                data.Text = Loc.GetString("Open bolt");
                data.Visibility = component.BoltOpen ? VerbVisibility.Invisible : VerbVisibility.Visible;
            }

            protected override void Activate(IEntity user, BoltActionBarrelComponent component)
            {
                component.BoltOpen = true;
            }
        }

        [Verb]
        private sealed class CloseBoltVerb : Verb<BoltActionBarrelComponent>
        {
            protected override void GetData(IEntity user, BoltActionBarrelComponent component, VerbData data)
            {
                if (!ActionBlockerSystem.CanInteract(user))
                {
                    data.Visibility = VerbVisibility.Invisible;
                    return;
                }

                data.Text = Loc.GetString("Close bolt");
                data.Visibility = component.BoltOpen ? VerbVisibility.Visible : VerbVisibility.Invisible;
            }

            protected override void Activate(IEntity user, BoltActionBarrelComponent component)
            {
                component.BoltOpen = false;
            }
        }
    }
}
