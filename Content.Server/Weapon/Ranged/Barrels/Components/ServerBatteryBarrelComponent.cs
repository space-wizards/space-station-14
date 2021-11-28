using System;
using Content.Server.Power.Components;
using Content.Server.Projectiles.Components;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Weapons.Ranged.Barrels.Components;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Players;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.Weapon.Ranged.Barrels.Components
{
    [RegisterComponent]
    [NetworkedComponent()]
    public sealed class ServerBatteryBarrelComponent : ServerRangedBarrelComponent
    {
        public override string Name => "BatteryBarrel";

        [DataField("cellSlot", required: true)]
        public ItemSlot CellSlot = default!;

        // The minimum change we need before we can fire
        [DataField("lowerChargeLimit")]
        [ViewVariables] private float _lowerChargeLimit = 10;
        [DataField("fireCost")]
        [ViewVariables] private int _baseFireCost = 300;
        // What gets fired
        [DataField("ammoPrototype")]
        [ViewVariables] private string? _ammoPrototype;

        public BatteryComponent? PowerCell => CellSlot.Item?.GetComponentOrNull<BatteryComponent>();
        private ContainerSlot _ammoContainer = default!;

        public override int ShotsLeft
        {
            get
            {
                var powerCell = CellSlot.Item;

                if (powerCell == null)
                {
                    return 0;
                }

                return (int) Math.Ceiling(powerCell.GetComponent<BatteryComponent>().CurrentCharge / _baseFireCost);
            }
        }

        public override int Capacity
        {
            get
            {
                var powerCell = CellSlot.Item;

                if (powerCell == null)
                {
                    return 0;
                }

                return (int) Math.Ceiling((float) (powerCell.GetComponent<BatteryComponent>().MaxCharge / _baseFireCost));
            }
        }

        private AppearanceComponent? _appearanceComponent;

        public override ComponentState GetComponentState(ICommonSession player)
        {
            (int, int)? count = (ShotsLeft, Capacity);

            return new BatteryBarrelComponentState(
                FireRateSelector,
                count);
        }

        protected override void Initialize()
        {
            base.Initialize();
            EntitySystem.Get<ItemSlotsSystem>().AddItemSlot(OwnerUid, $"{Name}-powercell-container", CellSlot);

            if (_ammoPrototype != null)
            {
                _ammoContainer = ContainerHelpers.EnsureContainer<ContainerSlot>(Owner, $"{Name}-ammo-container");
            }

            if (Owner.TryGetComponent(out AppearanceComponent? appearanceComponent))
            {
                _appearanceComponent = appearanceComponent;
            }
            Dirty();
        }

        protected override void OnRemove()
        {
            base.OnRemove();
            EntitySystem.Get<ItemSlotsSystem>().RemoveItemSlot(OwnerUid, CellSlot);
        }

        protected override void Startup()
        {
            base.Startup();
            UpdateAppearance();
        }

        public void UpdateAppearance()
        {
            _appearanceComponent?.SetData(MagazineBarrelVisuals.MagLoaded, CellSlot.HasItem);
            _appearanceComponent?.SetData(AmmoVisuals.AmmoCount, ShotsLeft);
            _appearanceComponent?.SetData(AmmoVisuals.AmmoMax, Capacity);
            Dirty();
        }

        public override IEntity PeekAmmo()
        {
            // Spawn a dummy entity because it's easier to work with I guess
            // This will get re-used for the projectile
            var ammo = _ammoContainer.ContainedEntity;
            if (ammo == null)
            {
                ammo = Owner.EntityManager.SpawnEntity(_ammoPrototype, Owner.Transform.Coordinates);
                _ammoContainer.Insert(ammo);
            }

            return ammo;
        }

        public override IEntity? TakeProjectile(EntityCoordinates spawnAt)
        {
            var powerCellEntity = CellSlot.Item;

            if (powerCellEntity == null)
            {
                return null;
            }

            var capacitor = powerCellEntity.GetComponent<BatteryComponent>();
            if (capacitor.CurrentCharge < _lowerChargeLimit)
            {
                return null;
            }

            // Can fire confirmed
            // Multiply the entity's damage / whatever by the percentage of charge the shot has.
            IEntity entity;
            var chargeChange = Math.Min(capacitor.CurrentCharge, _baseFireCost);
            if (capacitor.UseCharge(chargeChange) < _lowerChargeLimit)
            {
                // Handling of funny exploding cells.
                return null;
            }
            var energyRatio = chargeChange / _baseFireCost;

            if (_ammoContainer.ContainedEntity != null)
            {
                entity = _ammoContainer.ContainedEntity;
                _ammoContainer.Remove(entity);
                entity.Transform.Coordinates = spawnAt;
            }
            else
            {
                entity = Owner.EntityManager.SpawnEntity(_ammoPrototype, spawnAt);
            }

            if (entity.TryGetComponent(out ProjectileComponent? projectileComponent))
            {
                if (energyRatio < 1.0)
                {
                    projectileComponent.Damage *= energyRatio;
                }
            } else if (entity.TryGetComponent(out HitscanComponent? hitscanComponent))
            {
                hitscanComponent.Damage *= energyRatio;
                hitscanComponent.ColorModifier = energyRatio;
            }
            else
            {
                throw new InvalidOperationException("Ammo doesn't have hitscan or projectile?");
            }

            Dirty();
            UpdateAppearance();
            return entity;
        }
    }
}
