using Content.Shared.Materials;
using Content.Shared.Research.Prototypes;
using JetBrains.Annotations;
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

    [PublicAPI]
    public bool CanProduce(LatheComponent component, string recipe, int amount = 1)
    {
        return _proto.TryIndex<LatheRecipePrototype>(recipe, out var proto) && CanProduce(component, proto, amount);
    }

    public bool CanProduce(LatheComponent component, LatheRecipePrototype recipe, int amount = 1)
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
