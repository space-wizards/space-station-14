using Content.Server.Administration;
using Content.Server.Chat.Systems;
using Content.Server.Popups;
using Content.Shared.Chat;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Robust.Server.Console;
using Robust.Shared.Player;
using Content.Shared.Speech.Muting;

namespace Content.Server.Mobs;

/// <summary>
///     Handles performing crit-specific actions.
/// </summary>
public sealed class CritMobActionsSystem : EntitySystem
{
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly DeathgaspSystem _deathgasp = default!;
    [Dependency] private readonly IServerConsoleHost _host = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly PopupSystem _popupSystem = default!;
    [Dependency] private readonly QuickDialogSystem _quickDialog = default!;

    private const int MaxLastWordsLength = 30;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MobStateActionsComponent, CritSuccumbEvent>(OnSuccumb);
        SubscribeLocalEvent<MobStateActionsComponent, CritFakeDeathEvent>(OnFakeDeath);
        SubscribeLocalEvent<MobStateActionsComponent, CritLastWordsEvent>(OnLastWords);
    }

    private void OnSuccumb(EntityUid uid, MobStateActionsComponent component, CritSuccumbEvent args)
    {
        if (!TryComp<ActorComponent>(uid, out var actor) || !_mobState.IsCritical(uid))
            return;

        _host.ExecuteCommand(actor.PlayerSession, "ghost");
        args.Handled = true;
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

    private void OnLastWords(EntityUid uid, MobStateActionsComponent component, CritLastWordsEvent args)
    {
        if (!TryComp<ActorComponent>(uid, out var actor))
            return;

        _quickDialog.OpenDialog(actor.PlayerSession, Loc.GetString("action-name-crit-last-words"), "",
            (string lastWords) =>
            {
                // if a person is gibbed/deleted, they can't say last words
                if (Deleted(uid))
                    return;

                // Intentionally does not check for muteness
                if (actor.PlayerSession.AttachedEntity != uid
                    || !_mobState.IsCritical(uid))
                    return;

                if (lastWords.Length > MaxLastWordsLength)
                {
                    lastWords = lastWords.Substring(0, MaxLastWordsLength);
                }
                lastWords += "...";

                _chat.TrySendInGameICMessage(uid, lastWords, InGameICChatType.Whisper, ChatTransmitRange.Normal, checkRadioPrefix: false, ignoreActionBlocker: true);
                _host.ExecuteCommand(actor.PlayerSession, "ghost");
            });

        args.Handled = true;
    }
}
