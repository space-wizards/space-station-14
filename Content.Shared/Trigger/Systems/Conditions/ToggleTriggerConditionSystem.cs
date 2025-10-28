using Content.Shared.Popups;
using Content.Shared.Trigger.Components.Conditions;
using Content.Shared.Verbs;

namespace Content.Shared.Trigger.Systems.Conditions;

public sealed class ToggleTriggerConditionSystem : TriggerConditionSystem<ToggleTriggerConditionComponent>
{
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ToggleTriggerConditionComponent, GetVerbsEvent<AlternativeVerb>>(OnToggleGetAltVerbs);
    }

    protected override void CheckCondition(Entity<ToggleTriggerConditionComponent> ent, ref AttemptTriggerEvent args)
    {
        ModifyEvent(ent, !ent.Comp.Enabled, ref args);
    }

    private void OnToggleGetAltVerbs(Entity<ToggleTriggerConditionComponent> ent,
        ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanInteract || !args.CanAccess || args.Hands == null)
            return;

        var user = args.User;

        args.Verbs.Add(new AlternativeVerb()
        {
            Text = Loc.GetString(ent.Comp.ToggleVerb),
            Act = () => Toggle(ent, user)
        });
    }

    private void Toggle(Entity<ToggleTriggerConditionComponent> ent, EntityUid user)
    {
        var msg = ent.Comp.Enabled ? ent.Comp.ToggleOff : ent.Comp.ToggleOn;
        _popup.PopupPredicted(Loc.GetString(msg), ent.Owner, user);
        ent.Comp.Enabled = !ent.Comp.Enabled;
        Dirty(ent);
    }
}
