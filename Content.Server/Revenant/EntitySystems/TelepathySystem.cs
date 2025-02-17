using Content.Shared.Verbs;
using Robust.Shared.Player;
using Content.Server.Administration;
using Content.Server.Prayer;
using Content.Shared.Revenant.Components;
using Robust.Shared.Utility;
using Content.Server.Chat.Systems;

namespace Content.Server.Revenant;

public sealed partial class TelepathySystem : EntitySystem
{
    [Dependency] private readonly QuickDialogSystem _quickDialog = default!;
    [Dependency] private readonly PrayerSystem _prayerSystem = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<GetVerbsEvent<Verb>>(GetVerbs);
    }

    private void GetVerbs(GetVerbsEvent<Verb> ev)
    {
        AddRevenantVerbs(ev);
    }

    private void AddRevenantVerbs(GetVerbsEvent<Verb> args)
    {
        if (!EntityManager.TryGetComponent(args.User, out ActorComponent? actor) || args.User == args.Target)
            return;

        var player = actor.PlayerSession;

        if (HasComp<RevenantComponent>(args.User))
        {
            if (TryComp(args.Target, out ActorComponent? targetActor))
            {
                // Subtle Messages
                Verb telepathy = new();
                telepathy.Text = Loc.GetString("prayer-verbs-subtle-message-revenant");
                telepathy.Priority = -3;
                telepathy.DoContactInteraction = true;
                telepathy.Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/vv.svg.192dpi.png"));
                telepathy.Act = () =>
                {
                    _quickDialog.OpenDialog(player, "Пробраться в мысли", "Сообщение", (string message) =>
                    {
                        _prayerSystem.SendSubtleMessage(targetActor.PlayerSession, player, message, Loc.GetString("prayer-popup-subtle-revenant"));
                        var ev = new EntitySpokeToEntityEvent(args.Target, message);
                        RaiseLocalEvent(args.User, ev, true);
                    });
                };
                args.Verbs.Add(telepathy);
            }
        }
    }
}

