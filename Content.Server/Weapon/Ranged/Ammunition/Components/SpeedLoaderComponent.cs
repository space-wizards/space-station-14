using System.Collections.Generic;
using System.Threading.Tasks;
using Content.Server.Hands.Components;
using Content.Server.Items;
using Content.Server.Weapon.Ranged.Barrels.Components;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Weapons.Ranged.Barrels.Components;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.Weapon.Ranged.Ammunition.Components
{
    /// <summary>
    /// Used to load certain ranged weapons quickly
    /// </summary>
    [RegisterComponent]
    public class SpeedLoaderComponent : Component, IAfterInteract, IInteractUsing, IMapInit, IUse
    {
        public override string Name => "SpeedLoader";

        [DataField("caliber")]
        private BallisticCaliber _caliber = BallisticCaliber.Unspecified;
        public int Capacity => _capacity;
        [DataField("capacity")]
        private int _capacity = 6;
        private Container _ammoContainer = default!;
        private Stack<IEntity> _spawnedAmmo = new();
        private int _unspawnedCount;

        public int AmmoLeft => _spawnedAmmo.Count + _unspawnedCount;

        [DataField("fillPrototype")]
        private string? _fillPrototype = default;

        protected override void Initialize()
        {
            base.Initialize();
            _ammoContainer = ContainerHelpers.EnsureContainer<Container>(Owner, $"{Name}-container", out var existing);

            if (existing)
            {
                foreach (var ammo in _ammoContainer.ContainedEntities)
                {
                    _unspawnedCount--;
                    _spawnedAmmo.Push(ammo);
                }
            }
        }

        void IMapInit.MapInit()
        {
            _unspawnedCount += _capacity;
            UpdateAppearance();
        }

        private void UpdateAppearance()
        {
            if (Owner.TryGetComponent(out AppearanceComponent? appearanceComponent))
            {
                appearanceComponent?.SetData(MagazineBarrelVisuals.MagLoaded, true);
                appearanceComponent?.SetData(AmmoVisuals.AmmoCount, AmmoLeft);
                appearanceComponent?.SetData(AmmoVisuals.AmmoMax, Capacity);
            }
        }

        public bool TryInsertAmmo(IEntity user, IEntity entity)
        {
            if (!entity.TryGetComponent(out AmmoComponent? ammoComponent))
            {
                return false;
            }

            if (ammoComponent.Caliber != _caliber)
            {
                Owner.PopupMessage(user, Loc.GetString("speed-loader-component-try-insert-ammo-wrong-caliber"));
                return false;
            }

            if (AmmoLeft >= Capacity)
            {
                Owner.PopupMessage(user, Loc.GetString("speed-loader-component-try-insert-ammo-no-room"));
                return false;
            }

            _spawnedAmmo.Push(entity);
            _ammoContainer.Insert(entity);
            UpdateAppearance();
            return true;

        }

        private bool UseEntity(IEntity user)
        {
            if (!user.TryGetComponent(out HandsComponent? handsComponent))
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
                ServerRangedBarrelComponent.EjectCasing(ammo);
            }
            else
            {
                handsComponent.PutInHand(itemComponent);
            }

            UpdateAppearance();
            return true;
        }

        private IEntity? TakeAmmo()
        {
            if (_spawnedAmmo.TryPop(out var entity))
            {
                _ammoContainer.Remove(entity);
                return entity;
            }

            if (_unspawnedCount > 0)
            {
                entity = Owner.EntityManager.SpawnEntity(_fillPrototype, Owner.Transform.Coordinates);
                _unspawnedCount--;
            }

            return entity;
        }

        async Task<bool> IAfterInteract.AfterInteract(AfterInteractEventArgs eventArgs)
        {
            if (eventArgs.Target == null)
            {
                return false;
            }

            // This area is dirty but not sure of an easier way to do it besides add an interface or somethin
            var changed = false;

            if (eventArgs.Target.TryGetComponent(out RevolverBarrelComponent? revolverBarrel))
            {
                for (var i = 0; i < Capacity; i++)
                {
                    var ammo = TakeAmmo();
                    if (ammo == null)
                    {
                        break;
                    }

                    if (revolverBarrel.TryInsertBullet(eventArgs.User, ammo))
                    {
                        changed = true;
                        continue;
                    }

                    // Take the ammo back
                    TryInsertAmmo(eventArgs.User, ammo);
                    break;
                }
            } else if (eventArgs.Target.TryGetComponent(out BoltActionBarrelComponent? boltActionBarrel))
            {
                for (var i = 0; i < Capacity; i++)
                {
                    var ammo = TakeAmmo();
                    if (ammo == null)
                    {
                        break;
                    }

                    if (boltActionBarrel.TryInsertBullet(eventArgs.User, ammo))
                    {
                        changed = true;
                        continue;
                    }

                    // Take the ammo back
                    TryInsertAmmo(eventArgs.User, ammo);
                    break;
                }

            }

            if (changed)
            {
                UpdateAppearance();
            }

            return true;
        }

        async Task<bool> IInteractUsing.InteractUsing(InteractUsingEventArgs eventArgs)
        {
            return TryInsertAmmo(eventArgs.User, eventArgs.Using);
        }

        bool IUse.UseEntity(UseEntityEventArgs eventArgs)
        {
            return UseEntity(eventArgs.User);
        }
    }
}
