using Content.Shared.Clothing;
using Content.Shared.Clothing.ActionEvent;
using Content.Shared.Clothing.Components;
using Content.Shared.Clothing.EntitySystems;
using Robust.Client.GameObjects;

namespace Content.Client.Clothing.Systems;
public sealed class HailerSystem : SharedHailerSystem
{

    [Dependency] private readonly UserInterfaceSystem _ui = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HailerComponent, HailerActionEvent>(OnHailAction);
        SubscribeLocalEvent<HailerComponent, ItemMaskToggledEvent>(OnMaskToggle);
    }

    private void OnHailAction(Entity<HailerComponent> ent, ref HailerActionEvent ev)
    {
        if (ev.Handled)
            return;

        if (TryComp<MaskComponent>(ent, out var mask))
        {
            if (!mask.IsToggled && !ent.Comp.AreWiresCut)
            {
                //Otherwise, open it
                ev.Handled = true;
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
