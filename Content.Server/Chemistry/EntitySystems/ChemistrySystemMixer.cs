using Content.Server.Xenoarchaeology.XenoArtifacts.Triggers.Components;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reaction;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Robust.Shared.Player;

namespace Content.Server.Chemistry.EntitySystems;

public sealed partial class ChemistrySystem
{
    public void InitializeMixing()
    {
        SubscribeLocalEvent<ReactionMixerComponent, AfterInteractEvent>(OnAfterInteract);
    }

    private void OnAfterInteract(EntityUid uid, ReactionMixerComponent component, AfterInteractEvent args)
    {
        if (!args.Target.HasValue || !args.CanReach)
            return;

        var mixAttemptEvent = new MixingAttemptEvent(uid);
        RaiseLocalEvent(uid, ref mixAttemptEvent);
        if(mixAttemptEvent.Cancelled)
        {
            return;
        }

        Solution? solution = null;
        if (!_solutions.TryGetMixableSolution(args.Target.Value, out solution))
              return;

        _popup.PopupEntity(Loc.GetString(component.MixMessage, ("mixed", Identity.Entity(args.Target.Value, EntityManager)), ("mixer", Identity.Entity(uid, EntityManager))), args.User, args.User); ;

        _solutions.UpdateChemicals(args.Target.Value, solution, true, component);

        var afterMixingEvent = new AfterMixingEvent(uid, args.Target.Value);
        RaiseLocalEvent(uid, afterMixingEvent);
    }
}
