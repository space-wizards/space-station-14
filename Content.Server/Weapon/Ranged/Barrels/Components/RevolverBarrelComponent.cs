using System;
using System.Threading.Tasks;
using Content.Server.Weapon.Ranged.Ammunition.Components;
using Content.Shared.ActionBlocker;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Sound;
using Content.Shared.Verbs;
using Content.Shared.Weapons.Ranged.Barrels.Components;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Players;
using Robust.Shared.Random;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.Weapon.Ranged.Barrels.Components
{
    [RegisterComponent]
    [NetworkedComponent()]
    public sealed class RevolverBarrelComponent : ServerRangedBarrelComponent, ISerializationHooks
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
        private IEntity?[] _ammoSlots = Array.Empty<IEntity?>();

        public override int ShotsLeft => _ammoContainer.ContainedEntities.Count;

        [ViewVariables]
        [DataField("fillPrototype")]
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
            _ammoSlots = new IEntity[_serializedCapacity];
        }

        public override ComponentState GetComponentState(ICommonSession player)
        {
            var slotsSpent = new bool?[Capacity];
            for (var i = 0; i < Capacity; i++)
            {
                slotsSpent[i] = null;
                var ammoEntity = _ammoSlots[i];
                if (ammoEntity != null && ammoEntity.TryGetComponent(out AmmoComponent? ammo))
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
                var entity = Owner.EntityManager.SpawnEntity(_fillPrototype, Owner.Transform.Coordinates);
                _ammoSlots[idx] = entity;
                _ammoContainer.Insert(entity);
                idx++;
            }

            UpdateAppearance();
            Dirty();
        }

        private void UpdateAppearance()
        {
            if (!Owner.TryGetComponent(out AppearanceComponent? appearance))
            {
                return;
            }

            // Placeholder, at this stage it's just here for the RPG
            appearance.SetData(MagazineBarrelVisuals.MagLoaded, ShotsLeft > 0);
            appearance.SetData(AmmoVisuals.AmmoCount, ShotsLeft);
            appearance.SetData(AmmoVisuals.AmmoMax, Capacity);
        }

        public bool TryInsertBullet(IEntity user, IEntity entity)
        {
            if (!entity.TryGetComponent(out AmmoComponent? ammoComponent))
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
                if (slot == null)
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

        public override IEntity? PeekAmmo()
        {
            return _ammoSlots[_currentSlot];
        }

        /// <summary>
        /// Takes a projectile out if possible
        /// IEnumerable just to make supporting shotguns saner
        /// </summary>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public override IEntity? TakeProjectile(EntityCoordinates spawnAt)
        {
            var ammo = _ammoSlots[_currentSlot];
            IEntity? bullet = null;
            if (ammo != null)
            {
                var ammoComponent = ammo.GetComponent<AmmoComponent>();
                bullet = ammoComponent.TakeBullet(spawnAt);
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
                SoundSystem.Play(Filter.Pvs(Owner), _soundEject.GetSound(), Owner, AudioParams.Default.WithVolume(-1));
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
                if (!EntitySystem.Get<ActionBlockerSystem>().CanInteract(user))
                {
                    data.Visibility = VerbVisibility.Invisible;
                    return;
                }

                data.Text = Loc.GetString("spin-revolver-verb-get-data-text");
                if (component.Capacity <= 1)
                {
                    data.Visibility = VerbVisibility.Invisible;
                    return;
                }

                data.Visibility = component.ShotsLeft > 0 ? VerbVisibility.Visible : VerbVisibility.Disabled;
                data.IconTexture = "/Textures/Interface/VerbIcons/refresh.svg.192dpi.png";
            }

            protected override void Activate(IEntity user, RevolverBarrelComponent component)
            {
                component.Spin();
                component.Owner.PopupMessage(user, Loc.GetString("spin-revolver-verb-on-activate"));
            }
        }
    }
}
