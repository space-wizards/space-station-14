using Robust.Shared.Player;
using System.Diagnostics.CodeAnalysis;
/*using Content.Shared.Administration;*/
/*using Robust.Server.Player;*/
/*using Robust.Shared.Enums;*/
using Robust.Shared.Network;
/*using Robust.Shared.Player;*/

namespace Content.Shared.Administration;

/// <summary>
/// This handles the client portion of quick dialogs.
/// </summary>
public abstract partial class SharedQuickDialogSystem : EntitySystem
{

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();
    }

    protected abstract int GetDialogId(NetUserId userId);

    protected virtual int OpenDialogInternal(ICommonSession session, string title, List<QuickDialogEntry> entries, QuickDialogButtonFlag buttons, Action<QuickDialogResponseEvent> okAction, Action cancelAction)
    {
        var did = GetDialogId(session.UserId);
        RaiseLocalEvent(
            new QuickDialogOpenEvent(
                title,
                entries,
                did,
                buttons)
        );

        return did;
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
