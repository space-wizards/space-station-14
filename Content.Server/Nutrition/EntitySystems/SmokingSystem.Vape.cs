using Content.Server.Body.Components;
using Content.Server.Body.Systems;
using Content.Server.DoAfter;
using Content.Server.Explosion.EntitySystems;
using Content.Server.Nutrition.Components;
using Content.Server.Popups;
using Content.Server.Power.Components;
using Content.Shared.Administration.Logs;
using Content.Shared.Atmos;
using Content.Shared.Chemistry;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Damage;
using Content.Shared.Database;
using Content.Shared.DoAfter;
using Content.Shared.Emag.Components;
using Content.Shared.Emag.Systems;
using Content.Shared.FixedPoint;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Nutrition;

/// <summary>
/// System for vapes
/// </summary>
namespace Content.Server.Nutrition.EntitySystems;

public sealed partial class SmokingSystem
{
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private readonly DoAfterSystem _doAfterSystem = default!;
    [Dependency] private readonly DamageableSystem _damageableSystem = default!;
    [Dependency] private readonly FoodSystem _foodSystem = default!;
    [Dependency] private readonly ExplosionSystem _explosionSystem = default!;
    [Dependency] private readonly PopupSystem _popupSystem = default!;
    [Dependency] private readonly BloodstreamSystem _bedstreamSystem = default!;

    private void InitializeVapes()
    {
        SubscribeLocalEvent<VapeComponent, AfterInteractEvent>(OnVapeInteraction);
        SubscribeLocalEvent<VapeComponent, VapeDoAfterEvent>(OnVapeDoAfter);
        SubscribeLocalEvent<VapeComponent, GotEmaggedEvent>(OnEmagged);
    }

    private void OnVapeInteraction(EntityUid uid, VapeComponent vape, ref AfterInteractEvent args)
    {
        var user = args.User;
        var target = args.Target;

        if (!args.CanReach
            || !_solutionContainerSystem.TryGetRefillableSolution(uid, out var _, out var solution)
            || !HasComp<BloodstreamComponent>(target)
            || _foodSystem.IsMouthBlocked(target.Value, user))
        {
            return;
        }

        args.Handled = true;

        if (solution.Contents.Count == 0)
        {
            _popupSystem.PopupEntity(Loc.GetString("vape-component-vape-empty"), user, user);
            return;
        }

        var forced = target != user;

        if (forced)
        {
            var userName = Identity.Entity(user, EntityManager);

            _popupSystem.PopupEntity(
                Loc.GetString("vape-component-try-use-vape-forced", ("user", userName)), target.Value,
                target.Value);

            // Log involuntary vaping
            _adminLogger.Add(LogType.ForceFeed, LogImpact.Medium, $"{ToPrettyString(user):user} is forcing {ToPrettyString(target):target} to vape {ToPrettyString(uid)} {SharedSolutionContainerSystem.ToPrettyString(solution)}");
        }
        else
        {
            // Log voluntary vaping
            _adminLogger.Add(LogType.Ingestion, LogImpact.Low, $"{ToPrettyString(target):target} is vape {ToPrettyString(uid)} {SharedSolutionContainerSystem.ToPrettyString(solution)}");
        }

        if (HasComp<EmaggedComponent>(uid) || TryComp<RiggableComponent>(uid, out var riggable) && riggable.IsRigged)
        {
            _explosionSystem.QueueExplosion(uid, "Default", vape.ExplosionIntensity, 0.5f, 3, canCreateVacuum: false);
            EntityManager.QueueDeleteEntity(uid);
            return;
        }

        var doAfterArgs = new DoAfterArgs(EntityManager,
            user,
            forced ? vape.Delay : vape.UserDelay,
            new VapeDoAfterEvent(),
            uid,
            target: target,
            used: uid)
        {
            BreakOnMove = forced,
            BreakOnDamage = true
        };

        _doAfterSystem.TryStartDoAfter(doAfterArgs);
    }

    private void OnVapeDoAfter(EntityUid uid, VapeComponent vape, ref VapeDoAfterEvent args)
    {
        var user = args.User;
        var target = args.Args.Target;

        if (args.Cancelled || args.Handled || target == null)
            return;

        if (!TryComp<BloodstreamComponent>(target, out var bloodstream))
            return;

        if (!_solutionContainerSystem.TryGetRefillableSolution(uid, out var solutionComp, out var solution))
            return;

        if (!_solutionContainerSystem.ResolveSolution(target.Value, bloodstream.ChemicalSolutionName, ref bloodstream.ChemicalSolution, out var chemSolution))
            return;

        var environment = _atmos.GetContainingMixture(target.Value, true, true);

        if (environment == null)
            return;

        var merger = new GasMixture(1) { Temperature = solution.Temperature };
        merger.SetMoles(vape.GasType, solution.Volume.Value / vape.ReductionFactor);

        _atmos.Merge(environment, merger);

        // Move as much solution as we can
        var removedSolution = _solutionContainerSystem.SplitSolution(solutionComp.Value, FixedPoint2.Min(solution.Volume, chemSolution.AvailableVolume));

        // Make vape's solution affect entity
        _bloodstreamSystem.TryAddToChemicals(target.Value, removedSolution, bloodstream);

        _reactiveSystem.DoEntityReaction(target.Value, removedSolution, ReactionMethod.Ingestion);

        // Clean vape's solution
        _solutionContainerSystem.RemoveAllSolution(solutionComp.Value);

        // Smoking kills (your lungs, but there is no organ damage yet)
        _damageableSystem.TryChangeDamage(target.Value, vape.Damage, true);

        if (args.User != args.Target)
        {
            var targetName = Identity.Entity(target.Value, EntityManager);
            var userName = Identity.Entity(user, EntityManager);

            _popupSystem.PopupEntity(
                Loc.GetString("vape-component-vape-success-forced", ("user", userName)), target.Value,
                target.Value);

            _popupSystem.PopupEntity(
                Loc.GetString("vape-component-vape-success-user-forced", ("target", targetName)), user,
                target.Value);

            // Log involuntary vaping
            _adminLogger.Add(LogType.ForceFeed, LogImpact.Medium, $"{ToPrettyString(user):user} forced {ToPrettyString(target):target} to vape {ToPrettyString(uid)} {SharedSolutionContainerSystem.ToPrettyString(solution)}");
        }
        else
        {
            _popupSystem.PopupEntity(
                Loc.GetString("vape-component-vape-success"), target.Value,
                target.Value);

            // Log voluntary vaping
            _adminLogger.Add(LogType.Ingestion, LogImpact.Low, $"{ToPrettyString(target):target} vaped {ToPrettyString(uid)} {SharedSolutionContainerSystem.ToPrettyString(solution)}");
        }

        args.Handled = true;
    }
    private void OnEmagged(Entity<VapeComponent> entity, ref GotEmaggedEvent args)
    {
        args.Handled = true;
    }
}
