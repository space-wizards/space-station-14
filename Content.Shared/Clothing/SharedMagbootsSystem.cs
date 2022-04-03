using Content.Shared.Actions;
using Content.Shared.Slippery;
using Content.Shared.Toggleable;
using Content.Shared.Verbs;

namespace Content.Shared.Clothing;

public abstract class SharedMagbootsSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SharedMagbootsComponent, GetVerbsEvent<ActivationVerb>>(AddToggleVerb);
        SubscribeLocalEvent<SharedMagbootsComponent, SlipAttemptEvent>(OnSlipAttempt);
        SubscribeLocalEvent<SharedMagbootsComponent, GetActionsEvent>(OnGetActions);
        SubscribeLocalEvent<SharedMagbootsComponent, ToggleActionEvent>(OnToggleAction);
    }

    private void AddToggleVerb(EntityUid uid, SharedMagbootsComponent component, GetVerbsEvent<ActivationVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract)
            return;

        ActivationVerb verb = new();
        verb.Text = Loc.GetString("toggle-magboots-verb-get-data-text");
        verb.Act = () => component.On = !component.On;
        // TODO VERB ICON add toggle icon? maybe a computer on/off symbol?
        args.Verbs.Add(verb);
    }

    private void OnSlipAttempt(EntityUid uid, SharedMagbootsComponent component, SlipAttemptEvent args)
    {
        if (component.On)
            args.Cancel();
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
