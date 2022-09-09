using Content.Shared.Materials;
using Content.Shared.Research.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Shared.Lathe;

/// <summary>
/// This handles...
/// </summary>
public abstract class SharedLatheSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly SharedMaterialStorageSystem _materialStorage = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {

    }

    public bool CanProduce(SharedLatheComponent component, string recipe, int amount = 1)
    {
        if (!_proto.TryIndex<LatheRecipePrototype>(recipe, out var proto))
            return false;
        return CanProduce(component, proto, amount);
    }

    public bool CanProduce(SharedLatheComponent component, LatheRecipePrototype recipe, int amount = 1)
    {
        var lathe = component.Owner;
        if (!TryComp<MaterialStorageComponent>(lathe, out var materialStorage))
            return false;

        //do check for having recipe here

        foreach (var (material, needed) in recipe.RequiredMaterials)
        {
            if (_materialStorage.GetMaterialAmount(materialStorage, material) < amount * needed)
                return false;
        }

        return true;
    }
}
