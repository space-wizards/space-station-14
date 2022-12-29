using Content.Server.Power.Components;
using Content.Server.Xenoarchaeology.XenoArtifacts.Effects.Components;
using Content.Server.Xenoarchaeology.XenoArtifacts.Events;

namespace Content.Server.Xenoarchaeology.XenoArtifacts.Effects.Systems;

/// <summary>
/// This handles <see cref="ChargeBatteryArtifactComponent"/>
/// </summary>
public sealed class ChargeBatteryArtifactSystem : EntitySystem
{
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<ChargeBatteryArtifactComponent, ArtifactActivatedEvent>(OnActivated);
    }

    private void OnActivated(EntityUid uid, ChargeBatteryArtifactComponent component, ArtifactActivatedEvent args)
    {
        foreach (var battery in _lookup.GetComponentsInRange<BatteryComponent>(Transform(uid).MapPosition, component.Radius))
        {
            battery.CurrentCharge = battery.MaxCharge;
        }
    }
}
