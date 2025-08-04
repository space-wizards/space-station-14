using System.Collections.Concurrent;
using System.Linq;
using Content.Shared.CCVar;
using NetCord;
using NetCord.Gateway;

namespace Content.Server.Discord.DiscordLink;

public sealed partial class DiscordLink
{
    [ViewVariables]
    private readonly ConcurrentDictionary<string, StatusRef> _statuses = new();
    [ViewVariables]
    private int _currentStatusIndex = 0;
    [ViewVariables]
    private bool _statusEnabled = true;
    [ViewVariables]
    private int _swapDelay = 10;
    [ViewVariables]
    private DateTime _nextSwap;

    /// <summary>
    /// The next time a presence update is allowed
    /// </summary>
    [ViewVariables]
    private DateTime _nextUpdate = DateTime.MinValue;

    /// <summary>
    /// Internal rate limit for updates
    /// </summary>
    [ViewVariables]
    private readonly TimeSpan _rateLimit = TimeSpan.FromSeconds(5);

    private async void UpdateStatus()
    {
        try
        {
            if (!_statusEnabled || !IsConnected)
                return;

            // Rate limit check
            if (DateTime.Now < _nextUpdate)
                return;

            var statuses = GetAllActiveStatuses();
            if (statuses.Count == 0)
                return;

            var status = statuses[_currentStatusIndex % statuses.Count];

            if (DateTime.Now < _nextSwap && !status.Dirty)
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

            _nextUpdate = DateTime.Now.Add(_rateLimit);

            if (status.Dirty) // Was this a dirty?
            {
                _sawmill.Verbose("Status was dirty!");
                status.Clean();
            }

            if (DateTime.Now > _nextSwap)
            {
                // Was this a swap? if so, go to next status.
                _sawmill.Verbose("Cycling to next status!");
                _nextSwap = DateTime.Now.AddSeconds(_swapDelay + status.ShowFor);
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
            @ref.IsValid = false;

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
        if (_swapDelay < 1) // I fear that swapping too quickly results in ratelimits.
            throw new InvalidOperationException("Delay may not be under 1 second.");

        _swapDelay = obj;
    }

    private async void ClearStatus()
    {
        try
        {
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
/// Used for setting the bots status message.
/// </summary>
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
    /// If this reference is still valid
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public bool IsValid { get; internal set; } = true;

    /// <summary>
    /// How long this status will show for. Value will be added with <see cref="CCVars.DiscordStatusSwapBaseDelay"/>
    /// Defaults to 0.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public int ShowFor { get; set; } = 0;

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
    /// Ensures that the ref is still valid before setting a field. This method sets the <see cref="LastUpdated"/> field.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when the status is, in fact, not valid.</exception>
    private void EnsureValid()
    {
        if (!IsValid)
            throw new InvalidOperationException("StatusRef has been dropped");

        Dirty = true;
    }

    internal void Invalidate()
    {
        IsValid = false;
    }
}
