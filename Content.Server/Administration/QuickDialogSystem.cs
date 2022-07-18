using System.ComponentModel;
using Content.Shared.Administration;
using Robust.Server.Player;
using Robust.Shared.Enums;
using Robust.Shared.Network;
using Robust.Shared.Player;

namespace Content.Server.Administration;

/// <summary>
/// This handles...
/// </summary>
public sealed partial class QuickDialogSystem : EntitySystem
{
    [Dependency] private readonly IPlayerManager _playerManager = default!;

    /// <summary>
    /// Contains the success/cancel actions for a dialog.
    /// </summary>
    private readonly Dictionary<int, (Action<QuickDialogResponseEvent> okAction, Action cancelAction)> _openDialogs = new();
    private readonly Dictionary<NetUserId, List<int>> _openDialogsByUser = new();

    private int _nextDialogId = 1;

    /// <inheritdoc/>
    public override void Initialize()
    {
        _playerManager.PlayerStatusChanged += PlayerManagerOnPlayerStatusChanged;

        SubscribeNetworkEvent<QuickDialogResponseEvent>(Handler);
    }

    private void Handler(QuickDialogResponseEvent msg, EntitySessionEventArgs args)
    {
        if (!_openDialogs.ContainsKey(msg.DialogId) || !_openDialogsByUser[args.SenderSession.UserId].Contains(msg.DialogId))
        {
            args.SenderSession.ConnectedClient.Disconnect($"Replied with invalid quick dialog data with id {msg.DialogId}.");
            return;
        }

        switch (msg.ButtonPressed)
        {
            case QuickDialogButtonFlag.OkButton:
                _openDialogs[msg.DialogId].okAction.Invoke(msg);
                break;
            case QuickDialogButtonFlag.CancelButton:
                _openDialogs[msg.DialogId].cancelAction.Invoke();
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        _openDialogs.Remove(msg.DialogId);
        _openDialogsByUser[args.SenderSession.UserId].Remove(msg.DialogId);
    }

    private int GetDialogId()
    {
        return _nextDialogId++;
    }

    private void PlayerManagerOnPlayerStatusChanged(object? sender, SessionStatusEventArgs e)
    {
        if (e.NewStatus != SessionStatus.Disconnected && e.NewStatus != SessionStatus.Zombie)
            return;

        var user = e.Session.UserId;

        if (!_openDialogsByUser.ContainsKey(user))
            return;

        foreach (var dialogId in _openDialogsByUser[user])
        {
            _openDialogs[dialogId].cancelAction.Invoke();
            _openDialogs.Remove(dialogId);
        }

        _openDialogsByUser.Remove(user);
    }

    private void OpenDialogInternal(IPlayerSession session, string title, List<QuickDialogEntry> entries, QuickDialogButtonFlag buttons, Action<QuickDialogResponseEvent> okAction, Action cancelAction)
    {
        var did = GetDialogId();
        RaiseNetworkEvent(
            new QuickDialogOpenEvent(
                title,
                entries,
                did,
                buttons),
            Filter.SinglePlayer(session)
        );

        _openDialogs.Add(did, (okAction, cancelAction));
        if (!_openDialogsByUser.ContainsKey(session.UserId))
            _openDialogsByUser.Add(session.UserId, new List<int>());

        _openDialogsByUser[session.UserId].Add(did);
    }

    private T ParseQuickDialog<T>(QuickDialogEntryType entryType, string input)
    {
        return entryType switch
        {
            QuickDialogEntryType.ShortText when input.Length > 100 => throw new ArgumentException(
                "Got an unexpectedly large short-text entry."),
            QuickDialogEntryType.ShortText => (T)(object)input,

            QuickDialogEntryType.LongText when input.Length > 2000 => throw new ArgumentException(
                "Got an unexpectedly large long-text entry."),
            QuickDialogEntryType.LongText => (T)(object)new LongString(input),

            QuickDialogEntryType.Float => (T)(object) float.Parse(input), // Gross, but fine. This exceptioning out will boot the client.

            QuickDialogEntryType.Integer => (T)(object) int.Parse(input),

            _ => throw new NotSupportedException(),
        };
    }

    private QuickDialogEntryType TypeToEntryType(Type T)
    {
        if (T == typeof(int) || T == typeof(uint) || T == typeof(long) || T == typeof(ulong))
        {
            return QuickDialogEntryType.Integer;
        }
        else if (T == typeof(float) || T == typeof(double))
        {
            return QuickDialogEntryType.Float;
        }
        else if (T == typeof(string)) // People are more likely to notice the input box is too short than they are to notice it's too long.
        {
            return QuickDialogEntryType.ShortText;
        } else if (T == typeof(LongString))
        {
            return QuickDialogEntryType.LongText;
        }

        throw new ArgumentException($"Tried to open a dialog with unsupported type {T}.");
    }
}

/// <summary>
/// A type used with quick dialogs to indicate you want a large entry window for text and not a short one.
/// </summary>
/// <param name="String">The string retrieved.</param>
public record struct LongString(string String);
