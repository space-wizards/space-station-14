using Content.Shared.Clothing.Components;
using Robust.Shared.GameStates;

namespace Content.Shared.Clothing.EntitySystems;

public sealed class ClothingSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SharedClothingComponent, ComponentGetState>(OnGetState);
        SubscribeLocalEvent<SharedClothingComponent, ComponentHandleState>(OnHandleState);
    }

    private void OnGetState(EntityUid uid, SharedClothingComponent component, ref ComponentGetState args)
    {
        args.State = new ClothingComponentState(component.EquippedPrefix);
    }

    private void OnHandleState(EntityUid uid, SharedClothingComponent component, ref ComponentHandleState args)
    {
        if (args.Current is ClothingComponentState state)
            component.EquippedPrefix = state.EquippedPrefix;
    }

    #region Public API

    public void SetEquippedPrefix(EntityUid uid, string? prefix, SharedClothingComponent? clothing = null)
    {
        if (!Resolve(uid, ref clothing))
            return;

        clothing.EquippedPrefix = prefix;
        Dirty(clothing);
    }

    #endregion
}
