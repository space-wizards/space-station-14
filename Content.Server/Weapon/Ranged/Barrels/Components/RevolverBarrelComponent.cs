using System;
using System.Threading.Tasks;
using Content.Server.Weapon.Ranged.Ammunition.Components;
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
using Robust.Shared.Random;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.ViewVariables;

namespace Content.Server.Weapon.Ranged.Barrels.Components
{
    [RegisterComponent]
    [NetworkedComponent()]
    public sealed class RevolverBarrelComponent : ServerRangedBarrelComponent, IUse, IInteractUsing, ISerializationHooks
    {
        [Dependency] private readonly IRobustRandom _random = default!;

        public override string Name => "RevolverBarrel";

        [ViewVariables]
        [DataField("caliber")]
        private BallisticCaliber _caliber = BallisticCaliber.Unspecified;

        private Container _ammoContainer = default!;

        [ViewVariables]
        private int _currentSlot;

        public override int Capacity => _ammoSlots.Length;

        [DataField("capacity")]
        private int _serializedCapacity = 6;

        [DataField("ammoSlots", readOnly: true)]
        private EntityUid[] _ammoSlots = Array.Empty<EntityUid>();

        public override int ShotsLeft => _ammoContainer.ContainedEntities.Count;

        [ViewVariables]
        [DataField("fillPrototype", customTypeSerializer:typeof(PrototypeIdSerializer<EntityPrototype>))]
        private string? _fillPrototype;

        [ViewVariables]
        private int _unspawnedCount;

        // Sounds
        [DataField("soundEject")]
        private SoundSpecifier _soundEject = new SoundPathSpecifier("/Audio/Weapons/Guns/MagOut/revolver_magout.ogg");

        [DataField("soundInsert")]
        private SoundSpecifier _soundInsert = new SoundPathSpecifier("/Audio/Weapons/Guns/MagIn/revolver_magin.ogg");

        [DataField("soundSpin")]
        private SoundSpecifier _soundSpin = new SoundPathSpecifier("/Audio/Weapons/Guns/Misc/revolver_spin.ogg");

        void ISerializationHooks.BeforeSerialization()
        {
            _serializedCapacity = _ammoSlots.Length;
        }

        void ISerializationHooks.AfterDeserialization()
        {
            _ammoSlots = new EntityUid[_serializedCapacity];
        }

        public override ComponentState GetComponentState()
        {
            var slotsSpent = new bool?[Capacity];
            for (var i = 0; i < Capacity; i++)
            {
                slotsSpent[i] = null;
                var ammoEntity = _ammoSlots[i];
                if (ammoEntity != default && Entities.TryGetComponent(ammoEntity, out AmmoComponent? ammo))
                {
                    slotsSpent[i] = ammo.Spent;
                }
            }

            //TODO: make yaml var to not sent currentSlot/UI? (for russian roulette)
            return new RevolverBarrelComponentState(
                _currentSlot,
                FireRateSelector,
                slotsSpent,
                SoundGunshot.GetSound());
        }

        protected override void Initialize()
        {
            base.Initialize();
            _unspawnedCount = Capacity;
            int idx = 0;
            _ammoContainer = ContainerHelpers.EnsureContainer<Container>(Owner, $"{Name}-ammoContainer", out var existing);
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
                var entity = Entities.SpawnEntity(_fillPrototype, Entities.GetComponent<TransformComponent>(Owner).Coordinates);
                _ammoSlots[idx] = entity;
                _ammoContainer.Insert(entity);
                idx++;
            }

            UpdateAppearance();
            Dirty();
        }

        private void UpdateAppearance()
        {
            if (!Entities.TryGetComponent(Owner, out AppearanceComponent? appearance))
            {
                return;
            }

            // Placeholder, at this stage it's just here for the RPG
            appearance.SetData(MagazineBarrelVisuals.MagLoaded, ShotsLeft > 0);
            appearance.SetData(AmmoVisuals.AmmoCount, ShotsLeft);
            appearance.SetData(AmmoVisuals.AmmoMax, Capacity);
        }

        public bool TryInsertBullet(EntityUid user, EntityUid entity)
        {
            if (!Entities.TryGetComponent(entity, out AmmoComponent? ammoComponent))
            {
                return false;
            }

            if (ammoComponent.Caliber != _caliber)
            {
                Owner.PopupMessage(user, Loc.GetString("revolver-barrel-component-try-inser-bullet-wrong-caliber"));
                return false;
            }

            // Functions like a stack
            // These are inserted in reverse order but then when fired Cycle will go through in order
            // The reason we don't just use an actual stack is because spin can select a random slot to point at
            for (var i = _ammoSlots.Length - 1; i >= 0; i--)
            {
                var slot = _ammoSlots[i];
                if (slot == default)
                {
                    _currentSlot = i;
                    _ammoSlots[i] = entity;
                    _ammoContainer.Insert(entity);
                    SoundSystem.Play(Filter.Pvs(Owner), _soundInsert.GetSound(), Owner, AudioParams.Default.WithVolume(-2));

                    Dirty();
                    UpdateAppearance();
                    return true;
                }
            }

            Owner.PopupMessage(user, Loc.GetString("revolver-barrel-component-try-inser-bullet-ammo-full"));
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
            SoundSystem.Play(Filter.Pvs(Owner), _soundSpin.GetSound(), Owner, AudioParams.Default.WithVolume(-2));
            Dirty();
        }

        public override EntityUid? PeekAmmo()
        {
            return _ammoSlots[_currentSlot];
        }

        /// <summary>
        /// Takes a projectile out if possible
        /// IEnumerable just to make supporting shotguns saner
        /// </summary>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public override EntityUid? TakeProjectile(EntityCoordinates spawnAt)
        {
            var ammo = _ammoSlots[_currentSlot];
            EntityUid? bullet = null;
            if (ammo != default)
            {
                var ammoComponent = Entities.GetComponent<AmmoComponent>(ammo);
                bullet = EntitySystem.Get<GunSystem>().TakeBullet(ammoComponent, spawnAt);
                if (ammoComponent.Caseless)
                {
                    _ammoSlots[_currentSlot] = default;
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
                if (entity == default)
                {
                    continue;
                }

                _ammoContainer.Remove(entity);
                EjectCasing(entity);
                _ammoSlots[i] = default;
            }

            if (_ammoContainer.ContainedEntities.Count > 0)
            {
                SoundSystem.Play(Filter.Pvs(Owner), _soundEject.GetSound(), Owner, AudioParams.Default.WithVolume(-1));
            }

            // May as well point back at the end?
            _currentSlot = _ammoSlots.Length - 1;
        }

        /// <summary>
        /// Eject all casings
        /// </summary>
        /// <param name="eventArgs"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public bool UseEntity(UseEntityEventArgs eventArgs)
        {
            EjectAllSlots();
            Dirty();
            UpdateAppearance();
            return true;
        }

        public async Task<bool> InteractUsing(InteractUsingEventArgs eventArgs)
        {
            return TryInsertBullet(eventArgs.User, eventArgs.Using);
        }
    }
}
