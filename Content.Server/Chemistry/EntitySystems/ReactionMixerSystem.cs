using Content.Shared.Chemistry.Reaction;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Server.Chemistry.Containers.EntitySystems;
using Content.Server.Popups;

namespace Content.Server.Chemistry.EntitySystems;

public sealed partial class ReactionMixerSystem : EntitySystem
{
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly SolutionContainerSystem _solutionContainers = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ReactionMixerComponent, AfterInteractEvent>(OnAfterInteract);
    }

    private void OnAfterInteract(Entity<ReactionMixerComponent> entity, ref AfterInteractEvent args)
    {
        if (!args.Target.HasValue || !args.CanReach)
            return;

        var mixAttemptEvent = new MixingAttemptEvent(entity);
        RaiseLocalEvent(entity, ref mixAttemptEvent);
        if (mixAttemptEvent.Cancelled)
        {
            return;
        }

        if (!_solutionContainers.TryGetMixableSolution(args.Target.Value, out var solution, out _))
            return;

        _popup.PopupEntity(Loc.GetString(entity.Comp.MixMessage, ("mixed", Identity.Entity(args.Target.Value, EntityManager)), ("mixer", Identity.Entity(entity.Owner, EntityManager))), args.User, args.User);

        _solutionContainers.UpdateChemicals(solution.Value, true, entity.Comp);

        var afterMixingEvent = new AfterMixingEvent(entity, args.Target.Value);
        RaiseLocalEvent(entity, afterMixingEvent);
    }
}
