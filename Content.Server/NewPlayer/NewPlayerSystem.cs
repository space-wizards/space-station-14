using Content.Server.Database;
using Content.Server.Players.PlayTimeTracking;
using Content.Server.Players.Whitelist;
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
    [Dependency] private WhitelistManager _whitelistManager = default!;

    private TimeSpan _newPlayerTimeTotal;

    /// <inheritdoc />
    public override void Initialize()
    {
        Subs.CVar(_config, CCVars.NewPlayerTimeTotalHours, v => _newPlayerTimeTotal = TimeSpan.FromHours(v), true);
    }

    [SubscribeLocalEvent]
    private void OnNewPlayerGetStateAttempt(Entity<NewPlayerIconComponent> entity, ref ComponentGetStateAttemptEvent args)
    {
        args.Cancelled = !(args.Player?.AttachedEntity is not { } uid || HasComp<ShowNewPlayerIconComponent>(uid));
    }

    /// <summary>
    /// This is a bit of a "hack" to ensure that the component states are properly updated in the event a component
    /// is removed out of when the client isn't here to receive it. It's taken from how SharedRevolutionarySystem does it.
    /// TODO: It's not ideal, and preferably we'd have a more solid way to handle specific session component handling.
    /// </summary>
    [SubscribeLocalEvent]
    private void OnComponentStartup(Entity<ShowNewPlayerIconComponent> entity, ref ComponentStartup args)
    {
        var newPlayerQuery = AllEntityQuery<NewPlayerIconComponent>();
        while (newPlayerQuery.MoveNext(out var uid, out var comp))
        {
            Dirty(uid, comp);
        }
    }

    // Async because it needs to run GetWhitelistStatusAsync. Seems to work!
    [SubscribeLocalEvent]
    private void OnPlayerSpawnComplete(PlayerSpawnCompleteEvent ev)
    {
        if (_whitelistManager.IsConnectedWhitelisted(ev.Player))
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
