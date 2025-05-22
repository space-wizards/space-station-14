using Content.Server.Body.Components;
using Content.Shared.Body.Components;
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
        SubscribeLocalEvent<BloodstreamComponent, ColorGibsEvent>(OnColorGibs);
        SubscribeLocalEvent<GibSplatterComponent, ColorGibPartEvent>(OnColorGibPart);
    }

    private void OnColorGibs(Entity<BloodstreamComponent> ent, ref ColorGibsEvent ev)
    {
        if (ev.Gibs == null || !_solutionContainerSystem.ResolveSolution(ent.Owner, ent.Comp.BloodSolutionName, ref ent.Comp.BloodSolution, out var bloodSolution))
            return;

        var bloodColor = bloodSolution.GetColor(_prototype);
        foreach (var part in ev.Gibs)
        {
            var colorGibPartEvent = new ColorGibPartEvent { GibColor = bloodColor };
            RaiseLocalEvent(part, ref colorGibPartEvent);
        }
    }

    private void OnColorGibPart(Entity<GibSplatterComponent> ent, ref ColorGibPartEvent ev)
    {
        _appearanceSystem.SetData(ent.Owner, GoreVisuals.ColorTint, ev.GibColor);
    }
}
