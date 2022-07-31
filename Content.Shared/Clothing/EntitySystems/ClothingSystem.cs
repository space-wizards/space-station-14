using Content.Shared.Clothing.Components;
using Content.Shared.Inventory;
using Content.Shared.Item;
using Robust.Shared.GameStates;

namespace Content.Shared.Clothing.EntitySystems;

public sealed class ClothingSystem : EntitySystem
{
    [Dependency] private readonly SharedItemSystem _itemSys = default!;

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
            SetEquippedPrefix(uid, state.EquippedPrefix, component);
    }

    #region Public API

    public void SetEquippedPrefix(EntityUid uid, string? prefix, SharedClothingComponent? clothing = null)
    {
        if (!Resolve(uid, ref clothing, false))
            return;

        if (clothing.EquippedPrefix == prefix)
            return;

        clothing.EquippedPrefix = prefix;
        _itemSys.VisualsChanged(uid);
        Dirty(clothing);
    }

    public void SetSlots(EntityUid uid, SlotFlags slots, SharedClothingComponent? clothing = null)
    {
        if (!Resolve(uid, ref clothing))
            return;

        clothing.Slots = slots;
        Dirty(clothing);
    }

    #endregion
}
