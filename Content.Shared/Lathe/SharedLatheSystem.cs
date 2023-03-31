using Content.Shared.Materials;
using Content.Shared.Research.Prototypes;
using JetBrains.Annotations;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using System.Net.Mail;

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

        SubscribeLocalEvent<LatheComponent, ComponentGetState>(OnGetState);
        SubscribeLocalEvent<LatheComponent, ComponentHandleState>(OnHandleState);
    }

    private void OnGetState(EntityUid uid, LatheComponent component, ref ComponentGetState args)
    {
        args.State = new LatheComponentState(component.MaterialUseMultiplier);
    }

    private void OnHandleState(EntityUid uid, LatheComponent component, ref ComponentHandleState args)
    {
        if (args.Current is not LatheComponentState state)
            return;
        component.MaterialUseMultiplier = state.MaterialUseMultiplier;
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

    public static int AdjustMaterial(int original, bool reduce, float multiplier)
        => reduce ? (int) MathF.Ceiling(original * multiplier) : original;

    protected abstract bool HasRecipe(EntityUid uid, LatheRecipePrototype recipe, LatheComponent component);
}

[Serializable, NetSerializable]
public sealed class LatheComponentState : ComponentState
{
    public float MaterialUseMultiplier;

    public LatheComponentState(float materialUseMultiplier)
    {
        MaterialUseMultiplier = materialUseMultiplier;
    }
}
