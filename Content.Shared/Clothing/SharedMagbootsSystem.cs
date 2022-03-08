using Content.Shared.Actions;
using Content.Shared.Toggleable;

namespace Content.Shared.Clothing;

public abstract class SharedMagbootsSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SharedMagbootsComponent, GetActionsEvent>(OnGetActions);
        SubscribeLocalEvent<SharedMagbootsComponent, ToggleActionEvent>(OnToggleAction);
    }

    private void OnGetActions(EntityUid uid, SharedMagbootsComponent component, GetActionsEvent args)
    {
        args.Actions.Add(component.ToggleAction);
    }

    private void OnToggleAction(EntityUid uid, SharedMagbootsComponent component, ToggleActionEvent args)
    {
        if (args.Handled)
            return;

        component.On = !component.On;

        args.Handled = true;
    }
}
