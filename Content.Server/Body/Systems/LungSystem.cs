using Content.Server.Atmos.Components;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Body.Components;
using Content.Server.Chemistry.EntitySystems;
using Content.Shared.Atmos;
using Content.Shared.Inventory.Events;

namespace Content.Server.Body.Systems;

public sealed class LungSystem : EntitySystem
{
    [Dependency] private readonly InternalsSystem _internals = default!;
    [Dependency] private readonly SolutionContainerSystem _solutionContainerSystem = default!;
    [Dependency] private readonly AtmosphereSystem _atmosphereSystem = default!;

    public static string LungSolutionName = "Lung";

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<LungComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<BreathToolComponent, GotEquippedEvent>(OnGotEquipped);
        SubscribeLocalEvent<BreathToolComponent, GotUnequippedEvent>(OnGotUnequipped);
    }

    private void OnGotUnequipped(EntityUid uid, BreathToolComponent component, GotUnequippedEvent args)
    {
        _atmosphereSystem.DisconnectInternals(component);
    }

    private void OnGotEquipped(EntityUid uid, BreathToolComponent component, GotEquippedEvent args)
    {

        if ((args.SlotFlags & component.AllowedSlots) != component.AllowedSlots) return;
        component.IsFunctional = true;

        if (TryComp(args.Equipee, out InternalsComponent? internals))
        {
            component.ConnectedInternalsEntity = args.Equipee;
            _internals.ConnectBreathTool(internals, uid);
        }
    }

    private void OnComponentInit(EntityUid uid, LungComponent component, ComponentInit args)
    {
        component.LungSolution = _solutionContainerSystem.EnsureSolution(uid, LungSolutionName);
        component.LungSolution.MaxVolume = 100.0f;
        component.LungSolution.CanReact = false; // No dexalin lungs
    }

    public void GasToReagent(EntityUid uid, LungComponent lung)
    {
        foreach (var gas in Enum.GetValues<Gas>())
        {
            var i = (int) gas;
            var moles = lung.Air.Moles[i];
            if (moles <= 0)
                continue;
            var reagent = _atmosphereSystem.GasReagents[i];
            if (reagent == null) continue;

            var amount = moles * Atmospherics.BreathMolesToReagentMultiplier;
            _solutionContainerSystem.TryAddReagent(uid, lung.LungSolution, reagent, amount, out _);

            // We don't remove the gas from the lung mix,
            // that's the responsibility of whatever gas is being metabolized.
            // Most things will just want to exhale again.
        }
    }
}
