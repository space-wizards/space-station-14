using Content.Shared.Actions;
using Content.Shared.Slippery;
using Content.Shared.Toggleable;
using Content.Shared.Verbs;

namespace Content.Shared.Clothing;

public abstract class SharedMagbootsSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _sharedActions = default!;
    [Dependency] private readonly ClothingSpeedModifierSystem _clothingSpeedModifier = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MagbootsComponent, GetVerbsEvent<ActivationVerb>>(AddToggleVerb);
        SubscribeLocalEvent<MagbootsComponent, SlipAttemptEvent>(OnSlipAttempt);
        SubscribeLocalEvent<MagbootsComponent, GetItemActionsEvent>(OnGetActions);
    }

    protected void OnChanged(MagbootsComponent component)
    {
        _sharedActions.SetToggled(component.ToggleAction, component.On);
        _clothingSpeedModifier.SetClothingSpeedModifierEnabled(component.Owner, component.On);
    }

    private void AddToggleVerb(EntityUid uid, MagbootsComponent component, GetVerbsEvent<ActivationVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract)
            return;

        ActivationVerb verb = new();
        verb.Text = Loc.GetString("toggle-magboots-verb-get-data-text");
        verb.Act = () => component.On = !component.On;
        // TODO VERB ICON add toggle icon? maybe a computer on/off symbol?
        args.Verbs.Add(verb);
    }

    private void OnSlipAttempt(EntityUid uid, MagbootsComponent component, SlipAttemptEvent args)
    {
        if (component.On)
            args.Cancel();
    }

    private void OnGetActions(EntityUid uid, MagbootsComponent component, GetItemActionsEvent args)
    {
        args.Actions.Add(component.ToggleAction);
    }
}
