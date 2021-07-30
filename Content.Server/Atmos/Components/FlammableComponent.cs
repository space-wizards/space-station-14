using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Content.Server.Alert;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Stunnable.Components;
using Content.Server.Temperature.Components;
using Content.Shared.ActionBlocker;
using Content.Shared.Alert;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Components;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Interaction;
using Content.Shared.Notification.Managers;
using Content.Shared.Temperature;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.Physics;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.Atmos.Components
{
    [RegisterComponent]
    public class FlammableComponent : SharedFlammableComponent, IFireAct, IInteractUsing
    {
        private bool _resisting = false;
        private readonly List<EntityUid> _collided = new();

        [ViewVariables(VVAccess.ReadWrite)]
        public bool OnFire { get; set; }

        [ViewVariables(VVAccess.ReadWrite)]
        public float FireStacks { get; set; }

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("fireSpread")]
        public bool FireSpread { get; private set; } = false;

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("canResistFire")]
        public bool CanResistFire { get; private set; } = false;

        public void Extinguish()
        {
            if (!OnFire) return;
            OnFire = false;
            FireStacks = 0;

            _collided.Clear();

            UpdateAppearance();
        }

        public void AdjustFireStacks(float relativeFireStacks)
        {
            FireStacks = MathF.Min(MathF.Max(-10f, FireStacks + relativeFireStacks), 20f);
            if (OnFire && FireStacks <= 0)
                Extinguish();

            UpdateAppearance();
        }

        public void Update(TileAtmosphere tile)
        {
            // Slowly dry ourselves off if wet.
            if (FireStacks < 0)
            {
                FireStacks = MathF.Min(0, FireStacks + 1);
            }

            Owner.TryGetComponent(out ServerAlertsComponent? status);

            if (!OnFire)
            {
                status?.ClearAlert(AlertType.Fire);
                return;
            }

            status?.ShowAlert(AlertType.Fire);

            if (FireStacks > 0)
            {
                if (Owner.TryGetComponent(out TemperatureComponent? temp))
                {
                    temp.ReceiveHeat(200 * FireStacks);
                }

                if (Owner.TryGetComponent(out IDamageableComponent? damageable))
                {
                    // TODO ATMOS Fire resistance from armor
                    var damage = Math.Min((int) (FireStacks * 2.5f), 10);
                    damageable.ChangeDamage(DamageClass.Burn, damage, false);
                }

                AdjustFireStacks(-0.1f * (_resisting ? 10f : 1f));
            }
            else
            {
                Extinguish();
                return;
            }

            // If we're in an oxygenless environment, put the fire out.
            if (tile.Air?.GetMoles(Gas.Oxygen) < 1f)
            {
                Extinguish();
                return;
            }

            EntitySystem.Get<AtmosphereSystem>().HotspotExpose(tile.GridIndex, tile.GridIndices, 700f, 50f, true);

            var physics = Owner.GetComponent<IPhysBody>();

            foreach (var uid in _collided.ToArray())
            {
                if (!uid.IsValid() || !Owner.EntityManager.EntityExists(uid))
                {
                    _collided.Remove(uid);
                    continue;
                }

                var entity = Owner.EntityManager.GetEntity(uid);
                var otherPhysics = entity.GetComponent<IPhysBody>();

                if (!physics.GetWorldAABB().Intersects(otherPhysics.GetWorldAABB()))
                {
                    _collided.Remove(uid);
                }
            }
        }

        public void UpdateAppearance()
        {
            if (Owner.Deleted || !Owner.TryGetComponent(out AppearanceComponent? appearanceComponent)) return;
            appearanceComponent.SetData(FireVisuals.OnFire, OnFire);
            appearanceComponent.SetData(FireVisuals.FireStacks, FireStacks);
        }

        public void FireAct(float temperature, float volume)
        {
            AdjustFireStacks(3);
            EntitySystem.Get<FlammableSystem>().Ignite(this);
        }

        // This needs some improvements...
        public void Resist()
        {
            if (!OnFire || !EntitySystem.Get<ActionBlockerSystem>().CanInteract(Owner) || _resisting || !Owner.TryGetComponent(out StunnableComponent? stunnable)) return;

            _resisting = true;

            Owner.PopupMessage(Loc.GetString("flammable-component-resist-message"));
            stunnable.Paralyze(2f);

            Owner.SpawnTimer(2000, () =>
            {
                _resisting = false;
                FireStacks -= 3f;
                UpdateAppearance();
            });
        }

        public async Task<bool> InteractUsing(InteractUsingEventArgs eventArgs)
        {
            foreach (var hotItem in eventArgs.Using.GetAllComponents<IHotItem>())
            {
                if (hotItem.IsCurrentlyHot())
                {
                    EntitySystem.Get<FlammableSystem>().Ignite(this);
                    return true;
                }
            }

            return false;
        }
    }
}
