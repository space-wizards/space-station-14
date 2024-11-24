using Content.Server.Tools;
using Content.Shared.Damage.Events;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Systems;
using Content.Shared.Wieldable.Components;
using Robust.Shared.Containers;

/* New Frontiers - Fuscian Changes - modifying the .yml to not be specific to Oni.
This code is licensed under AGPLv3. See AGPLv3.txt */
namespace Content.Server.Abilities.Fuscian
{
    public sealed class FuscianSystem : EntitySystem
    {
        [Dependency] private readonly ToolSystem _toolSystem = default!;
        [Dependency] private readonly SharedGunSystem _gunSystem = default!;

        private const double GunInaccuracyFactor = 0.0; // Vault Station (20x<16x)

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<FuscianComponent, EntInsertedIntoContainerMessage>(OnEntInserted);
            SubscribeLocalEvent<FuscianComponent, EntRemovedFromContainerMessage>(OnEntRemoved);
            SubscribeLocalEvent<FuscianComponent, MeleeHitEvent>(OnFuscianMeleeHit);
            SubscribeLocalEvent<HeldByFuscianComponent, MeleeHitEvent>(OnHeldMeleeHit);
            SubscribeLocalEvent<HeldByFuscianComponent, StaminaMeleeHitEvent>(OnStamHit);
        }

        private void OnEntInserted(EntityUid uid, FuscianComponent component, EntInsertedIntoContainerMessage args)
        {
            var heldComp = EnsureComp<HeldByFuscianComponent>(args.Entity);
            heldComp.Holder = uid;

            if (TryComp<GunComponent>(args.Entity, out var gun))
            {
                // Frontier: adjust penalty for wielded malus (ensuring it's actually wieldable)
                if (TryComp<GunWieldBonusComponent>(args.Entity, out var bonus) && HasComp<WieldableComponent>(args.Entity))
                {
                    //GunWieldBonus values are stored as negative.
                    heldComp.minAngleAdded = (gun.MinAngle + bonus.MinAngle) * GunInaccuracyFactor;
                    heldComp.angleIncreaseAdded = (gun.AngleIncrease + bonus.AngleIncrease) * GunInaccuracyFactor;
                    heldComp.maxAngleAdded = (gun.MaxAngle + bonus.MaxAngle) * GunInaccuracyFactor;
                }
                else
                {
                    heldComp.minAngleAdded = gun.MinAngle * GunInaccuracyFactor;
                    heldComp.angleIncreaseAdded = gun.AngleIncrease * GunInaccuracyFactor;
                    heldComp.maxAngleAdded = gun.MaxAngle * GunInaccuracyFactor;
                }

                gun.MinAngle += heldComp.minAngleAdded;
                gun.AngleIncrease += heldComp.angleIncreaseAdded;
                gun.MaxAngle += heldComp.maxAngleAdded;
                _gunSystem.RefreshModifiers(args.Entity); // Make sure values propagate to modified values (this also dirties the gun for us)
                // End Frontier
            }
        }

        private void OnEntRemoved(EntityUid uid, FuscianComponent component, EntRemovedFromContainerMessage args)
        {
            // Frontier: angle manipulation stored in HeldByFuscianComponent
            if (TryComp<GunComponent>(args.Entity, out var gun) &&
                TryComp<HeldByFuscianComponent>(args.Entity, out var heldComp))
            {
                gun.MinAngle -= heldComp.minAngleAdded;
                gun.AngleIncrease -= heldComp.angleIncreaseAdded;
                gun.MaxAngle -= heldComp.maxAngleAdded;
                _gunSystem.RefreshModifiers(args.Entity); // Make sure values propagate to modified values (this also dirties the gun for us)
            }
            // End Frontier

            RemComp<HeldByFuscianComponent>(args.Entity);
        }

        private void OnFuscianMeleeHit(EntityUid uid, FuscianComponent component, MeleeHitEvent args)
        {
            args.ModifiersList.Add(component.MeleeModifiers);
        }

        private void OnHeldMeleeHit(EntityUid uid, HeldByFuscianComponent component, MeleeHitEvent args)
        {
            if (!TryComp<FuscianComponent>(component.Holder, out var Fuscian))
                return;

            args.ModifiersList.Add(Fuscian.MeleeModifiers);
        }

        private void OnStamHit(EntityUid uid, HeldByFuscianComponent component, StaminaMeleeHitEvent args)
        {
            if (!TryComp<FuscianComponent>(component.Holder, out var Fuscian))
                return;

            args.Multiplier *= Fuscian.StamDamageMultiplier;
        }
    }
}
// End of modified code
