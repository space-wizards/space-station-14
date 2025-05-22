using Content.Server.Body.Components;
using Content.Shared.Body.Organ;
using Content.Shared.Chemistry.EntitySystems;
using Robust.Shared.Prototypes;

namespace Content.Server.Body.Systems;

public sealed class GibsColorSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearanceSystem = default!;
    [Dependency] private readonly BloodstreamSystem _bloodstreamSystem = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainerSystem = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<BloodstreamComponent, ColorGibsEvent>(OnColorGibs);
    }

    private void OnColorGibs(Entity<BloodstreamComponent> ent, ref ColorGibsEvent ev)
    {
        if (ev.Gibs == null || !_solutionContainerSystem.ResolveSolution(ent.Owner, ent.Comp.BloodSolutionName, ref ent.Comp.BloodSolution, out var bloodSolution))
            return;

        var bloodColor = bloodSolution.GetColor(_prototype);
        foreach (var part in ev.Gibs)
        {
            // TODO: It's only organs we want to tint for now, but perhaps we
            // could put blood tint/decal/overlay on all items here?
            if (HasComp<OrganComponent>(part))
            {
                _appearanceSystem.SetData(part, GoreVisuals.ColorTint, bloodColor);
            }
        }
    }
}
