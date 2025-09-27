using Content.Shared.DoAfter;
using Content.Shared.Verbs;
using Content.Shared.Disarmable; //берём ивент отсюда
using Content.Server.Chat.Managers;
using Content.Server.Chat.Systems;
using Content.Server.GameTicking;

namespace Content.Server.Disarmable;

public sealed class DisarmableSystem : EntitySystem
{
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly IEntityManager _entMan = default!;
    [Dependency] private readonly IChatManager _chat = default!;
    [Dependency] private readonly ChatSystem _chatSystem = default!;
    [Dependency] private readonly GameTicker _gameTicker = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<DisarmableComponent, GetVerbsEvent<Verb>>(OnGetVerbs);
        SubscribeLocalEvent<DisarmableComponent, DisarmDoAfterEvent>(OnDoAfter);
    }

    private void OnGetVerbs(EntityUid uid, DisarmableComponent comp, GetVerbsEvent<Verb> args)
    {
        if (!args.CanInteract || !args.CanAccess)
            return;

        var verb = new Verb
        {
            Text = "Обезвредить",
            Act = () =>
            {
                var ev = new DisarmDoAfterEvent();
                var doAfterArgs = new DoAfterArgs(EntityManager, args.User, TimeSpan.FromSeconds(comp.DisarmTime), ev, uid, target: uid)
                {
                    BreakOnDamage = true,
                    BreakOnMove = true,
                    NeedHand = true,
                };

                _doAfter.TryStartDoAfter(doAfterArgs);
            },
            Priority = 2
        };

        args.Verbs.Add(verb);
    }

    private void OnDoAfter(EntityUid uid, DisarmableComponent comp, DisarmDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled)
            return;

        var xform = Transform(uid);
        _entMan.SpawnEntity(comp.ResultPrototype, xform.Coordinates);
        _entMan.DeleteEntity(uid);

        // Глобальное оповещение от Центрального Командования
        _chatSystem.DispatchGlobalAnnouncement("Бомба обезврежена.", sender: "Центральное командование");

        // Переход раунда в PostRound после обезвреживания
        _gameTicker.EndRound("Бой фракций завершен. Победа за Контр-Террористами.");

        args.Handled = true;
    }
}
