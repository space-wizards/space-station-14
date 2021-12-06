using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Content.Server.Hands.Components;
using Content.Server.Items;
using Content.Server.Weapon.Ranged.Barrels.Components;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Weapons.Ranged.Barrels.Components;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Utility;

namespace Content.Server.Weapon.Ranged.Ammunition.Components
{
    [RegisterComponent]
#pragma warning disable 618
    public sealed class AmmoBoxComponent : Component, IInteractUsing, IUse, IInteractHand, IMapInit, IExamine
#pragma warning restore 618
    {
        [Dependency] private readonly IEntityManager _entities = default!;

        public override string Name => "AmmoBox";

        [DataField("caliber")]
        private BallisticCaliber _caliber = BallisticCaliber.Unspecified;

        [DataField("capacity")]
        public int Capacity
        {
            get => _capacity;
            set
            {
                _capacity = value;
                _spawnedAmmo = new Stack<EntityUid>(value);
            }
        }

        private int _capacity = 30;

        public int AmmoLeft => _spawnedAmmo.Count + _unspawnedCount;
        private Stack<EntityUid> _spawnedAmmo = new();
        private Container _ammoContainer = default!;
        private int _unspawnedCount;

        [DataField("fillPrototype")]
        private string? _fillPrototype;

        protected override void Initialize()
        {
            base.Initialize();
            _ammoContainer = ContainerHelpers.EnsureContainer<Container>(Owner, $"{Name}-container", out var existing);

            if (existing)
            {
                foreach (var entity in _ammoContainer.ContainedEntities)
                {
                    _unspawnedCount--;
                    _spawnedAmmo.Push(entity);
                    _ammoContainer.Insert(entity);
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
            if (_entities.TryGetComponent(Owner, out AppearanceComponent? appearanceComponent))
            {
                appearanceComponent.SetData(MagazineBarrelVisuals.MagLoaded, true);
                appearanceComponent.SetData(AmmoVisuals.AmmoCount, AmmoLeft);
                appearanceComponent.SetData(AmmoVisuals.AmmoMax, _capacity);
            }
        }

        public EntityUid? TakeAmmo()
        {
            if (_spawnedAmmo.TryPop(out var ammo))
            {
                _ammoContainer.Remove(ammo);
                return ammo;
            }

            if (_unspawnedCount > 0)
            {
                ammo = _entities.SpawnEntity(_fillPrototype, _entities.GetComponent<TransformComponent>(Owner).Coordinates);

                // when dumping from held ammo box, this detaches the spawned ammo from the player.
                _entities.GetComponent<TransformComponent>(ammo).AttachParentToContainerOrGrid();

                _unspawnedCount--;
            }

            return ammo;
        }

        public bool TryInsertAmmo(EntityUid user, EntityUid entity)
        {
            if (!_entities.TryGetComponent(entity, out AmmoComponent? ammoComponent))
            {
                return false;
            }

            if (ammoComponent.Caliber != _caliber)
            {
                Owner.PopupMessage(user, Loc.GetString("ammo-box-component-try-insert-ammo-wrong-caliber"));
                return false;
            }

            if (AmmoLeft >= Capacity)
            {
                Owner.PopupMessage(user, Loc.GetString("ammo-box-component-try-insert-ammo-no-room"));
                return false;
            }

            _spawnedAmmo.Push(entity);
            _ammoContainer.Insert(entity);
            UpdateAppearance();
            return true;
        }

        async Task<bool> IInteractUsing.InteractUsing(InteractUsingEventArgs eventArgs)
        {
            if (_entities.HasComponent<AmmoComponent>(eventArgs.Using))
            {
                return TryInsertAmmo(eventArgs.User, eventArgs.Using);
            }

            if (_entities.TryGetComponent(eventArgs.Using, out RangedMagazineComponent? rangedMagazine))
            {
                for (var i = 0; i < Math.Max(10, rangedMagazine.ShotsLeft); i++)
                {
                    if (rangedMagazine.TakeAmmo() is not {Valid: true} ammo)
                    {
                        continue;
                    }

                    if (!TryInsertAmmo(eventArgs.User, ammo))
                    {
                        rangedMagazine.TryInsertAmmo(eventArgs.User, ammo);
                        return true;
                    }
                }

                return true;
            }

            return false;
        }

        private bool TryUse(EntityUid user)
        {
            if (!_entities.TryGetComponent(user, out HandsComponent? handsComponent))
            {
                return false;
            }

            if (TakeAmmo() is not { } ammo)
            {
                return false;
            }

            if (_entities.TryGetComponent(ammo, out ItemComponent? item))
            {
                if (!handsComponent.CanPutInHand(item))
                {
                    TryInsertAmmo(user, ammo);
                    return false;
                }

                handsComponent.PutInHand(item);
            }

            UpdateAppearance();
            return true;
        }

        public void EjectContents(int count)
        {
            var ejectCount = Math.Min(count, Capacity);
            var ejectAmmo = new List<EntityUid>(ejectCount);

            for (var i = 0; i < Math.Min(count, Capacity); i++)
            {
                if (TakeAmmo() is not { } ammo)
                {
                    break;
                }

                ejectAmmo.Add(ammo);
            }

            ServerRangedBarrelComponent.EjectCasings(ejectAmmo);
            UpdateAppearance();
        }

        bool IUse.UseEntity(UseEntityEventArgs eventArgs)
        {
            return TryUse(eventArgs.User);
        }

        bool IInteractHand.InteractHand(InteractHandEventArgs eventArgs)
        {
            return TryUse(eventArgs.User);
        }

        public void Examine(FormattedMessage message, bool inDetailsRange)
        {
            message.AddMarkup("\n" + Loc.GetString("ammo-box-component-on-examine-caliber-description", ("caliber", _caliber)));
            message.AddMarkup("\n" + Loc.GetString("ammo-box-component-on-examine-remaining-ammo-description", ("ammoLeft",AmmoLeft),("capacity", _capacity)));
        }
    }
}
