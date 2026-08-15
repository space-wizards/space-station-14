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

        dialogs.Remove(msg.DialogId);

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
    }

    /// <summary>
    ///
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <typeparam name="T1"></typeparam>
    /// <param name="entry"></param>
    /// <param name="input"></param>
    /// <param name="output"></param>
    /// <returns></returns>
    private static bool TryParse<T, T1>(IQuickDialogEntry entry, string input, [NotNullWhen(true)] out object? output)
        where T : INumber<T>
        where T1 : notnull
    {
        output = default;

        if (entry is not IQuickDialogEntry<T, T1> typedEntry)
            return false;

        if (!typedEntry.TryParse(input, out var result))
            return false;

        output = result;
        return true;
    }

    /// <summary>
    ///
    /// </summary>
    /// <param name="entry"></param>
    /// <param name="input"></param>
    /// <param name="output"></param>
    /// <returns></returns>
    [PublicAPI]
    public static bool TryParse(IQuickDialogEntry entry, string input, [NotNullWhen(true)] out object? output)
    {
        output = null;

        var type = entry.Type;
        return type switch
        {
            _ when type == typeof(string) => TryParse<int, string>(entry, input, out output),
            _ when type == typeof(int) => TryParse<int, int>(entry, input, out output),
            _ when type == typeof(uint) => TryParse<uint, uint>(entry, input, out output),
            _ when type == typeof(long) => TryParse<long, long>(entry, input, out output),
            _ when type == typeof(ulong) => TryParse<ulong, ulong>(entry, input, out output),
            _ when type == typeof(float) => TryParse<float, float>(entry, input, out output),
            _ when type == typeof(double) => TryParse<double, double>(entry, input, out output),
            _ => throw new NotSupportedException($"Type {entry.Type} not supported")
        };
    }

    /// <summary>
    ///
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <typeparam name="T1"></typeparam>
    /// <param name="entry"></param>
    /// <returns></returns>
    private static (object Min, object Max) GetMinMax<T, T1>(IQuickDialogEntry entry)
        where T : INumber<T>
        where T1 : notnull
    {
        if (entry is not IQuickDialogEntry<T, T1> typedEntry)
            return default;

        return typedEntry.MinMax;
    }

    /// <summary>
    ///
    /// </summary>
    /// <param name="entry"></param>
    /// <returns></returns>
    [PublicAPI]
    public static (object Min, object Max) GetMinMax(IQuickDialogEntry entry)
    {
        var type = entry.Type;
        return type switch
        {
            _ when type == typeof(string) => GetMinMax<int, string>(entry),
            _ when type == typeof(int) => GetMinMax<int, int>(entry),
            _ when type == typeof(uint) => GetMinMax<uint, uint>(entry),
            _ when type == typeof(long) => GetMinMax<long, long>(entry),
            _ when type == typeof(ulong) => GetMinMax<ulong, ulong>(entry),
            _ when type == typeof(float) => GetMinMax<float, float>(entry),
            _ when type == typeof(double) => GetMinMax<double, double>(entry),
            _ => throw new NotSupportedException($"Type {entry.Type} not supported")
        };
    }

    /// <summary>
    ///
    /// </summary>
    /// <param name="entry"></param>
    /// <param name="input"></param>
    /// <param name="output"></param>
    /// <returns></returns>
    [PublicAPI]
    public static LocId? GetPlaceholder(IQuickDialogEntry entry)
    {
        var type = entry.Type;
        return type switch
        {
            _ when type == typeof(string) => "quick-dialog-ui-placeholder-text",
            _ when type == typeof(int) => "quick-dialog-ui-placeholder-integer",
            _ when type == typeof(uint) => "quick-dialog-ui-placeholder-integer",
            _ when type == typeof(long) => "quick-dialog-ui-placeholder-integer",
            _ when type == typeof(ulong) => "quick-dialog-ui-placeholder-integer",
            _ when type == typeof(float) => "quick-dialog-ui-placeholder-float",
            _ when type == typeof(double) => "quick-dialog-ui-placeholder-float",
            _ => throw new NotSupportedException($"Type {entry.Type} not supported")
        };
    }
}
