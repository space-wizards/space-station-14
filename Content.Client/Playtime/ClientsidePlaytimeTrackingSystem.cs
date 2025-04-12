using Content.Shared.CCVar;
using Content.Shared.Mobs.Components;
using Robust.Client.Player;
using Robust.Shared.Network;
using Robust.Shared.Configuration;

namespace Content.Client.Playtime;

public sealed class ClientsidePlaytimeTracking
{
    [Dependency] private readonly IClientNetManager _clientNetManager = default!;
    [Dependency] private readonly IConfigurationManager _configurationManager = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly ILogManager _logManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    private ISawmill _sawmill = default!;

    private readonly string _internalDateFormat = "yyyy-MM-dd";

    private DateTime _livingMobAttachmentTime = DateTime.MinValue;

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
        _sawmill.Info("Current day: " + datatimey.Day.ToString() + " Current Date: " + datatimey.Date.ToString(_internalDateFormat));

        var recordedDateString = _configurationManager.GetCVar(CCVars.LastConnectDate);
        var formattedDate = datatimey.Date.ToString(_internalDateFormat);

        if (formattedDate == recordedDateString)
            return;

        _configurationManager.SetCVar(CCVars.MinutesToday, 0);
        _configurationManager.SetCVar(CCVars.LastConnectDate, formattedDate);
    }

    private void OnPlayerAttached(EntityUid entity)
    {
        if (_entityManager.HasComponent<MobStateComponent>(entity)) // Ghosts and other OOC mobs can safely be assumed to lack MobStateComponents, while all other mobs will have them.
            _livingMobAttachmentTime = DateTime.UtcNow; // Don't want daylight savings to cause jank, so we use UtcNow instead of Now
        else
            _livingMobAttachmentTime = DateTime.MinValue;
    }
    private void OnPlayerDetached(EntityUid entity)
    {
        if (_livingMobAttachmentTime == DateTime.MinValue)
            return;

        var currentTime = TimeSpan.FromTicks(DateTime.UtcNow.Ticks);
        var possessedTime = TimeSpan.FromTicks(_livingMobAttachmentTime.Ticks);
        _livingMobAttachmentTime = DateTime.MinValue;

        var timeDiff = currentTime - possessedTime;
        if (timeDiff < TimeSpan.Zero)
            throw new Exception("Time differential on player detachment somehow less than zero!");

        _configurationManager.SetCVar(CCVars.MinutesToday, _configurationManager.GetCVar(CCVars.MinutesToday) + timeDiff.Minutes);
        _sawmill.Info("Recorded " + timeDiff.Minutes.ToString() + " minutes of living playtime!");

        // Tests in particular aren't gonna have a config. So don't save it in those cases.
        if (_configurationManager.IsCVarRegistered(CCVars.MinutesToday.Name) && _configurationManager.IsCVarRegistered(CCVars.LastConnectDate.Name))
            _configurationManager.SaveToFile(); // We don't like that we have to save the entire config just to store playtime stats '^'
    }
}
