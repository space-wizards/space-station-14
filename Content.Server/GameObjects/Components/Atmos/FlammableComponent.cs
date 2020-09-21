using System;
using System.Collections.Generic;
using Content.Server.Atmos;
using Content.Server.GameObjects.Components.Temperature;
using Content.Shared.Atmos;
using Content.Shared.Damage;
using Content.Shared.GameObjects.Components.Atmos;
using Content.Shared.GameObjects.Components.Damage;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Atmos
{
    [RegisterComponent]
    public class FlammableComponent : SharedFlammableComponent, ICollideBehavior, IFireAct
    {
        [Dependency] private IEntityManager _entityManager = default!;

        private readonly List<EntityUid> _collided = new List<EntityUid>();

        [ViewVariables(VVAccess.ReadWrite)]
        public bool OnFire { get; private set; }

        [ViewVariables(VVAccess.ReadWrite)]
        public float FireStacks { get; private set; }

        [ViewVariables(VVAccess.ReadWrite)]
        public bool FireSpread { get; private set; } = false;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
            serializer.DataField(this, x => x.FireSpread, "fireSpread", false);
        }

        public void Ignite()
        {
            if (FireStacks > 0 && !OnFire)
            {
                OnFire = true;

            }

            UpdateAppearance();
        }

        public void Extinguish()
        {
            if (OnFire)
            {
                OnFire = false;
                FireStacks = 0;
            }

            _collided.Clear();

            UpdateAppearance();
        }

        public void AdjustFireStacks(float relativeFireStacks)
        {
            FireStacks += relativeFireStacks;
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

            if (!OnFire)
                return;

            if (FireStacks > 0)
            {
                if(Owner.TryGetComponent(out TemperatureComponent temp))
                {
                    temp.ReceiveHeat(50 * FireStacks);
                }

                if (Owner.TryGetComponent(out DamageableComponent damageable))
                {
                    damageable.ChangeDamage(DamageClass.Burn, (int)(FireStacks * 2.5f), false);
                }

                AdjustFireStacks(-0.1f);
            }
            else
            {
                Extinguish();
                return;
            }

            // If we're in an oxygenless environment, put the fire out.
            if (tile?.Air?.GetMoles(Gas.Oxygen) < 1f)
            {
                Extinguish();
                return;
            }

            tile.HotspotExpose(700, 50, true);

            foreach (var uid in _collided.ToArray())
            {
                if (!uid.IsValid() || !_entityManager.EntityExists(uid))
                {
                    _collided.Remove(uid);
                    continue;
                }

                var entity = _entityManager.GetEntity(uid);
                var collidable = Owner.GetComponent<ICollidableComponent>();
                var otherCollidable = entity.GetComponent<ICollidableComponent>();

                if (!collidable.WorldAABB.Intersects(otherCollidable.WorldAABB))
                {
                    _collided.Remove(uid);
                }
            }
        }

        public void CollideWith(IEntity collidedWith)
        {
            if (!collidedWith.TryGetComponent(out FlammableComponent otherFlammable))
                return;

            if (!FireSpread || !otherFlammable.FireSpread)
                return;

            if (OnFire)
            {
                if (otherFlammable.OnFire)
                {
                    var fireSplit = (FireStacks + otherFlammable.FireStacks) / 2;
                    FireStacks = fireSplit;
                    otherFlammable.FireStacks = fireSplit;
                }
                else
                {
                    FireStacks /= 2;
                    otherFlammable.FireStacks += FireStacks;
                    otherFlammable.Ignite();
                }
            } else if (otherFlammable.OnFire)
            {
                otherFlammable.FireStacks /= 2;
                FireStacks += otherFlammable.FireStacks;
                Ignite();
            }
        }

        private void UpdateAppearance()
        {
            if (!Owner.TryGetComponent(out AppearanceComponent appearanceComponent)) return;
            appearanceComponent.SetData(FireVisuals.OnFire, OnFire);
            appearanceComponent.SetData(FireVisuals.FireStacks, FireStacks);
        }

        public void FireAct(float temperature, float volume)
        {
            AdjustFireStacks(3);
            Ignite();
        }
    }
}
