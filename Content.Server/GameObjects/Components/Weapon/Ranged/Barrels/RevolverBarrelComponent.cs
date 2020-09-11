using System;
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
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Random;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Map;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Weapon.Ranged.Barrels
{
    [RegisterComponent]
    public sealed class RevolverBarrelComponent : ServerRangedBarrelComponent
    {
        [Dependency] private readonly IRobustRandom _random = default!;

        public override string Name => "RevolverBarrel";
        public override uint? NetID => ContentNetIDs.REVOLVER_BARREL;

        [ViewVariables]
        private BallisticCaliber _caliber;
        private Container _ammoContainer;
        [ViewVariables]
        private int _currentSlot = 0;
        public override int Capacity => _ammoSlots.Length;
        private IEntity[] _ammoSlots;

        public override int ShotsLeft => _ammoContainer.ContainedEntities.Count;

        private AppearanceComponent _appearanceComponent;
        [ViewVariables]
        private string _fillPrototype;
        private int _unspawnedCount;

        // Sounds
        private string _soundEject;
        private string _soundInsert;
        private string _soundSpin;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(ref _caliber, "caliber", BallisticCaliber.Unspecified);
            serializer.DataReadWriteFunction(
                "capacity",
                6,
                cap => _ammoSlots = new IEntity[cap],
                () => _ammoSlots.Length);
            serializer.DataField(ref _fillPrototype, "fillPrototype", null);

            // Sounds
            serializer.DataField(ref _soundEject, "soundEject", "/Audio/Weapons/Guns/MagOut/revolver_magout.ogg");
            serializer.DataField(ref _soundInsert, "soundInsert", "/Audio/Weapons/Guns/MagIn/revolver_magin.ogg");
            serializer.DataField(ref _soundSpin, "soundSpin", "/Audio/Weapons/Guns/Misc/revolver_spin.ogg");
        }

        public override ComponentState GetComponentState()
        {
            var slotsSpent = new bool?[Capacity];
            for (var i = 0; i < Capacity; i++)
            {
                slotsSpent[i] = null;
                if (_ammoSlots[i] != null && _ammoSlots[i].TryGetComponent(out AmmoComponent ammo))
                {
                    slotsSpent[i] = ammo.Spent;
                }
            }

            //TODO: make yaml var to not sent currentSlot/UI? (for russian roulette)
            return new RevolverBarrelComponentState(
                _currentSlot,
                FireRateSelector,
                slotsSpent,
                SoundGunshot);
        }

        public override void Initialize()
        {
            base.Initialize();
            _unspawnedCount = Capacity;
            int idx = 0;
            _ammoContainer = ContainerManagerComponent.Ensure<Container>($"{Name}-ammoContainer", Owner, out var existing);
            if (existing)
            {
                foreach (var entity in _ammoContainer.ContainedEntities)
                {
                    _unspawnedCount--;
                    _ammoSlots[idx] = entity;
                    idx++;
                }
            }

            for (var i = 0; i < _unspawnedCount; i++)
            {
                var entity = Owner.EntityManager.SpawnEntity(_fillPrototype, Owner.Transform.Coordinates);
                _ammoSlots[idx] = entity;
                _ammoContainer.Insert(entity);
                idx++;
            }

            if (Owner.TryGetComponent(out AppearanceComponent appearanceComponent))
            {
                _appearanceComponent = appearanceComponent;
            }

            UpdateAppearance();
            Dirty();
        }

        private void UpdateAppearance()
        {
            // Placeholder, at this stage it's just here for the RPG
            _appearanceComponent?.SetData(MagazineBarrelVisuals.MagLoaded, ShotsLeft > 0);
            _appearanceComponent?.SetData(AmmoVisuals.AmmoCount, ShotsLeft);
            _appearanceComponent?.SetData(AmmoVisuals.AmmoMax, Capacity);
        }

        public bool TryInsertBullet(IEntity user, IEntity entity)
        {
            if (!entity.TryGetComponent(out AmmoComponent ammoComponent))
            {
                return false;
            }

            if (ammoComponent.Caliber != _caliber)
            {
                Owner.PopupMessage(user, Loc.GetString("Wrong caliber"));
                return false;
            }

            // Functions like a stack
            // These are inserted in reverse order but then when fired Cycle will go through in order
            // The reason we don't just use an actual stack is because spin can select a random slot to point at
            for (var i = _ammoSlots.Length - 1; i >= 0; i--)
            {
                var slot = _ammoSlots[i];
                if (slot == null)
                {
                    _currentSlot = i;
                    _ammoSlots[i] = entity;
                    _ammoContainer.Insert(entity);
                    if (_soundInsert != null)
                    {
                        EntitySystem.Get<AudioSystem>().PlayAtCoords(_soundInsert, Owner.Transform.Coordinates, AudioParams.Default.WithVolume(-2));
                    }

                    Dirty();
                    UpdateAppearance();
                    return true;
                }
            }

            Owner.PopupMessage(user, Loc.GetString("Ammo full"));
            return false;
        }

        public void Cycle()
        {
            // Move up a slot
            _currentSlot = (_currentSlot + 1) % _ammoSlots.Length;
            Dirty();
            UpdateAppearance();
        }

        /// <summary>
        /// Russian Roulette
        /// </summary>
        public void Spin()
        {
            var random = _random.Next(_ammoSlots.Length - 1);
            _currentSlot = random;
            if (_soundSpin != null)
            {
                EntitySystem.Get<AudioSystem>().PlayAtCoords(_soundSpin, Owner.Transform.Coordinates, AudioParams.Default.WithVolume(-2));
            }
            Dirty();
        }

        public override IEntity PeekAmmo()
        {
            return _ammoSlots[_currentSlot];
        }

        /// <summary>
        /// Takes a projectile out if possible
        /// IEnumerable just to make supporting shotguns saner
        /// </summary>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public override IEntity TakeProjectile(EntityCoordinates spawnAtGrid, MapCoordinates spawnAtMap)
        {
            var ammo = _ammoSlots[_currentSlot];
            IEntity bullet = null;
            if (ammo != null)
            {
                var ammoComponent = ammo.GetComponent<AmmoComponent>();
                bullet = ammoComponent.TakeBullet(spawnAtGrid, spawnAtMap);
                if (ammoComponent.Caseless)
                {
                    _ammoSlots[_currentSlot] = null;
                    _ammoContainer.Remove(ammo);
                }
            }
            Cycle();
            UpdateAppearance();
            return bullet;
        }

        private void EjectAllSlots()
        {
            for (var i = 0; i < _ammoSlots.Length; i++)
            {
                var entity = _ammoSlots[i];
                if (entity == null)
                {
                    continue;
                }

                _ammoContainer.Remove(entity);
                EjectCasing(entity);
                _ammoSlots[i] = null;
            }

            if (_ammoContainer.ContainedEntities.Count > 0)
            {
                if (_soundEject != null)
                {
                    EntitySystem.Get<AudioSystem>().PlayAtCoords(_soundEject, Owner.Transform.Coordinates, AudioParams.Default.WithVolume(-1));
                }
            }

            // May as well point back at the end?
            _currentSlot = _ammoSlots.Length - 1;
            return;
        }

        /// <summary>
        /// Eject all casings
        /// </summary>
        /// <param name="eventArgs"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public override bool UseEntity(UseEntityEventArgs eventArgs)
        {
            EjectAllSlots();
            Dirty();
            UpdateAppearance();
            return true;
        }

        public override async Task<bool> InteractUsing(InteractUsingEventArgs eventArgs)
        {
            return TryInsertBullet(eventArgs.User, eventArgs.Using);
        }

        [Verb]
        private sealed class SpinRevolverVerb : Verb<RevolverBarrelComponent>
        {
            protected override void GetData(IEntity user, RevolverBarrelComponent component, VerbData data)
            {
                if (!ActionBlockerSystem.CanInteract(user))
                {
                    data.Visibility = VerbVisibility.Invisible;
                    return;
                }

                data.Text = Loc.GetString("Spin");
                if (component.Capacity <= 1)
                {
                    data.Visibility = VerbVisibility.Invisible;
                    return;
                }

                data.Visibility = component.ShotsLeft > 0 ? VerbVisibility.Visible : VerbVisibility.Disabled;
            }

            protected override void Activate(IEntity user, RevolverBarrelComponent component)
            {
                component.Spin();
                component.Owner.PopupMessage(user, Loc.GetString("Spun the cylinder"));
            }
        }
    }
}
