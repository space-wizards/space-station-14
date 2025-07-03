using Content.Server.Body.Components;
using Content.Shared.Body.Components;
using Content.Shared.Body.Events;
using Content.Shared.Chemistry.EntitySystems;
using Robust.Shared.Prototypes;

namespace Content.Server.Body.Systems;

public sealed class GibsColorSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearanceSystem = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainerSystem = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    public override void Initialize()
    {
        base.Initialize();

        // This needs to run before OnBeingGibbed() in BloodstreamSystem
        // because OnBeingGibbed() in BloodstreamSystem spills all the blood on
        // the floor, which prevents THIS function from getting the blood color
        // (since it's all spilled on the floor).
        // If you want to create more entities with custom colored gibs,
        // create more of these subscriptions to BeingGibbedEvent with their own solutions + components.
        SubscribeLocalEvent<SlimeBloodComponent, BeingGibbedEvent>(OnBeingGibbed, before:[typeof(BloodstreamSystem)]);

        SubscribeLocalEvent<SlimeGibSplatterComponent, ColorGibPartEvent>(OnColorGibPart);
    }

    private void OnBeingGibbed(Entity<SlimeBloodComponent> ent, ref BeingGibbedEvent ev)
    {
        if (!TryComp<BloodstreamComponent>(ent, out var bloodstream))
            return;

        if (!_solutionContainerSystem.ResolveSolution(ent.Owner, bloodstream.BloodSolutionName, ref bloodstream.BloodSolution, out var bloodSolution))
            return;

        var bloodColor = bloodSolution.GetColor(_prototype);
        foreach (var part in ev.GibbedParts)
        {
            var colorGibPartEvent = new ColorGibPartEvent { GibColor = bloodColor };
            RaiseLocalEvent(part, ref colorGibPartEvent);
        }
    }

    private void OnColorGibPart(Entity<SlimeGibSplatterComponent> ent, ref ColorGibPartEvent ev)
    {
        _appearanceSystem.SetData(ent.Owner, GoreVisuals.ColorTint, ev.GibColor);
    }
}
