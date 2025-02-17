using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Events;
using Content.Shared.DoAfter;
using Content.Shared.IdentityManagement;
using Content.Shared.Popups;
using Content.Shared.Verbs;

namespace Content.Shared.Chemistry.EntitySystems;

/// <inheritdoc cref="HarvestableSolutionComponent"/>
public sealed class HarvestableSolutionSystem : EntitySystem
{
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainer = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HarvestableSolutionComponent, GetVerbsEvent<AlternativeVerb>>(AddHarvestVerb);
        SubscribeLocalEvent<HarvestableSolutionComponent, HarvestableSolutionDoAfterEvent>(OnDoAfter);
    }

    private void AddHarvestVerb(Entity<HarvestableSolutionComponent> entity, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (args.Using == null ||
             !args.CanInteract ||
             !EntityManager.HasComponent<RefillableSolutionComponent>(args.Using.Value))
            return;

        var user = args.User;
        var used = args.Using.Value;
        AlternativeVerb verb = new()
        {
            Act = () =>
            {
                TryStartHarvest(entity, user, used);
            },
            Text = Loc.GetString(entity.Comp.VerbText),
            Icon = entity.Comp.VerbIcon,
            Priority = 2
        };
        args.Verbs.Add(verb);
    }

    private void OnDoAfter(Entity<HarvestableSolutionComponent> entity, ref HarvestableSolutionDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || args.Args.Used == null)
            return;

        args.Handled = TryHarvest(entity.AsNullable(), args.Args.User, args.Args.Used.Value);
    }

    private bool TryStartHarvest(Entity<HarvestableSolutionComponent> entity, EntityUid userUid, EntityUid containerUid)
    {
        var doAfterArgs = new DoAfterArgs(EntityManager,
            userUid,
            entity.Comp.Duration,
            new HarvestableSolutionDoAfterEvent(),
            entity,
            entity,
            used: containerUid)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            MovementThreshold = 1.0f,
        };

        return _doAfter.TryStartDoAfter(doAfterArgs);
    }

    /// <summary>
    /// Attempts to harvest as much solution as possible from the target entity and place it
    /// into the container.
    /// </summary>
    /// <param name="entity">Target entity from which to harvest.</param>
    /// <param name="userUid">Entity performing the harvest action.</param>
    /// <param name="containerUid">Container entity that the solution will be transferred into.
    /// The harvest will fail if this entity does not have a <see cref="RefillableSolutionComponent"/>.</param>
    /// <returns>True if any solution is harvested, otherwise false.</returns>
    public bool TryHarvest(Entity<HarvestableSolutionComponent?> entity, EntityUid userUid, EntityUid containerUid)
    {
        if (!Resolve(entity, ref entity.Comp, logMissing: false))
            return false;

        if (!_solutionContainer.TryGetSolution(entity.Owner,
            entity.Comp.Solution,
            out var solutionEnt,
            out var solution,
            errorOnMissing: false))
        {
            return false;
        }

        if (!_solutionContainer.TryGetRefillableSolution(containerUid, out var targetSoln, out var targetSolution))
            return false;

        var quantity = solution.Volume;
        var sourceIdentity = Identity.Entity(entity.Owner, EntityManager);
        var targetIdentity = Identity.Entity(containerUid, EntityManager);
        if (quantity == 0)
        {
            _popup.PopupClient(Loc.GetString(entity.Comp.EmptyMessage,
                ("source", sourceIdentity),
                ("target", targetIdentity)),
                entity.Owner,
                userUid);
            return false;
        }

        if (targetSolution.AvailableVolume <= 0)
        {
            // Target container is full
            _popup.PopupClient(Loc.GetString(entity.Comp.TargetFullMessage,
                ("target", targetIdentity)),
                entity.Owner,
                userUid);
            return false;
        }

        if (quantity > targetSolution.AvailableVolume)
            quantity = targetSolution.AvailableVolume;

        var split = _solutionContainer.SplitSolution(solutionEnt.Value, quantity);
        _solutionContainer.TryAddSolution(targetSoln.Value, split);

        _popup.PopupClient(Loc.GetString(entity.Comp.SuccessMessage,
            ("source", sourceIdentity),
            ("amount", quantity),
            ("target", targetIdentity)),
            entity.Owner,
            userUid,
            PopupType.Medium);

        return true;
    }
}
