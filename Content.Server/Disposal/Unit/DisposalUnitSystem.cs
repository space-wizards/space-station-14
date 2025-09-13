using Content.Server.Atmos.EntitySystems;
using Content.Shared.Atmos;
using Content.Shared.Disposal.Components;
using Content.Shared.Disposal.Unit;

namespace Content.Server.Disposal.Unit;

public sealed partial class DisposalUnitSystem : SharedDisposalUnitSystem
{
    [Dependency] private readonly SharedTransformSystem _xform = default!;
    [Dependency] private readonly AtmosphereSystem _atmos = default!;

    protected override void HandleAir(Entity<DisposalUnitComponent> ent, TransformComponent xform)
    {
        var air = ent.Comp.Air;
        var indices = _xform.GetGridTilePositionOrDefault((ent, xform));

        if (_atmos.GetTileMixture(xform.GridUid, xform.MapUid, indices, true) is { Temperature: > 0f } environment)
        {
            var transferMoles = 0.1f * (0.25f * Atmospherics.OneAtmosphere * 1.01f - air.Pressure) * air.Volume / (environment.Temperature * Atmospherics.R);

            ent.Comp.Air = environment.Remove(transferMoles);
        }
    }
}
