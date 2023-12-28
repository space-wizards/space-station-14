using Content.Shared.Chemistry.Reaction;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;

namespace Content.Server.Chemistry.EntitySystems;

public sealed partial class ChemistrySystem
{
    public void InitializeMixing()
    {
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

        if (!_solutionContainers.TryGetMixableSolution(args.Target.Value, out var solution))
            return;

        _popup.PopupEntity(Loc.GetString(entity.Comp.MixMessage, ("mixed", Identity.Entity(args.Target.Value, EntityManager)), ("mixer", Identity.Entity(entity.Owner, EntityManager))), args.User, args.User);

        _solutionContainers.UpdateChemicals(solution.Value, true, entity.Comp);

        var afterMixingEvent = new AfterMixingEvent(entity, args.Target.Value);
        RaiseLocalEvent(entity, afterMixingEvent);
    }
}
