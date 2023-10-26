using Content.Shared.Emag.Systems;
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

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EmagLatheRecipesComponent, GotEmaggedEvent>(OnEmagged);
    }

    [PublicAPI]
    public bool CanProduce(EntityUid uid, string recipe, int amount = 1, LatheComponent? component = null)
    {
        return _proto.TryIndex<LatheRecipePrototype>(recipe, out var proto) && CanProduce(uid, proto, amount, component);
    }

    public bool CanProduce(EntityUid uid, LatheRecipePrototype recipe, int amount = 1, LatheComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return false;
        if (!HasRecipe(uid, recipe, component))
            return false;

        foreach (var (material, needed) in recipe.RequiredMaterials)
        {
            var adjustedAmount = AdjustMaterial(needed, recipe.ApplyMaterialDiscount, component.MaterialUseMultiplier);

            if (_materialStorage.GetMaterialAmount(component.Owner, material) < adjustedAmount * amount)
                return false;
        }
        return true;
    }

    private void OnEmagged(EntityUid uid, EmagLatheRecipesComponent component, ref GotEmaggedEvent args)
    {
        args.Handled = true;
    }

    public static int AdjustMaterial(int original, bool reduce, float multiplier)
        => reduce ? (int) MathF.Ceiling(original * multiplier) : original;

    protected abstract bool HasRecipe(EntityUid uid, LatheRecipePrototype recipe, LatheComponent component);
}
