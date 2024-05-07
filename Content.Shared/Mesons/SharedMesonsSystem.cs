using Content.Shared.Actions;
using Content.Shared.Clothing;
using Content.Shared.Toggleable;

namespace Content.Shared.Mesons;

public abstract class SharedMesonsSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;
    [Dependency] private readonly ActionContainerSystem _actionContainer = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MesonsComponent, ClothingGotEquippedEvent>(OnEquip);
        SubscribeLocalEvent<MesonsComponent, ClothingGotUnequippedEvent>(OnUnequip);
        SubscribeLocalEvent<MesonsComponent, ToggleActionEvent>(Toggle);
        SubscribeLocalEvent<MesonsComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(Entity<MesonsComponent> uid, ref MapInitEvent args)
    {
        _actionContainer.EnsureAction(uid, ref uid.Comp.ActionEntity, uid.Comp.Action);
    }

    private void OnEquip(Entity<MesonsComponent> uid, ref ClothingGotEquippedEvent args)
    {
        _actionsSystem.AddAction(args.Wearer, ref uid.Comp.ActionEntity, uid.Comp.Action);
    }

    private void OnUnequip(Entity<MesonsComponent> uid, ref ClothingGotUnequippedEvent args)
    {

    }

    private void Toggle(Entity<MesonsComponent> uid, ref ToggleActionEvent args)
    {
        if (uid.Comp.Enabled)
            Disable(uid);
        else
            Enable(uid);
    }

    public void Disable(Entity<MesonsComponent> uid)
    {
        uid.Comp.Enabled = false;
        _appearance.SetData(uid, ToggleVisuals.Layer, false);
    }

    public void Enable(Entity<MesonsComponent> uid)
    {
        uid.Comp.Enabled = true;
        _appearance.SetData(uid, ToggleVisuals.Layer, true);
    }
}
