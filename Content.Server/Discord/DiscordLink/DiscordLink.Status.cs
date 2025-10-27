using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using Content.Shared.CCVar;
using NetCord;
using NetCord.Gateway;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Server.Discord.DiscordLink;

public sealed partial class DiscordLink
{
    /// <summary>
    /// The current list of statutes, the key being their identifier when first created using <see cref="GetOrCreateStatusRef"/>.
    /// The value being the status itself. Items in this dictionary are guaranteed to be valid.
    /// </summary>
    [ViewVariables]
    private readonly ConcurrentDictionary<string, StatusRef> _statuses = new();
    /// <summary>
    /// A number used to index into <see cref="_statuses"/>. This number is incremented every time the status changes.
    /// </summary>
    /// <remarks>
    /// While it is called an index, it does not actually represent a direct index into the <see cref="_statuses"/> dictionary.
    /// The actual status is determined by taking the remainder of this number and the count of statutes to ensure that there is never an invalid index.
    /// </remarks>
    [ViewVariables]
    private int _currentStatusIndex = 0;
    /// <summary>
    /// If the status system is enabled. This is set by <see cref="CCVars.DiscordStatusEnabled"/>
    /// </summary>
    [ViewVariables]
    private bool _statusEnabled = true;
    /// <summary>
    /// The base time (in seconds) for swapping statutes. Set by <see cref="CCVars.DiscordStatusSwapBaseDelay"/>
    /// </summary>
    /// <remarks>
    /// Note that this is added on top of what the statuses themselves can set.
    /// </remarks>
    [ViewVariables]
    private TimeSpan _swapDelay = TimeSpan.FromSeconds(10);
    /// <summary>
    /// The time until we next swap our status. Relative to <see cref="IGameTiming.RealTime"/>
    /// </summary>
    [ViewVariables]
    private TimeSpan _nextSwap;

    /// <summary>
    /// The next time a presence update is allowed.
    /// </summary>
    [ViewVariables]
    private TimeSpan _nextUpdate = TimeSpan.MinValue;

    /// <summary>
    /// Internal rate limit for updates
    /// </summary>
    [ViewVariables]
    private static readonly TimeSpan RateLimit = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Lock used within <see cref="UpdateStatus"/> and <see cref="ClearStatus"/> to ensure that both don't run at the same time.
    /// </summary>
    private readonly SemaphoreSlim _statusLock = new(1, 1);

    private async void UpdateStatus()
    {
        try
        {
            if (!_statusEnabled || !IsConnected)
                return;

            // Ensure we aren't setting a status while the link is being disabled or similar.
            using var waitGuardAsync = await _statusLock.WaitGuardAsync();

            // Rate limit check
            if (_gameTiming.RealTime < _nextUpdate)
                return;

            var statuses = GetAllActiveStatuses();
            if (statuses.Count == 0)
                return;

            var status = statuses[_currentStatusIndex % statuses.Count];

            if (_gameTiming.RealTime < _nextSwap && !status.Dirty)
                return;

            // no need to update if this is the only status and it isn't dirty
            if (!status.Dirty && statuses.Count == 1)
                return;

            _sawmill.Debug("Updating presence...");

            var props = new PresenceProperties(UserStatusType.Online);
            var activity = new UserActivityProperties(status.Message, status.Type);
            if (status.Type == UserActivityType.Custom)
            {
                // With the custom type, the state will be used as the message instead of the name.
                // Discord is weird.
                activity.WithState(status.Message);
            }

            props = props.AddActivities([activity]);
            await _client!.UpdatePresenceAsync(props);

            _nextUpdate = _gameTiming.RealTime + RateLimit;

            if (status.Dirty) // Was this a dirty?
            {
                _sawmill.Verbose("Status was dirty!");
                status.Clean();
            }

            if (_gameTiming.RealTime > _nextSwap)
            {
                // Was this a swap? if so, go to next status.
                _sawmill.Verbose("Cycling to next status!");
                _nextSwap = _swapDelay + status.ShowFor;
                _currentStatusIndex++;
            }
        }
        catch (Exception e)
        {
            _sawmill.Error("Failed setting status!", e);
        }
    }

    /// <summary>
    /// Returns all active status messages.
    /// </summary>
    public List<StatusRef> GetAllActiveStatuses()
    {
        return _statuses.Values.ToList();
    }

    /// <summary>
    /// Gets a status ref by id and adds it if it doesn't exist yet.
    /// </summary>
    public StatusRef GetOrCreateStatusRef([ForbidLiteral] string id)
    {
        return _statuses.GetOrAdd(id, _ => new StatusRef(id));
    }

