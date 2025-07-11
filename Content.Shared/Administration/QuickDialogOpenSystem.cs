using Robust.Shared.Player;
using System.Diagnostics.CodeAnalysis;
using Robust.Shared.Network;
using Robust.Shared.Timing;

namespace Content.Shared.Administration;

/// <summary>
/// This handles the client portion of quick dialogs.
/// </summary>
public abstract partial class SharedQuickDialogSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _gameTiming = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeAllEvent<QuickDialogResponseEvent>(Handler);
    }

    protected abstract int GetDialogId(NetUserId userId, bool predicted);

    protected readonly Dictionary<int, Dialog> _openDialogs = new();
    protected readonly Dictionary<(NetUserId, int), int> _mappingClientToLocal = new();

    private void OpenDialogInternal(ICommonSession session, string title, List<QuickDialogEntry> entries, QuickDialogButtonFlag buttons, Action<QuickDialogResponseEvent> okAction, Action cancelAction, bool predicted)
    {
        var did = GetDialogId(session.UserId, predicted);
        if (predicted)
            RaiseLocalEvent(
                new QuickDialogOpenEvent(
                    title,
                    entries,
                    did,
                    buttons,
                    true)
            );
        else
            RaiseNetworkEvent(
                new QuickDialogOpenEvent(
                    title,
                    entries,
                    did,
                    buttons),
                session
            );

        _openDialogs.Add(did, new Dialog(okAction, cancelAction));
    }

    private void Handler(QuickDialogResponseEvent msg, EntitySessionEventArgs args)
    {
        if (!_gameTiming.IsFirstTimePredicted)
            return;

        if (!_mappingClientToLocal.ContainsKey((args.SenderSession.UserId, msg.DialogId)))
        {
            args.SenderSession.Channel.Disconnect($"Replied with invalid quick dialog data with id {msg.DialogId} for {args.SenderSession.UserId}.");
            return;
        }

        var didLocal = _mappingClientToLocal[(args.SenderSession.UserId, msg.DialogId)];

        if (!_openDialogs.ContainsKey(didLocal))
        {
            args.SenderSession.Channel.Disconnect($"Replied with invalid quick dialog data with id {msg.DialogId}({didLocal}).");
            return;
        }

        switch (msg.ButtonPressed)
        {
            case QuickDialogButtonFlag.OkButton:
                _openDialogs[didLocal].okAction.Invoke(msg);
                break;
            case QuickDialogButtonFlag.CancelButton:
                _openDialogs[didLocal].cancelAction.Invoke();
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
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

public record struct Dialog(Action<QuickDialogResponseEvent> okAction, Action cancelAction);
