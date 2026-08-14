using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using Content.Shared.QuickDialog.Events;
using JetBrains.Annotations;
using Robust.Shared.Enums;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Serialization;

namespace Content.Shared.QuickDialog;

/// <summary>
///
/// </summary>
public abstract partial class QuickDialogSystem : EntitySystem
{
    [Dependency] private ISharedPlayerManager _playerManager = default!;

    /// <summary>
    ///
    /// </summary>
    private readonly Dictionary<NetUserId, Dictionary<string, (IQuickDialogEntry[] Entries, Action<object[]> OkAction, Action? CancelAction)>> _openDialogsPerUser = [];

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        _playerManager.PlayerStatusChanged += PlayerManagerOnPlayerStatusChanged;
    }

    /// <inheritdoc/>
    public override void Shutdown()
    {
        base.Shutdown();

        _playerManager.PlayerStatusChanged -= PlayerManagerOnPlayerStatusChanged;
    }

    private void PlayerManagerOnPlayerStatusChanged(object? sender, SessionStatusEventArgs e)
    {
        if (e.NewStatus != SessionStatus.Disconnected && e.NewStatus != SessionStatus.Zombie)
            return;

        foreach (var (_, actions) in _openDialogsPerUser[e.Session.UserId])
        {
            actions.CancelAction?.Invoke();
        }

        _openDialogsPerUser.Remove(e.Session.UserId);
    }

    [SubscribeNetworkEvent]
    private void Handler(QuickDialogResponseEvent msg, EntitySessionEventArgs args)
    {
        if (!_openDialogsPerUser.TryGetValue(args.SenderSession.UserId, out var dialogs) || !dialogs.TryGetValue(msg.DialogId, out var data))
        {
            args.SenderSession.Channel.Disconnect($"Replied with invalid quick dialog data with id {msg.DialogId}.");
            return;
        }

        if (msg.Responses == null || msg.Responses.Length < data.Entries.Length)
        {
            data.CancelAction?.Invoke();
            return;
        }

        var responses = new object[data.Entries.Length];
        for (var i = 0; i < data.Entries.Length; i++)
        {
            var entry = data.Entries[i];
            if (!TryParse(entry, msg.Responses[i], out var value))
            {
                data.CancelAction?.Invoke();
                return;
            }

            responses[i] = value;
        }

        switch (msg.ButtonPressed)
        {
            case QuickDialogButtonFlags.OkButton:
                data.OkAction.Invoke(responses);
                break;
            case QuickDialogButtonFlags.CancelButton:
                data.CancelAction?.Invoke();
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(msg), nameof(msg.ButtonPressed) + ": Invalid button flag.");
        }

        dialogs.Remove(msg.DialogId);
    }

    /// <summary>
    ///
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="entry"></param>
    /// <param name="value"></param>
    /// <param name="output"></param>
    /// <returns></returns>
    private static bool TryParseNumber<T>(IQuickDialogEntry entry, string value, [NotNullWhen(true)] out object? output) where T : INumber<T>
    {
        output = null;

        if (entry is not IQuickDialogEntry<T> typedEntry)
            return false;

        if (!T.TryParse(value, null, out var result))
            return false;

        if (result < typedEntry.Min)
            return false;

        if (result > typedEntry.Max)
            return false;

        output = result;
        return true;
    }

    /// <summary>
    ///
    /// </summary>
    /// <param name="entry"></param>
    /// <param name="value"></param>
    /// <param name="output"></param>
    /// <returns></returns>
    private static bool TryParseString(IQuickDialogEntry entry, string value, [NotNullWhen(true)] out object? output)
    {
        output = null;

        if (entry is not QuickDialogEntryString typedEntry)
            return false;

        if (value.Length < typedEntry.Min)
            return false;

        if (value.Length > typedEntry.Max)
            return false;

        output = value;
        return true;
    }

    /// <summary>
    ///
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="entry"></param>
    /// <returns></returns>
    private static (object min, object max) GetMinMax<T>(IQuickDialogEntry entry) where T : INumber<T>
    {
        if (entry is not IQuickDialogEntry<T> typedEntry)
            return (0, 0);

        return (typedEntry.Min, typedEntry.Max);
    }

    /// <summary>
    ///
    /// </summary>
    /// <param name="entry"></param>
    /// <param name="value"></param>
    /// <param name="output"></param>
    /// <returns></returns>
    [PublicAPI]
    public static bool TryParse(IQuickDialogEntry entry, string value, [NotNullWhen(true)] out object? output)
    {
        output = null;

        var type = entry.Type;
        return type switch
        {
            _ when type == typeof(string) => TryParseString(entry, value, out output),
            _ when type == typeof(int) => TryParseNumber<int>(entry, value, out output),
            _ when type == typeof(uint) => TryParseNumber<uint>(entry, value, out output),
            _ when type == typeof(long) => TryParseNumber<long>(entry, value, out output),
            _ when type == typeof(ulong) => TryParseNumber<ulong>(entry, value, out output),
            _ when type == typeof(float) => TryParseNumber<float>(entry, value, out output),
            _ when type == typeof(double) => TryParseNumber<double>(entry, value, out output),
            _ => throw new NotSupportedException($"Type {entry.Type.Name} not supported")
        };
    }

    /// <summary>
    ///
    /// </summary>
    /// <param name="entry"></param>
    /// <param name="value"></param>
    /// <param name="output"></param>
    /// <returns></returns>
    [PublicAPI]
    public static LocId? GetPlaceholder(IQuickDialogEntry entry)
    {
        var type = entry.Type;
        return type switch
        {
            _ when type == typeof(int) => "quick-dialog-ui-placeholder-integer",
            _ when type == typeof(uint) => "quick-dialog-ui-placeholder-integer",
            _ when type == typeof(long) => "quick-dialog-ui-placeholder-integer",
            _ when type == typeof(ulong) => "quick-dialog-ui-placeholder-integer",
            _ when type == typeof(float) => "quick-dialog-ui-placeholder-float",
            _ when type == typeof(double) => "quick-dialog-ui-placeholder-float",
            _ when type == typeof(string) => "quick-dialog-ui-placeholder-text",
            _ => throw new NotSupportedException($"Type {entry.Type.Name} not supported")
        };
    }

    /// <summary>
    ///
    /// </summary>
    /// <param name="entry"></param>
    /// <returns></returns>
    [PublicAPI]
    public static (object min, object max) GetMinMax(IQuickDialogEntry entry)
    {
        var type = entry.Type;
        return type switch
        {
            _ when type == typeof(string) => GetMinMax<int>(entry),
            _ when type == typeof(int) => GetMinMax<int>(entry),
            _ when type == typeof(uint) => GetMinMax<uint>(entry),
            _ when type == typeof(long) => GetMinMax<long>(entry),
            _ when type == typeof(ulong) => GetMinMax<ulong>(entry),
            _ when type == typeof(float) => GetMinMax<float>(entry),
            _ when type == typeof(double) => GetMinMax<double>(entry),
            _ => throw new NotSupportedException($"Type {entry.Type.Name} not supported")
        };
    }
}
