using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using Content.Shared.Administration;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Robust.Server.Player;
using Robust.Shared.Enums;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Serialization.TypeSerializers.Interfaces;

namespace Content.Server.Administration;

/// <summary>
/// This handles the server portion of quick dialogs, including opening them.
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

    public override void Shutdown()
    {
        base.Shutdown();
        _playerManager.PlayerStatusChanged -= PlayerManagerOnPlayerStatusChanged;
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
            session
        );

        _openDialogs.Add(did, (okAction, cancelAction));
        if (!_openDialogsByUser.ContainsKey(session.UserId))
            _openDialogsByUser.Add(session.UserId, new List<int>());

        _openDialogsByUser[session.UserId].Add(did);
    }

    private bool TryParseQuickDialog<T>(QuickDialogEntryType entryType, string input, [NotNullWhen(true)] out T? output)
    {
        switch (entryType)
        {
            case QuickDialogEntryType.Integer:
            {
                var result = int.TryParse(input, out var val);
                output = (T?) (object?) val;
                return result;
            }
            case QuickDialogEntryType.Float:
            {
                var result = float.TryParse(input, out var val);
                output = (T?) (object?) val;
                return result;
            }
            case QuickDialogEntryType.ShortText:
            {
                if (input.Length > 100)
                {
                    output = default;
                    return false;
                }

                output = (T?) (object?) input;
                return output is not null;
            }
            case QuickDialogEntryType.LongText:
            {
                if (input.Length > 2000)
                {
                    output = default;
                    return false;
                }

                //It's verrrry likely that this will be longstring
                var longString = (LongString) input;

                output = (T?) (object?) longString;
                return output is not null;
            }
            default:
                throw new ArgumentOutOfRangeException(nameof(entryType), entryType, null);
        }
    }

    private QuickDialogEntryType TypeToEntryType(Type T)
    {
        if (T == typeof(int) || T == typeof(uint) || T == typeof(long) || T == typeof(ulong))
            return QuickDialogEntryType.Integer;

        if (T == typeof(float) || T == typeof(double))
            return QuickDialogEntryType.Float;

        if (T == typeof(string)) // People are more likely to notice the input box is too short than they are to notice it's too long.
            return QuickDialogEntryType.ShortText;

        if (T == typeof(LongString))
            return QuickDialogEntryType.LongText;

        throw new ArgumentException($"Tried to open a dialog with unsupported type {T}.");
    }
}

/// <summary>
/// A type used with quick dialogs to indicate you want a large entry window for text and not a short one.
/// </summary>
/// <param name="String">The string retrieved.</param>
public record struct LongString(string String)
{
    public static implicit operator string(LongString longString)
    {
        return longString.String;
    }
    public static explicit operator LongString(string s)
    {
        return new(s);
    }
}
