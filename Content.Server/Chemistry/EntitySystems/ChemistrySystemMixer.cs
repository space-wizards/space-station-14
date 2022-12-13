using Content.Server.Chemistry.Components;
using Content.Server.Chemistry.Components.SolutionManager;
using Content.Server.Weapons.Melee;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reaction;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Database;
using Content.Shared.FixedPoint;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.MobState.Components;
using Content.Shared.Weapons.Melee.Events;
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
        Solution? solution = null;
        if (!_solutions.TryGetMixableSolution(args.Target.Value, out solution))
              return;

        _solutions.UpdateChemicals(args.Target.Value, solution, true, component);
    }
}
