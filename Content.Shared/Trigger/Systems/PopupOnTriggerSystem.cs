using Content.Shared.Popups;
using Content.Shared.Trigger.Components.Effects;

namespace Content.Shared.Trigger.Systems;

/// <summary>
/// This handles...
/// </summary>
public sealed class PopupOnTriggerSystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PopupOnTriggerComponent, TriggerEvent>(OnTrigger);
    }

    private void OnTrigger(Entity<PopupOnTriggerComponent> ent, ref TriggerEvent args)
    {
        if (args.Key != null && !ent.Comp.KeysIn.Contains(args.Key))
            return;

        var target = ent.Comp.TargetUser ? args.User : ent.Owner;

        if (target == null)
            return;

        if (ent.Comp.Quiet)
        {
            _popup.PopupClient(Loc.GetString(ent.Comp.Text),
                                target.Value,
                                ent.Comp.UserIsRecipient ? args.User : ent.Owner,
                                ent.Comp.PopupType);
            return;
        }

        if (ent.Comp.Predicted)
            _popup.PopupPredicted(Loc.GetString(ent.Comp.Text),
                                Loc.GetString(ent.Comp.OtherText ?? ent.Comp.Text),
                                target.Value,
                                ent.Comp.UserIsRecipient ? args.User : ent.Owner,
                                ent.Comp.PopupType);

        else
            _popup.PopupEntity(Loc.GetString(ent.Comp.OtherText ?? ent.Comp.Text),
                                target.Value,
                                ent.Comp.PopupType);
    }
}
