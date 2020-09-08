using System.Collections.Generic;
using System.Threading.Tasks;
using Content.Server.GameObjects.Components.Weapon.Ranged.Ammunition;
using Content.Shared.GameObjects;
using Content.Shared.GameObjects.Components.Weapons.Ranged.Barrels;
using Content.Shared.GameObjects.EntitySystems;
using Content.Shared.Interfaces;
using Content.Shared.Interfaces.GameObjects.Components;
using Robust.Server.GameObjects;
using Robust.Server.GameObjects.Components.Container;
using Robust.Server.GameObjects.EntitySystems;
using Robust.Server.Interfaces.GameObjects;
using Robust.Shared.Audio;
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
    /// Bolt-action rifles
    /// </summary>
    [RegisterComponent]
    public sealed class PumpBarrelComponent : ServerRangedBarrelComponent, IMapInit, IExamine
    {
        public override string Name => "PumpBarrel";
        public override uint? NetID => ContentNetIDs.PUMP_BARREL;

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

        // Even a point having a chamber? I guess it makes some of the below code cleaner
        private ContainerSlot _chamberContainer;
        private Stack<IEntity> _spawnedAmmo;
        private Container _ammoContainer;

        [ViewVariables]
        private BallisticCaliber _caliber;

        [ViewVariables]
        private string _fillPrototype;
        private int _unspawnedCount;

        private bool _manualCycle;

        private AppearanceComponent _appearanceComponent;

        // Sounds
        private string _soundCycle;
        private string _soundInsert;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
            serializer.DataField(ref _caliber, "caliber", BallisticCaliber.Unspecified);
            serializer.DataField(ref _capacity, "capacity", 6);
            serializer.DataField(ref _fillPrototype, "fillPrototype", null);
            serializer.DataField(ref _manualCycle, "manualCycle", true);

            serializer.DataField(ref _soundCycle, "soundCycle", "/Audio/Weapons/Guns/Cock/sf_rifle_cock.ogg");
            serializer.DataField(ref _soundInsert, "soundInsert", "/Audio/Weapons/Guns/MagIn/bullet_insert.ogg");

            _spawnedAmmo = new Stack<IEntity>(_capacity - 1);
        }

        void IMapInit.MapInit()
        {
            if (_fillPrototype != null)
            {
                _unspawnedCount += Capacity - 1;
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
            return new PumpBarrelComponentState(
                chamber,
                FireRateSelector,
                count,
                SoundGunshot);
        }

        public override void Initialize()
        {
            base.Initialize();

            _ammoContainer =
                ContainerManagerComponent.Ensure<Container>($"{Name}-ammo-container", Owner, out var existing);

            if (existing)
            {
                foreach (var entity in _ammoContainer.ContainedEntities)
                {
                    _spawnedAmmo.Push(entity);
                    _unspawnedCount--;
                }
            }

            _chamberContainer =
                ContainerManagerComponent.Ensure<ContainerSlot>($"{Name}-chamber-container", Owner, out existing);
            if (existing)
            {
                _unspawnedCount--;
            }

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
            if (!_manualCycle)
            {
                Cycle();
            }
            else
            {
                Dirty();
            }

            return chamberEntity?.GetComponent<AmmoComponent>().TakeBullet(spawnAtGrid, spawnAtMap);
        }

        private void Cycle(bool manual = false)
        {
            var chamberedEntity = _chamberContainer.ContainedEntity;
            if (chamberedEntity != null)
            {
                _chamberContainer.Remove(chamberedEntity);
                var ammoComponent = chamberedEntity.GetComponent<AmmoComponent>();
                if (!ammoComponent.Caseless)
                {
                    EjectCasing(chamberedEntity);
                }
            }

            if (_spawnedAmmo.TryPop(out var next))
            {
                _ammoContainer.Remove(next);
                _chamberContainer.Insert(next);
            }

            if (_unspawnedCount > 0)
            {
                _unspawnedCount--;
                var ammoEntity = Owner.EntityManager.SpawnEntity(_fillPrototype, Owner.Transform.Coordinates);
                _chamberContainer.Insert(ammoEntity);
            }

            if (manual)
            {
                if (_soundCycle != null)
                {
                    EntitySystem.Get<AudioSystem>().PlayAtCoords(_soundCycle, Owner.Transform.Coordinates, AudioParams.Default.WithVolume(-2));
                }
            }

            Dirty();
            UpdateAppearance();
        }

        public bool TryInsertBullet(InteractUsingEventArgs eventArgs)
        {
            if (!eventArgs.Using.TryGetComponent(out AmmoComponent ammoComponent))
            {
                return false;
            }

            if (ammoComponent.Caliber != _caliber)
            {
                Owner.PopupMessage(eventArgs.User, Loc.GetString("Wrong caliber"));
                return false;
            }

            if (_ammoContainer.ContainedEntities.Count < Capacity - 1)
            {
                _ammoContainer.Insert(eventArgs.Using);
                _spawnedAmmo.Push(eventArgs.Using);
                Dirty();
                UpdateAppearance();
                if (_soundInsert != null)
                {
                    EntitySystem.Get<AudioSystem>().PlayAtCoords(_soundInsert, Owner.Transform.Coordinates, AudioParams.Default.WithVolume(-2));
                }
                return true;
            }

            Owner.PopupMessage(eventArgs.User, Loc.GetString("No room"));

            return false;
        }

        public override bool UseEntity(UseEntityEventArgs eventArgs)
        {
            Cycle(true);
            return true;
        }

        public override async Task<bool> InteractUsing(InteractUsingEventArgs eventArgs)
        {
            return TryInsertBullet(eventArgs);
        }

        public override void Examine(FormattedMessage message, bool inDetailsRange)
        {
            base.Examine(message, inDetailsRange);

            message.AddMarkup(Loc.GetString("\nIt uses [color=white]{0}[/color] ammo.", _caliber));
        }
    }
}
