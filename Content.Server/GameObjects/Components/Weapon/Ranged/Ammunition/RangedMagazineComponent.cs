using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Content.Server.GameObjects.Components.GUI;
using Content.Server.GameObjects.Components.Items.Storage;
using Content.Server.GameObjects.Components.Weapon.Ranged.Barrels;
using Content.Shared.GameObjects.Components.Weapons.Ranged.Barrels;
using Content.Shared.GameObjects.EntitySystems;
using Content.Shared.Interfaces;
using Content.Shared.Interfaces.GameObjects.Components;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Utility;

namespace Content.Server.GameObjects.Components.Weapon.Ranged.Ammunition
{
    [RegisterComponent]
    public class RangedMagazineComponent : Component, IMapInit, IInteractUsing, IUse, IExamine
    {
        public override string Name => "RangedMagazine";

        private readonly Stack<IEntity> _spawnedAmmo = new();
        private Container _ammoContainer;

        public int ShotsLeft => _spawnedAmmo.Count + _unspawnedCount;
        public int Capacity => _capacity;
        [DataField("capacity")]
        private int _capacity = 20;

        public MagazineType MagazineType => _magazineType;
        [DataField("magazineType")]
        private MagazineType _magazineType = MagazineType.Unspecified;
        public BallisticCaliber Caliber => _caliber;
        [DataField("caliber")]
        private BallisticCaliber _caliber = BallisticCaliber.Unspecified;

        private AppearanceComponent _appearanceComponent;

        // If there's anything already in the magazine
        [DataField("fillPrototype")]
        private string _fillPrototype;
        // By default the magazine won't spawn the entity until needed so we need to keep track of how many left we can spawn
        // Generally you probablt don't want to use this
        private int _unspawnedCount;

        void IMapInit.MapInit()
        {
            if (_fillPrototype != null)
            {
                _unspawnedCount += Capacity;
            }
            UpdateAppearance();
        }

        public override void Initialize()
        {
            base.Initialize();
            _ammoContainer = ContainerHelpers.EnsureContainer<Container>(Owner, $"{Name}-magazine", out var existing);

            if (existing)
            {
                if (_ammoContainer.ContainedEntities.Count > Capacity)
                {
                    throw new InvalidOperationException("Initialized capacity of magazine higher than its actual capacity");
                }

                foreach (var entity in _ammoContainer.ContainedEntities)
                {
                    _spawnedAmmo.Push(entity);
                    _unspawnedCount--;
                }
            }

            if (Owner.TryGetComponent(out AppearanceComponent appearanceComponent))
            {
                _appearanceComponent = appearanceComponent;
            }

            _appearanceComponent?.SetData(MagazineBarrelVisuals.MagLoaded, true);
        }

        private void UpdateAppearance()
        {
            _appearanceComponent?.SetData(AmmoVisuals.AmmoCount, ShotsLeft);
            _appearanceComponent?.SetData(AmmoVisuals.AmmoMax, Capacity);
        }

        public bool TryInsertAmmo(IEntity user, IEntity ammo)
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

            if (ShotsLeft >= Capacity)
            {
                Owner.PopupMessage(user, Loc.GetString("Magazine is full"));
                return false;
            }

            _ammoContainer.Insert(ammo);
            _spawnedAmmo.Push(ammo);
            UpdateAppearance();
            return true;
        }

        public IEntity TakeAmmo()
        {
            IEntity ammo = null;
            // If anything's spawned use that first, otherwise use the fill prototype as a fallback (if we have spawn count left)
            if (_spawnedAmmo.TryPop(out var entity))
            {
                ammo = entity;
                _ammoContainer.Remove(entity);
            }
            else if (_unspawnedCount > 0)
            {
                _unspawnedCount--;
                ammo = Owner.EntityManager.SpawnEntity(_fillPrototype, Owner.Transform.Coordinates);
            }

            UpdateAppearance();
            return ammo;
        }

        async Task<bool> IInteractUsing.InteractUsing(InteractUsingEventArgs eventArgs)
        {
            return TryInsertAmmo(eventArgs.User, eventArgs.Using);
        }

        bool IUse.UseEntity(UseEntityEventArgs eventArgs)
        {
            if (!eventArgs.User.TryGetComponent(out HandsComponent handsComponent))
            {
                return false;
            }

            var ammo = TakeAmmo();
            if (ammo == null)
            {
                return false;
            }

            var itemComponent = ammo.GetComponent<ItemComponent>();
            if (!handsComponent.CanPutInHand(itemComponent))
            {
                ammo.Transform.Coordinates = eventArgs.User.Transform.Coordinates;
                ServerRangedBarrelComponent.EjectCasing(ammo);
            }
            else
            {
                handsComponent.PutInHand(itemComponent);
            }

            return true;
        }

        public void Examine(FormattedMessage message, bool inDetailsRange)
        {
            var text = Loc.GetString("It's a [color=white]{0}[/color] magazine of [color=white]{1}[/color] caliber.", MagazineType, Caliber);
            message.AddMarkup(text);
        }
    }
}
