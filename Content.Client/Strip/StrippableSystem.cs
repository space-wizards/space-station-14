using Content.Client.Inventory;
using Content.Shared.Cuffs.Components;
using Content.Shared.Hands;
using Content.Shared.Inventory.Events;
using Robust.Client.GameObjects;

namespace Content.Client.Strip;

/// <summary>
///     This is the client-side stripping system, which just triggers UI updates on events. 
/// </summary>
public sealed class StrippableSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StrippableComponent, CuffedStateChangeEvent>(OnCuffStateChange);
        SubscribeLocalEvent<StrippableComponent, DidEquipEvent>((e, _, _) => UpdateUi(e));
        SubscribeLocalEvent<StrippableComponent, DidUnequipEvent>((e, _, _) => UpdateUi(e));
        SubscribeLocalEvent<StrippableComponent, DidEquipHandEvent>((e, _, _) => UpdateUi(e));
        SubscribeLocalEvent<StrippableComponent, DidUnequipHandEvent>((e, _, _) => UpdateUi(e));
    }

    private void OnCuffStateChange(EntityUid uid, StrippableComponent component, ref CuffedStateChangeEvent args)
    {
        UpdateUi(uid);
    }

    public void UpdateUi(EntityUid uid)
    {
        if (!TryComp(uid, out ClientUserInterfaceComponent? uiComp))
            return;

        foreach (var ui in uiComp.Interfaces)
        {
            if (ui is StrippableBoundUserInterface stripUi)
                stripUi.UpdateMenu();
        }
    }
}
