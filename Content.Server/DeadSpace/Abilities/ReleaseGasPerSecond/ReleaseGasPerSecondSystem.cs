using Content.Shared.DeadSpace.Abilities.ReleaseGasPerSecond;
using Robust.Shared.Timing;
using Content.Server.Atmos.EntitySystems;
using Robust.Server.GameObjects;
using Content.Shared.DeadSpace.Abilities.ReleaseGasPerSecond.Components;

namespace Content.Server.DeadSpace.Abilities.ReleaseGasPerSecond;

public sealed partial class ReleaseGasPerSecondSystem : SharedReleaseGasPerSecondSystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly AtmosphereSystem _atmos = default!;
    [Dependency] private readonly TransformSystem _xform = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ReleaseGasPerSecondComponent, DomainGasEvent>(DoDomain);
    }

    private void DoDomain(EntityUid uid, ReleaseGasPerSecondComponent component, DomainGasEvent args)
    {
        var transform = Transform(uid);
        var indices = _xform.GetGridOrMapTilePosition(uid, transform);
        var tileMix = _atmos.GetTileMixture(transform.GridUid, transform.MapUid, indices, true);

        tileMix?.AdjustMoles(component.GasID, component.MolesInfectionPerDuration);

        component.NextEmitInfection = _timing.CurTime + TimeSpan.FromSeconds(component.DurationEmitInfection);
    }
}