    /// <summary>
    /// Gets a status ref by id. Returns null if there isn't one.
    /// </summary>
    public StatusRef? GetOrNullStatusRef([ForbidLiteral] string id)
    {
        if (_statuses.TryGetValue(id, out var @ref))
            return @ref;

        return null;
    }

    /// <summary>
    /// Drops a status ref by id.
    /// </summary>
    public void DropStatus([ForbidLiteral] string id)
    {
        var @ref = GetOrNullStatusRef(id);
        if (@ref != null)
            @ref.Invalidate();

        _statuses.TryRemove(id, out _);

        // If this was our last status, we need to empty our presence.
        if (_statuses.Count == 0)
            ClearStatus();
    }

    private void OnStatusChanged(bool obj)
    {
        _statusEnabled = obj;
        if (!obj && IsConnected)
        {
            ClearStatus();
        }
    }

    private void OnStatusSwapDelayChanged(int obj)
    {
        if (obj < 1) // I fear that swapping too quickly results in ratelimits.
            throw new InvalidOperationException("Delay may not be under 1 second.");

        _swapDelay = TimeSpan.FromSeconds(obj);
    }

    /// <summary>
    /// Clears the current active status. This does not disable the actual status logic, for that set the <see cref="CCVars.DiscordStatusEnabled"/> CVar
    /// </summary>
    private async void ClearStatus()
    {
        try
        {
            using var waitGuardAsync = await _statusLock.WaitGuardAsync();
            // Don't clear while an update is ongoing, it would otherwise just override the status we just cleared.

            // We are disabling status, so we clear our current one.
            await _client!.UpdatePresenceAsync(new PresenceProperties(UserStatusType.Online));
        }
        catch (Exception e)
        {
            _sawmill.Error("Failed clearing status!", e);
        }
    }
}

/// <summary>
/// Represents an active status for the Discord integration.
/// This is used by <see cref="DiscordLink.UpdateStatus"/>
/// </summary>
/// <remarks>
/// Even if <see cref="IsValid"/> is true, the status may not be shown if the Discord link is inactive or the status integration is disabled.
/// </remarks>
/// <exception cref="InvalidOperationException">
/// Setting any value while <see cref="IsValid"/> is false, will throw an exception.
/// </exception>
/// <example>
/// Getting a status ref and setting it:
/// <code>
/// [Dependency] private readonly DiscordLink _discordLink = default!;
/// const string MyIdentifier = "MyFancyStatus";
///
/// StatusRef status = _discordLink.GetOrCreateStatusRef(MyIdentifier);
/// status.Type = UserActivityType.Playing;
/// status.Message = "Rain World";
/// </code>
/// In this example, ensure that you keep track of the status if you need to modify the contents. While you *can* just call <see cref="DiscordLink.GetOrCreateStatusRef"/> again, it is preferred that you store the status in a variable.
/// </example>
/// <seealso cref="DiscordLink"/>
/// <seealso cref="DiscordStatusLink"/>
public sealed class StatusRef
{
    /// <summary>
    /// The id of this status
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    public string Id { get; }

    [ViewVariables(VVAccess.ReadWrite)]
    private string _message = string.Empty;

    /// <summary>
    /// The message to display
    /// </summary>
    public string Message
    {
        get => EnsureIsValidAndReturn(_message);
        set
        {
            EnsureValid();
            _message = value;
        }
    }

    [ViewVariables(VVAccess.ReadWrite)]
    private UserActivityType _type = UserActivityType.Custom;
    public UserActivityType Type
    {
        get => EnsureIsValidAndReturn(_type);
        set
        {
            EnsureValid();
            _type = value;
        }
    }

    public void Clean()
    {
        Dirty = false;
    }

    /// <summary>
    /// Is this status dirty and needs to be set again?
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public bool Dirty { get; private set; }
    /// <summary>
    /// If this reference is still valid?
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public bool IsValid { get; private set; } = true;

    /// <summary>
    /// How long this status will show for. Value will be added with <see cref="CCVars.DiscordStatusSwapBaseDelay"/>
    /// Defaults to 0.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan ShowFor { get; set; } = TimeSpan.Zero;

    public StatusRef(string id)
    {
        Id = id;
    }

    private T EnsureIsValidAndReturn<T>(T value)
    {
        if (!IsValid)
            throw new InvalidOperationException("StatusRef has been dropped");

        return value;
    }

    /// <summary>
    /// Ensures that the ref is still valid before setting a field. This method sets the <see cref="Dirty"/> field.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when the status is, in fact, not valid.</exception>
    private void EnsureValid()
    {
        if (!IsValid)
            throw new InvalidOperationException("StatusRef has been dropped");

        Dirty = true;
    }

    public void Invalidate()
    {
        IsValid = false;
    }
}
