using Content.Server.Database;
using Content.Server.Players.PlayTimeTracking;
using Content.Shared.CCVar;
using Content.Shared.NewPlayer;
using Content.Shared.GameTicking;
using Robust.Shared.Configuration;

namespace Content.Server.NewPlayer;

public sealed partial class NewPlayerSystem : EntitySystem
{
    [Dependency] private SharedAppearanceSystem _appearance = default!;
    [Dependency] private IConfigurationManager _config = default!;
    [Dependency] private PlayTimeTrackingManager _playtimeManager = default!;
    [Dependency] private IServerDbManager _dbManager = default!;

    private TimeSpan _newPlayerTimeTotal;

    public override void Initialize()
    {
        Subs.CVar(_config, CCVars.NewPlayerTimeTotalHours, v => _newPlayerTimeTotal = TimeSpan.FromHours(v), true);
    }

    [SubscribeLocalEvent]
    private void OnComponentInit(Entity<NewPlayerLabelComponent> entity, ref ComponentInit args)
    {
        _appearance.SetData(entity, NewPlayerLayers.Layer, NewPlayerVisuals.NewTotal);
    }

    [SubscribeLocalEvent]
    private void OnComponentShutdown(Entity<NewPlayerLabelComponent> entity, ref ComponentShutdown args)
    {
        _appearance.RemoveData(entity, NewPlayerLayers.Layer);
    }

    [SubscribeLocalEvent]
    private async void OnPlayerSpawnComplete(PlayerSpawnCompleteEvent ev)
    {
        try
        {
            var totalTime = _playtimeManager.GetOverallPlaytime(ev.Player);

            if (totalTime < _newPlayerTimeTotal)
                EnsureComp<NewPlayerLabelComponent>(ev.Mob);
        }
        catch (Exception e)
        {
            Log.Error($"Error getting new player playtime:\n{e}");
        }

        // Must be whitelisted
        if (!await _dbManager.GetWhitelistStatusAsync(ev.Player.UserId))
            return;

        EnsureComp<SeeNewPlayersComponent>(ev.Mob);
    }
}
