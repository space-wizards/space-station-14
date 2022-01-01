using System;
using Content.Server.Power.Components;
using Content.Server.Projectiles.Components;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Weapons.Ranged.Barrels.Components;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Players;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.ViewVariables;

namespace Content.Server.Weapon.Ranged.Barrels.Components
{
    [RegisterComponent]
    [NetworkedComponent()]
    public sealed class ServerBatteryBarrelComponent : ServerRangedBarrelComponent
    {
        [Dependency] private readonly IEntityManager _entities = default!;

        public override string Name => "BatteryBarrel";

        [DataField("cellSlot", required: true)]
        public ItemSlot CellSlot = new();

        // The minimum change we need before we can fire
        [DataField("lowerChargeLimit")]
        [ViewVariables] private float _lowerChargeLimit = 10;
        [DataField("fireCost")]
        [ViewVariables] private int _baseFireCost = 300;
        // What gets fired
        [DataField("ammoPrototype", customTypeSerializer:typeof(PrototypeIdSerializer<EntityPrototype>))]
        [ViewVariables] private string? _ammoPrototype;

        public BatteryComponent? PowerCell => _entities.GetComponentOrNull<BatteryComponent>(CellSlot.Item);
        private ContainerSlot _ammoContainer = default!;

        public override int ShotsLeft
        {
            get
            {
                if (CellSlot.Item is not {} powerCell)
                {
                    return 0;
                }

                return (int) Math.Ceiling(_entities.GetComponent<BatteryComponent>(powerCell).CurrentCharge / _baseFireCost);
            }
        }

        public override int Capacity
        {
            get
            {
                if (CellSlot.Item is not {} powerCell)
                {
                    return 0;
                }

                return (int) Math.Ceiling(_entities.GetComponent<BatteryComponent>(powerCell).MaxCharge / _baseFireCost);
            }
        }

        private AppearanceComponent? _appearanceComponent;

        public override ComponentState GetComponentState()
        {
            (int, int)? count = (ShotsLeft, Capacity);

            return new BatteryBarrelComponentState(
                FireRateSelector,
                count);
        }

        protected override void Initialize()
        {
            base.Initialize();

            EntitySystem.Get<ItemSlotsSystem>().AddItemSlot(Owner, $"{Name}-powercell-container", CellSlot);

            if (_ammoPrototype != null)
            {
                _ammoContainer = Owner.EnsureContainer<ContainerSlot>($"{Name}-ammo-container");
            }

            if (_entities.TryGetComponent(Owner, out AppearanceComponent? appearanceComponent))
            {
                _appearanceComponent = appearanceComponent;
            }
            Dirty();
        }

        protected override void OnRemove()
        {
            base.OnRemove();
            EntitySystem.Get<ItemSlotsSystem>().RemoveItemSlot(Owner, CellSlot);
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

        public override EntityUid? PeekAmmo()
        {
            // Spawn a dummy entity because it's easier to work with I guess
            // This will get re-used for the projectile
            var ammo = _ammoContainer.ContainedEntity;
            if (ammo == null)
            {
                ammo = _entities.SpawnEntity(_ammoPrototype, _entities.GetComponent<TransformComponent>(Owner).Coordinates);
                _ammoContainer.Insert(ammo.Value);
            }

            return ammo.Value;
        }

        public override EntityUid? TakeProjectile(EntityCoordinates spawnAt)
        {
            var powerCellEntity = CellSlot.Item;

            if (powerCellEntity == null)
            {
                return null;
            }

            var capacitor = _entities.GetComponent<BatteryComponent>(powerCellEntity.Value);
            if (capacitor.CurrentCharge < _lowerChargeLimit)
            {
                return null;
            }

            // Can fire confirmed
            // Multiply the entity's damage / whatever by the percentage of charge the shot has.
            EntityUid? entity;
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
                _ammoContainer.Remove(entity.Value);
                _entities.GetComponent<TransformComponent>(entity.Value).Coordinates = spawnAt;
            }
            else
            {
                entity = _entities.SpawnEntity(_ammoPrototype, spawnAt);
            }

            if (_entities.TryGetComponent(entity.Value, out ProjectileComponent? projectileComponent))
            {
                if (energyRatio < 1.0)
                {
                    projectileComponent.Damage *= energyRatio;
                }
            } else if (_entities.TryGetComponent(entity.Value, out HitscanComponent? hitscanComponent))
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
            return entity.Value;
        }
    }
}
