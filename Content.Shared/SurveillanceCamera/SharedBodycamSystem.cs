using Content.Shared.IdentityManagement;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Robust.Shared.Player;

namespace Content.Shared.SurveillanceCamera;

public class SharedBodycamSystem: EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<BodycamComponent, GetVerbsEvent<AlternativeVerb>>(OnGetVerbs);
    }

    private void OnGetVerbs(EntityUid uid, BodycamComponent comp, GetVerbsEvent<AlternativeVerb> args)
    {
        if (comp.State == BodycamState.Disabled)
        {
            args.Verbs.Add(new AlternativeVerb()
            {
                Act = () => SwitchOn(uid, comp, args.User),
                Text = Loc.GetString("bodycam-switch-on-verb"),
            });
        }
    }

    /// <summary>
    /// Called on a bodycam when someone alt-clicks it to turn it on
    /// </summary>
    protected virtual void SwitchOn(EntityUid uid, BodycamComponent comp, EntityUid user)
    {
        // todo: check if being worn

        comp.State = BodycamState.Active;

        // what the person who switches the body cam on sees (only send to user client)
        _popup.PopupClient(Loc.GetString("bodycam-switch-on-message-self"), user, user);
        // what everyone else sees (filter out the user from it)
        _popup.PopupEntity(Loc.GetString("bodycam-switch-on-message-other", ("user", Identity.Name(user, EntityManager))), user, Filter.Pvs(user, entityManager: EntityManager).RemoveWhere(e => e.AttachedEntity == user), true);
    }

    // todo: SwitchOff
    // todo: automatic SwitchOff when armour vest is taken off
}
