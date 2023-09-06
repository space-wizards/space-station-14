using Content.Server.Radiation.Components;
using Content.Shared.Radiation.Components;
using Content.Shared.Stacks;

namespace Content.Server.Radiation.Systems;

/// <summary>
/// Handles updating <see cref="StackRadiationComponent"/>'s radiation source intensity based on stack size changes.
/// </summary>
public sealed partial class RadiationSystem
{
    private void InitStack()
    {
        SubscribeLocalEvent<StackRadiationComponent, StackCountChangedEvent>(OnStackCountChanged);
    }

    private void OnStackCountChanged(EntityUid uid, StackRadiationComponent comp, StackCountChangedEvent args)
    {
        if (!TryComp<RadiationSourceComponent>(uid, out var source))
            return;

        source.Intensity = comp.BaseIntensity * args.NewCount;
    }
}
