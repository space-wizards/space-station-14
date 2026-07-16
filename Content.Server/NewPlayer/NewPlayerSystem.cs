using Content.Server.Database;
using Content.Server.Players.PlayTimeTracking;
using Content.Shared.CCVar;
using Content.Shared.NewPlayer;
using Content.Shared.GameTicking;
using Robust.Shared.Configuration;
using Robust.Shared.GameStates;

namespace Content.Server.NewPlayer;

/// <summary>
/// Used to set "new player" icons, and to manage the components to only have them visible to players with <see cref="ShowNewPlayerIconComponent"/>.
/// </summary>
public sealed partial class NewPlayerSystem : EntitySystem
{
    [Dependency] private IConfigurationManager _config = default!;
    [Dependency] private PlayTimeTrackingManager _playtimeManager = default!;
    [Dependency] private IServerDbManager _dbManager = default!;

    private TimeSpan _newPlayerTimeTotal;

    public override void Initialize()
    {
        Subs.CVar(_config, CCVars.NewPlayerTimeTotalHours, v => _newPlayerTimeTotal = TimeSpan.FromHours(v), true);
    }

    [SubscribeLocalEvent]
    private void OnNewPlayerGetStateAttempt(Entity<NewPlayerIconComponent> entity, ref ComponentGetStateAttemptEvent args)
    {
        args.Cancelled = !(args.Player?.AttachedEntity is not { } uid || HasComp<ShowNewPlayerIconComponent>(uid));
    }

    [SubscribeLocalEvent]
    private async void OnPlayerSpawnComplete(PlayerSpawnCompleteEvent ev)
    {
        if (await _dbManager.GetWhitelistStatusAsync(ev.Player.UserId))
            EnsureComp<ShowNewPlayerIconComponent>(ev.Mob);

        try
        {
            var totalTime = _playtimeManager.GetOverallPlaytime(ev.Player);

            if (totalTime < _newPlayerTimeTotal)
                EnsureComp<NewPlayerIconComponent>(ev.Mob);
        }
        catch (Exception e)
        {
            Log.Error($"Error getting new player playtime:\n{e}");
        }
    }
}
