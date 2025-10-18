using Content.Shared.Changeling.Systems;
using Content.Shared.Clothing;
using Content.Shared.Clothing.ActionEvent;
using Content.Shared.Clothing.Components;
using Content.Shared.Clothing.EntitySystems;
using Content.Shared.Emag.Components;
using Content.Shared.Labels;
using Robust.Client.GameObjects;
using System;

namespace Content.Client.Clothing.Systems;
public sealed class HailerSystem : SharedHailerSystem
{

    [Dependency] private readonly UserInterfaceSystem _ui = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HailerComponent, SecHailerActionEvent>(OnHailAction);
        SubscribeLocalEvent<HailerComponent, ItemMaskToggledEvent>(OnMaskToggle);
    }

    private void OnHailAction(Entity<HailerComponent> ent, ref SecHailerActionEvent ev)
    {
        if (ev.Handled)
            return;

        if (_ui.TryGetOpenUi(ent.Owner, HailerUiKey.Key, out var bui))
        {
            bui.Update();
        }
        else if (TryComp<MaskComponent>(ent, out var mask))
        {
            if (!mask.IsToggled && !ent.Comp.AreWiresCut)
            {
                _ui.TryOpenUi(ent.Owner, HailerUiKey.Key, ev.Performer, predicted: true);
            }
        }
    }

    private void OnMaskToggle(Entity<HailerComponent> ent, ref ItemMaskToggledEvent args)
    {
        if (TryComp<MaskComponent>(ent, out var mask) && mask.IsToggled)
            _ui.CloseUi(ent.Owner, HailerUiKey.Key);
    }
}
