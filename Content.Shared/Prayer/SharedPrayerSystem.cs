using Content.Shared.Administration;
using Content.Shared.Database;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Content.Shared.Bible.Components;
using Content.Shared.Chat;
using Robust.Shared.Player;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;

namespace Content.Shared.Prayer;

/// <summary>
/// Used to predict Pray verb
/// </summary>
public abstract class SharedPrayerSystem : EntitySystem
{
    [Dependency] private readonly ISharedChatManager _chatManager = default!;
    [Dependency] private readonly SharedQuickDialogSystem _quickDialog = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PrayableComponent, GetVerbsEvent<ActivationVerb>>(AddPrayVerb);
    }

    private void AddPrayVerb(EntityUid uid, PrayableComponent comp, GetVerbsEvent<ActivationVerb> args)
    {
        if (!_gameTiming.IsFirstTimePredicted)
            return;

        // if it doesn't have an actor and we can't reach it then don't add the verb
        if (!TryComp(args.User, out ActorComponent? actor))
            return;

        // this is to prevent ghosts from using it
        if (!args.CanInteract)
            return;

        var prayerVerb = new ActivationVerb
        {
            Text = Loc.GetString(comp.Verb),
            Icon = comp.VerbImage,
            Act = () =>
            {
                if (comp.BibleUserOnly && !TryComp<BibleUserComponent>(args.User, out var bibleUser))
                {
                    _popupSystem.PopupEntity(Loc.GetString("prayer-popup-notify-pray-locked"), uid, actor.PlayerSession, PopupType.Large);
                    return;
                }

                _quickDialog.OpenDialog(actor.PlayerSession, Loc.GetString(comp.Verb), Loc.GetString("prayer-popup-notify-pray-ui-message"), (string message) =>
                {
                    // Make sure the player's entity and the Prayable entity+component still exist
                    if (actor?.PlayerSession != null && TryComp<PrayableComponent>(uid, out var prayable))
                        RaiseNetworkEvent(new PrayEvent(prayable.NotificationPrefix, message));
                });
            },
            Impact = LogImpact.Low,

        };
        prayerVerb.Impact = LogImpact.Low;
        args.Verbs.Add(prayerVerb);
    }
}

/// <summary>
/// Event containing the message of prayer, that is to be sent to the admins
/// </summary>
[Serializable, NetSerializable]
public sealed class PrayEvent(LocId Prefix, string Message) : EntityEventArgs
{
    public string Message = Message;
    public LocId Prefix = Prefix;
};
