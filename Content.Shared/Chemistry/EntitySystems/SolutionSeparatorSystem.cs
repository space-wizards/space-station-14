using Content.Shared.Chemistry.Components;
using JetBrains.Annotations;

namespace Content.Shared.Chemistry.EntitySystems;

/// <summary>
/// Marks solutions with reagent layers separation after mixing finished.
/// </summary>
[UsedImplicitly]
public class SolutionSeparatorSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SolutionSeparatorComponent, SolutionContainerMixerMixedEvent>(OnMixerMixed);
    }

    private static void OnMixerMixed(EntityUid uid, SolutionSeparatorComponent component, SolutionContainerMixerMixedEvent args)
    {
        args.Solution.Comp.Solution.IsSeparatedByLayers = true;
    }
}
