using Content.Server.DoAfter;
using Content.Server.Explosion.EntitySystems;
using Content.Server.Nutrition.Components;
using Content.Server.Popups;
using Content.Shared.Atmos;
using Content.Shared.Body.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.DoAfter;
using Content.Shared.Emag.Systems;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Nutrition;
using Content.Shared.Nutrition.EntitySystems;

/// <summary>
/// System for vapes
/// </summary>
namespace Content.Server.Nutrition.EntitySystems
{
    public sealed partial class SmokingSystem
    {
        [Dependency] private DoAfterSystem _doAfterSystem = default!;
        [Dependency] private DamageableSystem _damageableSystem = default!;
        [Dependency] private EmagSystem _emag = default!;
        [Dependency] private IngestionSystem _ingestion = default!;
        [Dependency] private ExplosionSystem _explosionSystem = default!;
        [Dependency] private PopupSystem _popupSystem = default!;

        [SubscribeLocalEvent]
        private void OnVapeActivatedEvent(Entity<VapeComponent> entity, ref ActivateInWorldEvent args)
        {
            if (args.Handled)
                return;

            args.Handled = TryUseVape(entity, args.User, args.User);
        }

        [SubscribeLocalEvent]
        private void OnVapeInteraction(Entity<VapeComponent> entity, ref AfterInteractEvent args)
        {
            if (args.Handled || !args.CanReach)
                return;

            args.Handled = TryUseVape(entity, args.User, args.Target);
        }

        private bool TryUseVape(Entity<VapeComponent> entity, EntityUid user, EntityUid? target)
        {
            var delay = entity.Comp.Delay;
            var forced = true;
            var exploded = false;

            if (!_solutionContainerSystem.TryGetRefillableSolution(entity.Owner, out _, out var solution)
                || !HasComp<BloodstreamComponent>(target)
                || !_ingestion.HasMouthAvailable(user, target.Value)
                )
            {
                return false;
            }

            if (solution.Contents.Count == 0)
            {
                _popupSystem.PopupEntity(
                    Loc.GetString("vape-component-vape-empty"), target.Value,
                    user);
                return false;
            }

            if (target == user)
            {
                delay = entity.Comp.UserDelay;
                forced = false;
            }

            if (entity.Comp.ExplodeOnUse || _emag.CheckFlag(entity, EmagType.Interaction))
            {
                _explosionSystem.QueueExplosion(entity.Owner, "Default", entity.Comp.ExplosionIntensity, 0.5f, 3, canCreateVacuum: false);
                Del(entity);
                exploded = true;
            }
            else
            {
                // All vapes explode if they contain anything other than pure water???
                // WTF is this? Why is this? Am I going insane?
                // Who the fuck vapes pure water?
                // If this isn't how this is meant to work and this is meant to be for vapes with plasma or something,
                // just re-use the existing RiggableSystem.
                foreach (var name in solution.Contents)
                {
                    if (name.Reagent.Prototype != entity.Comp.SolutionNeeded)
                    {
                        exploded = true;
                        _explosionSystem.QueueExplosion(entity.Owner, "Default", entity.Comp.ExplosionIntensity, 0.5f, 3, canCreateVacuum: false);
                        Del(entity);
                        break;
                    }
                }
            }

            if (forced)
            {
                var targetName = Identity.Entity(target.Value, EntityManager);
                var userName = Identity.Entity(user, EntityManager);

                _popupSystem.PopupEntity(
                    Loc.GetString("vape-component-try-use-vape-forced", ("user", userName)), target.Value,
                    target.Value);

                _popupSystem.PopupEntity(
                    Loc.GetString("vape-component-try-use-vape-forced-user", ("target", targetName)), user,
                    user);
            }
            else
            {
                _popupSystem.PopupEntity(
                    Loc.GetString("vape-component-try-use-vape"), user,
                    user);
            }

            if (!exploded)
            {
                var vapeDoAfterEvent = new VapeDoAfterEvent(solution, forced);
                _doAfterSystem.TryStartDoAfter(new DoAfterArgs(EntityManager, user, delay, vapeDoAfterEvent, entity.Owner, target: target, used: entity.Owner)
                {
                    BreakOnMove = false,
                    BreakOnDamage = true
                });
            }
            return true;
        }

        [SubscribeLocalEvent]
        private void OnVapeDoAfter(Entity<VapeComponent> entity, ref VapeDoAfterEvent args)
        {
            if (args.Cancelled || args.Handled || args.Args.Target == null)
                return;

            var environment = _atmos.GetContainingMixture(args.Args.Target.Value, true, true);
            if (environment == null)
            {
                return;
            }

            //Smoking kills(your lungs, but there is no organ damage yet)
            _damageableSystem.TryChangeDamage(args.Args.Target.Value, entity.Comp.Damage, true);

            var merger = new GasMixture(1) { Temperature = args.Solution.Temperature };
            merger.SetMoles(entity.Comp.GasType, args.Solution.Volume.Value / entity.Comp.ReductionFactor);

            _atmos.Merge(environment, merger);

            args.Solution.RemoveAllSolution();

            if (args.Forced)
            {
                var targetName = Identity.Entity(args.Args.Target.Value, EntityManager);
                var userName = Identity.Entity(args.Args.User, EntityManager);

                _popupSystem.PopupEntity(
                    Loc.GetString("vape-component-vape-success-forced", ("user", userName)), args.Args.Target.Value,
                    args.Args.Target.Value);

                _popupSystem.PopupEntity(
                    Loc.GetString("vape-component-vape-success-user-forced", ("target", targetName)), args.Args.User,
                    args.Args.Target.Value);
            }
            else
            {
                _popupSystem.PopupEntity(
                    Loc.GetString("vape-component-vape-success"), args.Args.Target.Value,
                    args.Args.Target.Value);
            }
        }

        [SubscribeLocalEvent]
        private void OnEmagged(Entity<VapeComponent> entity, ref GotEmaggedEvent args)
        {
            if (!_emag.CompareFlag(args.Type, EmagType.Interaction))
                return;

            if (_emag.CheckFlag(entity, EmagType.Interaction))
                return;

            args.Handled = true;
        }
    }
}
