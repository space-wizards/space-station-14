#nullable enable
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Content.Server.GameObjects.Components.GUI;
using Content.Server.GameObjects.Components.Items.Storage;
using Content.Server.GameObjects.Components.Power;
using Content.Server.GameObjects.Components.Projectiles;
using Content.Shared.Audio;
using Content.Shared.Damage;
using Content.Shared.GameObjects.Components.Weapons.Ranged;
using Content.Shared.GameObjects.Components.Weapons.Ranged.Barrels;
using Content.Shared.GameObjects.EntitySystems;
using Content.Shared.GameObjects.Verbs;
using Content.Shared.Interfaces.GameObjects.Components;
using Robust.Server.GameObjects;
using Robust.Server.GameObjects.Components.Container;
using Robust.Server.GameObjects.EntitySystems;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Weapon.Ranged
{
    [RegisterComponent]
    [ComponentReference(typeof(SharedRangedWeaponComponent))]
    [ComponentReference(typeof(SharedBatteryBarrelComponent))]
    public sealed class ServerBatteryBarrelComponent : SharedBatteryBarrelComponent
    {
        
        [ViewVariables] public IEntity PowerCellEntity => _powerCellContainer.ContainedEntity;
        public BatteryComponent? PowerCell => _powerCellContainer.ContainedEntity?.GetComponent<BatteryComponent>();
        private ContainerSlot _powerCellContainer = default!;
        [ViewVariables] private bool _powerCellRemovable;
        
        /// <summary>
        ///     The pre-spawned prototype to use
        /// </summary>
        private string? _powerCellPrototype;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(ref _powerCellPrototype, "powerCellPrototype", null);
            serializer.DataField(ref _powerCellRemovable, "powerCellRemovable", false);
        }
        
        public override void UpdateAppearance()
        {
            if (!Owner.TryGetComponent(out AppearanceComponent? appearanceComponent))
                return;

            var count = (int) MathF.Ceiling(PowerCell?.CurrentCharge / BaseFireCost ?? 0);
            var max = (int) MathF.Ceiling(PowerCell?.MaxCharge / BaseFireCost ?? 0);
            
            appearanceComponent.SetData(MagazineBarrelVisuals.MagLoaded, PowerCell != null);
            appearanceComponent.SetData(AmmoVisuals.AmmoCount, count);
            appearanceComponent.SetData(AmmoVisuals.AmmoMax, max);
        }

        public override ComponentState GetComponentState()
        {
            (float currentCharge, float maxCharge)? cell;
            
            if (PowerCell == null)
            {
                cell = null;
            }
            else
            {
                var powerCell = PowerCell;
                cell = (powerCell.CurrentCharge, powerCell.MaxCharge);
            }

            return new BatteryBarrelComponentState(
                Selector,
                cell);
        }

        public override void Initialize()
        {
            base.Initialize();
            _powerCellContainer = ContainerManagerComponent.Ensure<ContainerSlot>($"{Name}-powercell-container", Owner, out var existing);
            if (!existing && _powerCellPrototype != null)
            {
                var powerCellEntity = Owner.EntityManager.SpawnEntity(_powerCellPrototype, Owner.Transform.Coordinates);
                _powerCellContainer.Insert(powerCellEntity);
                Dirty();
            }
        }

        public bool TryInsertPowerCell(IEntity entity)
        {
            if (!entity.HasComponent<BatteryComponent>())
                return false;
            
            if (_powerCellContainer.ContainedEntity != null)
                return false;

            if (SoundPowerCellInsert != null)
                EntitySystem.Get<AudioSystem>().PlayFromEntity(SoundPowerCellInsert, Owner, AudioHelpers.WithVariation(CellInsertVariation).WithVolume(CellInsertVolume));

            _powerCellContainer.Insert(entity);

            Dirty();
            return true;
        }

        public override bool UseEntity(UseEntityEventArgs eventArgs)
        {
            return TryEjectCell(eventArgs.User);
        }

        private bool TryEjectCell(IEntity user)
        {
            var cell = PowerCell;
            if (cell == null ||
                !_powerCellRemovable ||
                !user.TryGetComponent(out HandsComponent? hands))
            {
                return false;
            }
            
            if (!_powerCellContainer.Remove(cell.Owner))
                return false;

            if (!hands.PutInHand(cell.Owner.GetComponent<ItemComponent>()))
                cell.Owner.Transform.Coordinates = user.Transform.Coordinates;
            
            if (SoundPowerCellEject != null)
                EntitySystem.Get<AudioSystem>().PlayFromEntity(SoundPowerCellEject, Owner, AudioHelpers.WithVariation(CellEjectVariation).WithVolume(CellEjectVolume));
            
            Dirty();
            return true;
        }

        protected override bool TryShoot(Angle angle)
        {
            if (!base.TryShoot(angle))
                return false;

            var battery = PowerCell;
            if (battery == null)
                return false;

            if (battery.CurrentCharge < LowerChargeLimit)
                return false;

            // Can fire confirmed
            // Multiply the entity's damage / whatever by the percentage of charge the shot has.
            var chargeChange = Math.Min(battery.CurrentCharge, BaseFireCost);
            battery.UseCharge(chargeChange);

            var shooter = Shooter();
            var energyRatio = chargeChange / BaseFireCost;

            if (AmmoIsHitscan)
            {
                var prototype = IoCManager.Resolve<IPrototypeManager>().Index<HitscanPrototype>(AmmoPrototype);
                EntitySystem.Get<SharedRangedWeaponSystem>().ShootHitscan(Shooter(), this, prototype, angle, energyRatio, energyRatio);
            }
            else
            {
                var entity = Owner.EntityManager.SpawnEntity(AmmoPrototype, Owner.Transform.MapPosition);
                var ammoComponent = entity.GetComponent<SharedAmmoComponent>();
                var projectileComponent = entity.GetComponent<ProjectileComponent>();
                
                if (energyRatio < 1.0)
                {
                    var newDamages = new Dictionary<DamageType, int>(projectileComponent.Damages.Count);
                    foreach (var (damageType, damage) in projectileComponent.Damages)
                    {
                        newDamages.Add(damageType, (int) (damage * energyRatio));
                    }

                    projectileComponent.Damages = newDamages;
                }
                
                EntitySystem.Get<SharedRangedWeaponSystem>().ShootAmmo(shooter, this, angle, ammoComponent);
                EntitySystem.Get<SharedRangedWeaponSystem>().MuzzleFlash(shooter, this, angle, alphaRatio: energyRatio);
            }
            
            return true;
        }

        public override async Task<bool> InteractUsing(InteractUsingEventArgs eventArgs)
        {
            return TryInsertPowerCell(eventArgs.Using);
        }
        
        [Verb]
        public sealed class EjectCellVerb : Verb<ServerBatteryBarrelComponent>
        {
            protected override void GetData(IEntity user, ServerBatteryBarrelComponent component, VerbData data)
            {
                if (!ActionBlockerSystem.CanInteract(user) || !component._powerCellRemovable)
                {
                    data.Visibility = VerbVisibility.Invisible;
                    return;
                }

                if (component.PowerCell == null)
                {
                    data.Text = Loc.GetString("Eject cell (cell missing)");
                    data.Visibility = VerbVisibility.Disabled;
                }
                else
                {
                    data.Text = Loc.GetString("Eject cell");
                }
            }

            protected override void Activate(IEntity user, ServerBatteryBarrelComponent component)
            {
                component.TryEjectCell(user);
            }
        }
    }
}