using Content.Shared.Materials;
using Content.Shared.Maps;

namespace Content.Server.Materials;

/// <inheritdoc/>
public sealed class TileReclaimerSystem : SharedTileReclaimerSystem
{
    [Dependency] private readonly MaterialStorageSystem _materialStorage = default!;

    protected override void SpawnMaterialsFromComposition(EntityUid reclaimer,
        ContentTileDefinition tileDefinition,
        float efficiency,
        MaterialStorageComponent? storage = null,
        TransformComponent? xform = null)
    {
        if (!Resolve(reclaimer, ref storage, ref xform, false))
            return;

        foreach (var (material, amount) in tileDefinition.MaterialComposition)
        {
            var outputAmount = (int) (amount * efficiency);
            _materialStorage.TryChangeMaterialAmount(reclaimer, material, outputAmount, storage);
        }

        _materialStorage.SpawnMaterialFromStorage((reclaimer, storage), xform.Coordinates, true);
    }
}
