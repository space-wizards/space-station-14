using Content.Shared.Atmos;
using Content.Shared.Atmos.Components;
using Content.Shared.Atmos.EntitySystems;
using Content.Shared.Body.Components;
using Content.Shared.Body.Prototypes;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Inventory.Events;
using Robust.Shared.Prototypes;
using BreathToolComponent = Content.Shared.Atmos.Components.BreathToolComponent;
using InternalsComponent = Content.Shared.Body.Components.InternalsComponent;

namespace Content.Shared.Body.Systems;

public sealed class LungSystem : EntitySystem
{
    [Dependency] private readonly SharedAtmosphereSystem _atmos = default!;
    [Dependency] private readonly SharedInternalsSystem _internals = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainerSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<LungComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<BreathToolComponent, GotEquippedEvent>(OnGotEquipped);
        SubscribeLocalEvent<BreathToolComponent, GotUnequippedEvent>(OnGotUnequipped);
    }

    private void OnGotUnequipped(Entity<BreathToolComponent> ent, ref GotUnequippedEvent args)
    {
        _atmos.DisconnectInternals(ent);
    }

    private void OnGotEquipped(Entity<BreathToolComponent> ent, ref GotEquippedEvent args)
    {
        if ((args.SlotFlags & ent.Comp.AllowedSlots) == 0)
        {
            return;
        }

        if (TryComp(args.Equipee, out InternalsComponent? internals))
        {
            ent.Comp.ConnectedInternalsEntity = args.Equipee;
            _internals.ConnectBreathTool((args.Equipee, internals), ent);
        }
    }

    private void OnComponentInit(Entity<LungComponent> entity, ref ComponentInit args)
    {
        if (_solutionContainerSystem.EnsureSolution(entity.Owner, entity.Comp.SolutionName, out var solution))
        {
            solution.MaxVolume = 100.0f;
            solution.CanReact = false; // No dexalin lungs
        }
    }

    // TODO: JUST METABOLIZE GASES DIRECTLY DON'T CONVERT TO REAGENTS!!! (Needs Metabolism refactor :B)
    public void GasToReagent(EntityUid uid, LungComponent lung)
    {
        if (!_solutionContainerSystem.ResolveSolution(uid, lung.SolutionName, ref lung.Solution, out var solution))
            return;

        GasToReagent(lung.Air, solution);
        _solutionContainerSystem.UpdateChemicals(lung.Solution.Value);
    }

    /* This should really be moved to somewhere in the atmos system and modernized,
     so that other systems, like CondenserSystem, can use it.
     */
    private void GasToReagent(GasMixture gas, Solution solution)
    {
        foreach (var gasId in Enum.GetValues<Gas>())
        {
            var i = (int) gasId;
            var moles = gas[i];
            if (moles <= 0)
                continue;

            var reagent = _atmos.GasReagents[i];
            if (reagent is null)
                continue;

            var amount = moles * Atmospherics.BreathMolesToReagentMultiplier;
            solution.AddReagent(reagent, amount);
        }
    }

    public Solution GasToReagent(GasMixture gas)
    {
        var solution = new Solution();
        GasToReagent(gas, solution);
        return solution;
    }
}
