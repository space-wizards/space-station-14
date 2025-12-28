using Content.Server.Chat.Systems;
using Content.Server.Popups;
using Content.Shared.Chat;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Speech.Muting;
using Robust.Server.Console;
using Robust.Shared.Player;

namespace Content.Server.Mobs;

/// <summary>
///     Handles performing crit-specific actions.
/// </summary>
public sealed class CritMobActionsSystem : SharedCritMobActionsSystem
{
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly DeathgaspSystem _deathgasp = default!;
    [Dependency] private readonly IServerConsoleHost _host = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly PopupSystem _popupSystem = default!;

    private const int MaxLastWordsLength = 30;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MobStateActionsComponent, CritFakeDeathEvent>(OnFakeDeath);
        SubscribeLocalEvent<MobStateActionsComponent, CritSuccumbEvent>(OnSuccumb);
        SubscribeNetworkEvent<CritLastWordsSayEvent>(OnCritLastWordsSayEvent);
    }

    private void OnFakeDeath(EntityUid uid, MobStateActionsComponent component, CritFakeDeathEvent args)
    {
        if (!_mobState.IsCritical(uid))
            return;

        if (HasComp<MutedComponent>(uid))
        {
            _popupSystem.PopupEntity(Loc.GetString("fake-death-muted"), uid, uid);
            return;
        }

        args.Handled = _deathgasp.Deathgasp(uid);
    }

    private void OnSuccumb(EntityUid uid, MobStateActionsComponent component, CritSuccumbEvent args)
    {
        if (!TryComp<ActorComponent>(uid, out var actor) || !_mobState.IsCritical(uid))
            return;

        _host.ExecuteCommand(actor.PlayerSession, "ghost");
        args.Handled = true;
    }

    private void OnCritLastWordsSayEvent(CritLastWordsSayEvent msg, EntitySessionEventArgs args)
    {
        if (args.SenderSession.AttachedEntity == null)
            return;

        _chat.TrySendInGameICMessage(args.SenderSession.AttachedEntity.Value, msg.Message, InGameICChatType.Whisper, ChatTransmitRange.Normal, checkRadioPrefix: false, ignoreActionBlocker: true);
        _host.ExecuteCommand(args.SenderSession, "ghost");
    }
}
