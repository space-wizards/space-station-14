using Content.Server.Atmos.EntitySystems;
using Content.Server.Xenoarchaeology.Artifact.XAE.Components;
using Content.Shared.Atmos;
using Content.Shared.Xenoarchaeology.Artifact;
using Content.Shared.Xenoarchaeology.Artifact.XAE;
using Robust.Server.GameObjects;
using Robust.Shared.Collections;
using Robust.Shared.Map.Components;

namespace Content.Server.Xenoarchaeology.Artifact.XAE;

/// <summary>
/// System for xeno artifact effect that creates certain atmospheric gas on artifact tile / adjacent tiles.
/// </summary>
public sealed class XAECreateGasSystem : BaseXAESystem<XAECreateGasComponent>
{
    [Dependency] private readonly AtmosphereSystem _atmosphere = default!;
    [Dependency] private readonly TransformSystem _transform = default!;
    [Dependency] private readonly MapSystem _map = default!;

    protected override void OnActivated(Entity<XAECreateGasComponent> ent, ref XenoArtifactNodeActivatedEvent args)
    {
        var grid = _transform.GetGrid(args.Coordinates);
        var map = _transform.GetMap(args.Coordinates);
        if (map == null || !TryComp<MapGridComponent>(grid, out var gridComp))
            return;

        var tile = _map.LocalToTile(grid.Value, gridComp, args.Coordinates);

        var mixtures = new ValueList<GasMixture>();
        if (_atmosphere.GetTileMixture(grid.Value, map.Value, tile, excite: true) is { } localMixture)
            mixtures.Add(localMixture);

        if (_atmosphere.GetAdjacentTileMixtures(grid.Value, tile, excite: true) is var adjacentTileMixtures)
        {
            while (adjacentTileMixtures.MoveNext(out var adjacentMixture))
            {
                mixtures.Add(adjacentMixture);
            }
        }

        foreach (var (gas, moles) in ent.Comp.Gases)
        {
            var molesPerMixture = moles / mixtures.Count;

            foreach (var mixture in mixtures)
            {
                mixture.AdjustMoles(gas, molesPerMixture);
            }
        }
    }
}
