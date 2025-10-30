using Content.Shared.IdentityManagement;
using Content.Shared.Popups;
using Content.Shared.Trigger.Components.Effects;

namespace Content.Shared.Trigger.Systems;

/// <summary>
/// This handles <see cref="PopupOnTriggerComponent"/>
/// </summary>
public sealed class PopupOnTriggerSystem : XOnTriggerSystem<PopupOnTriggerComponent>
{
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    protected override void OnTrigger(Entity<PopupOnTriggerComponent> ent, EntityUid target, ref TriggerEvent args)
    {
        if(args.User is null)
            return;

        EntityUid user = Identity.Entity(args.User.Value, EntityManager);

        // Popups only play for one entity
        if (ent.Comp.Quiet)
        {
            if (ent.Comp.Predicted)
            {
                _popup.PopupClient(Loc.GetString(ent.Comp.Text,("entity",target),("user",user)),
                    target,
                    ent.Comp.UserIsRecipient ? args.User : ent.Owner,
                    ent.Comp.PopupType);
            }

            else
            {
                _popup.PopupEntity(Loc.GetString(ent.Comp.OtherText ?? ent.Comp.Text,("entity",target),("user",user)),
                    target,
                    args.User.Value,
                    ent.Comp.PopupType);
            }

            return;
        }

        // Popups play for all entities
        if (ent.Comp.Predicted)
        {
            _popup.PopupPredicted(Loc.GetString(ent.Comp.Text,("entity",target),("user",user)),
                Loc.GetString(ent.Comp.OtherText ?? ent.Comp.Text,("entity",target),("user",user)),
                target,
                ent.Comp.UserIsRecipient ? args.User : ent.Owner,
                ent.Comp.PopupType);
        }

        else
        {
            _popup.PopupEntity(Loc.GetString(ent.Comp.OtherText ?? ent.Comp.Text,("entity",target),("user",user)),
                target,
                ent.Comp.PopupType);
        }
    }
}
