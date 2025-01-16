using Content.Server.Administration;
using Content.Server.Administration.Logs;
using Content.Shared._CD.Engraving;
using Content.Shared.Database;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Robust.Shared.Player;

namespace Content.Server._CD.Engraving;

public sealed class EngraveableSystem : SharedEngraveableSystem
{
    [Dependency] private readonly IAdminLogManager _adminLogger = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly QuickDialogSystem _dialog = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EngraveableComponent, GetVerbsEvent<ActivationVerb>>(AddEngraveVerb);
    }

    private void AddEngraveVerb(Entity<EngraveableComponent> ent, ref GetVerbsEvent<ActivationVerb> args)
    {
        // First check if it's already been engraved. If it has, don't let them do it again.
        if (ent.Comp.EngravedMessage != string.Empty)
            return;

        // We need an actor to give the verb.
        if (!TryComp<ActorComponent>(args.User, out var actor))
            return;

        // Make sure ghosts can't engrave stuff.
        if (!args.CanInteract)
            return;

        var engraveVerb = new ActivationVerb
        {
            Text = Loc.GetString("engraving-verb-engrave"),
            Act = () =>
            {
                _dialog.OpenDialog(actor.PlayerSession,
                    Loc.GetString("engraving-verb-engrave"),
                    Loc.GetString("engraving-popup-ui-message"),
                    (string message) =>
                    {
                        // If either the actor or comp have magically vanished
                        if (actor.PlayerSession.AttachedEntity == null || !HasComp<EngraveableComponent>(ent))
                            return;

                        ent.Comp.EngravedMessage = message;
                        Dirty(ent);
                        _popup.PopupEntity(Loc.GetString(ent.Comp.EngraveSuccessMessage),
                            actor.PlayerSession.AttachedEntity.Value,
                            actor.PlayerSession,
                            PopupType.Medium);
                        _adminLogger.Add(LogType.Action,
                            LogImpact.Low,
                            $"{ToPrettyString(actor.PlayerSession.AttachedEntity):player} engraved an item with message: {message}");
                    });
            },
            Impact = LogImpact.Low,
        };
        engraveVerb.Impact = LogImpact.Low;
        args.Verbs.Add(engraveVerb);
    }
}
