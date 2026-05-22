using Content.Shared.Materials;
using Content.Shared.Maps;

namespace Content.Server.Materials;

/// <inheritdoc/>
public sealed partial class TileReclaimerSystem : SharedTileReclaimerSystem
{
    [Dependency] private MaterialStorageSystem _materialStorage = default!;

    protected override void SpawnMaterialsFromComposition(Entity<MaterialStorageComponent?, TransformComponent?> ent,
        ContentTileDefinition tileDefinition,
        float efficiency)
    {
        if (!Resolve(ent, ref ent.Comp1, ref ent.Comp2))
            return;

        foreach (var (material, amount) in tileDefinition.MaterialComposition)
        {
            var outputAmount = (int) (amount * efficiency);
            _materialStorage.TryChangeMaterialAmount(ent, material, outputAmount, ent.Comp1);
        }

        _materialStorage.EjectAllMaterial(ent, ent.Comp2.Coordinates, ent.Comp1, true);
    }
}
