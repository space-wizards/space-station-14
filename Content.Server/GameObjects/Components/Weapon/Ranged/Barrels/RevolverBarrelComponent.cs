using System;
using Content.Server.GameObjects.Components.Sound;
using Content.Server.GameObjects.Components.Weapon.Ranged.Ammunition;
using Content.Server.GameObjects.EntitySystems;
using Content.Shared.GameObjects;
using Content.Shared.Interfaces;
using Robust.Server.GameObjects;
using Robust.Server.GameObjects.Components.Container;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Random;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Serialization;

namespace Content.Server.GameObjects.Components.Weapon.Ranged.Barrels
{
    [RegisterComponent]
    public sealed class RevolverBarrelComponent : ServerRangedBarrelComponent
    {
        public override string Name => "RevolverBarrel";
        private BallisticCaliber _caliber;
        private Container _ammoContainer;
        private int _currentSlot = 0;
        public override int Capacity => _ammoSlots.Length;
        private IEntity[] _ammoSlots;

        public override int ShotsLeft => _ammoContainer.ContainedEntities.Count;

        private AppearanceComponent _appearanceComponent;

        // Sounds
        private SoundComponent _soundComponent;
        private string _soundEject;
        private string _soundInsert;
        private string _soundSpin;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
            serializer.DataField(ref _caliber, "caliber", BallisticCaliber.Unspecified);
            var capacity = serializer.ReadDataField("capacity", 6);
            _ammoSlots = new IEntity[capacity];

            // Sounds
            serializer.DataField(ref _soundEject, "soundEject", "/Audio/Guns/MagOut/revolver_magout.ogg");
            serializer.DataField(ref _soundInsert, "soundInsert", "/Audio/Guns/MagIn/revolver_magin.ogg");
            serializer.DataField(ref _soundSpin, "soundSpin", null);
        }

        public override void Initialize()
        {
            base.Initialize();
            if (Owner.TryGetComponent(out SoundComponent soundComponent))
            {
                _soundComponent = soundComponent;
            }
            _ammoContainer = ContainerManagerComponent.Ensure<Container>($"{Name}-ammoContainer", Owner);

            if (Owner.TryGetComponent(out AppearanceComponent appearanceComponent))
            {
                _appearanceComponent = appearanceComponent;
            }
        }

        private void UpdateAppearance()
        {
            // Currently none but they should probably exist...
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
                    if (_soundInsert != null && _soundComponent != null)
                    {
                        _soundComponent.Play(_soundInsert);
                    }

                    // Dirty();
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
            // Dirty();
            UpdateAppearance();
        }

        /// <summary>
        /// Russian Roulette
        /// </summary>
        public void Spin()
        {
            var random = IoCManager.Resolve<IRobustRandom>().Next(_ammoSlots.Length - 1);
            _currentSlot = random;
            _soundComponent?.Play(_soundSpin);
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
        public override IEntity TakeProjectile()
        {
            var ammo = _ammoSlots[_currentSlot];
            IEntity bullet = null;
            if (ammo != null)
            {
                bullet = ammo.GetComponent<AmmoComponent>().TakeBullet();
            }
            Cycle();
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
                if (_soundEject != null && _soundComponent != null)
                {
                    _soundComponent.Play(_soundEject);
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
            //Dirty();
            UpdateAppearance();
            return true;
        }

        public override bool AttackBy(AttackByEventArgs eventArgs)
        {
            return TryInsertBullet(eventArgs.User, eventArgs.AttackWith);
        }
        
        [Verb]
        private sealed class SpinRevolverVerb : Verb<RevolverBarrelComponent>
        {
            protected override string GetText(IEntity user, RevolverBarrelComponent component)
            {
                return Loc.GetString("Spin");
            }

            protected override VerbVisibility GetVisibility(IEntity user, RevolverBarrelComponent component)
            {
                if (component.Capacity <= 1)
                {
                    return VerbVisibility.Invisible;
                }
                
                return component.ShotsLeft > 0 ? VerbVisibility.Visible : VerbVisibility.Disabled;
            }

            protected override void Activate(IEntity user, RevolverBarrelComponent component)
            {
                component.Spin();
                component.Owner.PopupMessage(user, Loc.GetString("Spun the cylinder"));
            }
        }
    }
}
