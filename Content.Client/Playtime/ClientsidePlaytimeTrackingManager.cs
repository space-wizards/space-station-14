using Content.Shared.CCVar;
using Robust.Client.Player;
using Robust.Shared.Network;
using Robust.Shared.Configuration;
using Robust.Shared.Timing;

namespace Content.Client.Playtime;

/// <summary>
///     Keeps track of how long the player has played today.
/// </summary>
/// <remarks>
/// <para>
///     Playtime is treated as any time in which the player is attached to an entity.
///     This notably excludes scenarios like the lobby.
/// </para>
/// </remarks>
public sealed class ClientsidePlaytimeTrackingManager
{
    [Dependency] private readonly IClientNetManager _clientNetManager = default!;
    [Dependency] private readonly IConfigurationManager _configurationManager = default!;
    [Dependency] private readonly ILogManager _logManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;

    private ISawmill _sawmill = default!;

    private const string InternalDateFormat = "yyyy-MM-dd";

    [ViewVariables]
    private TimeSpan? _mobAttachmentTime;

    /// <summary>
    /// The total amount of time played today, in minutes.
    /// </summary>
    [ViewVariables]
    public float PlaytimeMinutesToday
    {
        get
        {
            var cvarValue = _configurationManager.GetCVar(CCVars.PlaytimeMinutesToday);
            if (_mobAttachmentTime == null)
                return cvarValue;

            return cvarValue + (float)(_gameTiming.RealTime - _mobAttachmentTime.Value).TotalMinutes;
        }
    }

    public void Initialize()
    {
        _sawmill = _logManager.GetSawmill("clientplaytime");
        _clientNetManager.Connected += OnConnected;

        // The downside to relying on playerattached and playerdetached is that unsaved playtime won't be saved in the event of a crash
        // But then again, the config doesn't get saved in the event of a crash, either, so /shrug
        // Playerdetached gets called on quit, though, so at least that's covered.
        _playerManager.LocalPlayerAttached += OnPlayerAttached;
        _playerManager.LocalPlayerDetached += OnPlayerDetached;
    }

    private void OnConnected(object? sender, NetChannelArgs args)
    {
        var datatimey = DateTime.Now;
        _sawmill.Info($"Current day: {datatimey.Day} Current Date: {datatimey.Date.ToString(InternalDateFormat)}");

        var recordedDateString = _configurationManager.GetCVar(CCVars.PlaytimeLastConnectDate);
        var formattedDate = datatimey.Date.ToString(InternalDateFormat);

        if (formattedDate == recordedDateString)
            return;

        _configurationManager.SetCVar(CCVars.PlaytimeMinutesToday, 0);
        _configurationManager.SetCVar(CCVars.PlaytimeLastConnectDate, formattedDate);
    }

    private void OnPlayerAttached(EntityUid entity)
    {
        _mobAttachmentTime = _gameTiming.RealTime;
    }

    private void OnPlayerDetached(EntityUid entity)
    {
        if (_mobAttachmentTime == null)
            return;

        var newTimeValue = PlaytimeMinutesToday;

        _mobAttachmentTime = null;

        var timeDiffMinutes = newTimeValue - _configurationManager.GetCVar(CCVars.PlaytimeMinutesToday);
        if (timeDiffMinutes < 0)
        {
            _sawmill.Error("Time differential on player detachment somehow less than zero!");
            return;
        }

        // At less than 1 minute of time diff, there's not much point, and saving regardless will brick tests
        // The reason this isn't checking for 0 is because TotalMinutes is fractional, rather than solely whole minutes
        if (timeDiffMinutes < 1)
            return;

        _configurationManager.SetCVar(CCVars.PlaytimeMinutesToday, newTimeValue);

        _sawmill.Info($"Recorded {timeDiffMinutes} minutes of living playtime!");

        _configurationManager.SaveToFile(); // We don't like that we have to save the entire config just to store playtime stats '^'
    }
}
