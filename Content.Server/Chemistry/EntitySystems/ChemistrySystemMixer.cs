using Content.Server.Xenoarchaeology.XenoArtifacts.Triggers.Components;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reaction;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;

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
        Solution? solution = null;
        if (!_solutions.TryGetMixableSolution(args.Target.Value, out solution))
              return;

        _solutions.UpdateChemicals(args.Target.Value, solution, true, component);

        var ev = new AfterMixingEvent(uid, args.Target.Value);
        RaiseLocalEvent(uid, ev);
    }
}
