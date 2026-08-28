using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Events;
using Content.Shared.DoAfter;
using Content.Shared.FixedPoint;
using Content.Shared.IdentityManagement;
using Content.Shared.Popups;
using Content.Shared.Verbs;

namespace Content.Shared.Chemistry.EntitySystems;

/// <inheritdoc cref="HarvestableSolutionComponent"/>
public sealed partial class HarvestableSolutionSystem : EntitySystem
{
    [Dependency] private SharedDoAfterSystem _doAfter = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedSolutionContainerSystem _solutionContainer = default!;

    [Dependency] private EntityQuery<RefillableSolutionComponent> _refillableQuery;
    [Dependency] private EntityQuery<HarvestableSolutionComponent> _harvestableQuery;

    [SubscribeLocalEvent]
    private void AddHarvestVerb(Entity<HarvestableSolutionComponent> ent, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (args.Using is not { } container ||
            !args.CanInteract ||
            !_refillableQuery.HasComp(container))
        {
            return;
        }

        var user = args.User;
        args.Verbs.Add(new AlternativeVerb
        {
            Act = () => TryStartHarvest(ent, user, container),
            Text = Loc.GetString(ent.Comp.VerbText),
            Icon = ent.Comp.VerbIcon,
            Priority = 2
        });
    }

    [SubscribeLocalEvent]
    private void OnDoAfter(Entity<HarvestableSolutionComponent> ent, ref HarvestableSolutionDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || args.Args.Used is not { } container)
            return;

        args.Handled = TryHarvest(ent.AsNullable(), args.Args.User, container);
    }

    private void TryStartHarvest(
        Entity<HarvestableSolutionComponent> ent,
        EntityUid user,
        EntityUid container)
    {
        if (!CanHarvest(ent.AsNullable(), user, container))
            return;

        var args = new DoAfterArgs(
            EntityManager,
            user,
            ent.Comp.Duration,
            new HarvestableSolutionDoAfterEvent(),
            ent,
            ent,
            used: container)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            MovementThreshold = 1f
        };

        _doAfter.TryStartDoAfter(args);
    }

    /// <summary>
    /// Checks that the source contains solution and the target has room for it.
    /// </summary>
    public bool CanHarvest(
        Entity<HarvestableSolutionComponent?> ent,
        EntityUid user,
        EntityUid container,
        bool popup = true)
    {
        if (!_harvestableQuery.Resolve(ent, ref ent.Comp, logMissing: false) ||
            !_solutionContainer.TryGetSolution(
                ent.Owner,
                ent.Comp.SolutionName,
                out _,
                out var source,
                errorOnMissing: false) ||
            !_solutionContainer.TryGetRefillableSolution(container, out _, out var target))
        {
            return false;
        }

        var sourceIdentity = Identity.Entity(ent.Owner, EntityManager);
        var targetIdentity = Identity.Entity(container, EntityManager);

        if (source.Volume <= 0)
        {
            if (popup)
            {
                _popup.PopupEntity(
                    Loc.GetString(ent.Comp.EmptyMessage, ("source", sourceIdentity), ("target", targetIdentity)),
                    ent.Owner,
                    user);
            }

            return false;
        }

        if (target.AvailableVolume > 0)
            return true;

        if (popup)
        {
            _popup.PopupEntity(
                Loc.GetString(ent.Comp.TargetFullMessage, ("source", sourceIdentity), ("target", targetIdentity)),
                ent.Owner,
                user);
        }

        return false;
    }

    /// <summary>
    /// Transfers as much as possible from the source solution into the target container.
    /// </summary>
    public bool TryHarvest(
        Entity<HarvestableSolutionComponent?> ent,
        EntityUid user,
        EntityUid container)
    {
        if (!_harvestableQuery.Resolve(ent, ref ent.Comp, logMissing: false) ||
            !CanHarvest(ent, user, container) ||
            !_solutionContainer.TryGetSolution(
                ent.Owner,
                ent.Comp.SolutionName,
                out var sourceEntity,
                out var source,
                errorOnMissing: false) ||
            !_solutionContainer.TryGetRefillableSolution(container, out var targetEntity, out var target))
        {
            return false;
        }

        var quantity = FixedPoint2.Min(source.Volume, target.AvailableVolume);
        var split = _solutionContainer.SplitSolution(sourceEntity.Value, quantity);
        _solutionContainer.TryAddSolution(targetEntity.Value, split);

        _popup.PopupEntity(
            Loc.GetString(
                ent.Comp.SuccessMessage,
                ("source", Identity.Entity(ent.Owner, EntityManager)),
                ("amount", quantity),
                ("target", Identity.Entity(container, EntityManager))),
            ent.Owner,
            user,
            PopupType.Medium);

        return true;
    }
}
