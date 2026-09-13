using Content.Server.Administration.Logs;
using Content.Server.Administration.Systems;
using Content.Server.Animals.Components;
using Content.Server.Database;
using Content.Server.GameTicking.Events;
using Content.Server.GameTicking;
using Content.Shared.Animals.Components;
using Content.Shared.CCVar;
using Content.Shared.Database;
using Content.Shared.Players.PlayTimeTracking;
using Robust.Shared.Configuration;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Server.Animals.Systems;

/// <summary>
/// If an entity has a ParrotDbMemoryComponent, this system periodically fills the ParrotMemoryComponent with
/// entries from the database, creating an inter-round parrot.
/// </summary>
public sealed partial class ParrotDbSystem : EntitySystem
{
    [Dependency] private readonly ActorSystem _actor = default!;
    [Dependency] private readonly GameTicker _ticker = default!;
    [Dependency] private readonly IAdminLogManager _adminLogger = default!;
    [Dependency] private readonly IConfigurationManager _config = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly IServerDbManager _db = default!;
    [Dependency] private readonly ISharedPlaytimeManager _playtimeManager = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ParrotDbMemoryComponent, LearnEvent>(OnLearn);

        SubscribeLocalEvent<RoundStartingEvent>(OnRoundStarting);

        SubscribeLocalEvent<EraseEvent>(OnErase);
    }

    /// <summary>
    /// Called when a player is erased. This ensures the memories from that player are blocked and the parrot memory is
    /// refreshed
    /// </summary>
    private void OnErase(ref EraseEvent args)
    {
        var sourcePlayerUserId = args.PlayerNetUserId;

        // this task should make sure the blocking of all parrot messages completes properly, then after that the parrot
        // memories are refreshed
        BlockPlayerMemories(sourcePlayerUserId);
    }

    private async void BlockPlayerMemories(NetUserId sourcePlayerUserId)
    {
        await _db.SetParrotMemoryBlockPlayer(sourcePlayerUserId, true);

        // refresh the memories of all parrots with a memorydb so that they can keep yapping undisturbed
        var query = EntityQueryEnumerator<ParrotMemoryComponent, ParrotDbMemoryComponent>();
        while (query.MoveNext(out var uid, out var memory, out var dbMemory))
        {
            RefreshMemoryFromDb((uid, memory, dbMemory));
        }
    }

    /// <summary>
    /// Called when a round has started. We do some DB cleaning here
    /// </summary>
    private async void OnRoundStarting(RoundStartingEvent args)
    {
        await _db.TruncateParrotMemory(_config.GetCVar(CCVars.ParrotMaximumMemoryAge));
    }

    /// <summary>
    /// Called when an entity with a ParrotMemoryComponent learns something
    /// </summary>
    private void OnLearn(Entity<ParrotDbMemoryComponent> entity, ref LearnEvent args)
    {
        TrySaveMemoryDb(entity, args.Message, args.SourcePlayer);
    }

    /// <summary>
    /// Attempt to save a memory to the database
    ///
    /// This contains a few checks to prevent garbage from filling the database
    /// </summary>
    private void TrySaveMemoryDb(Entity<ParrotDbMemoryComponent> entity, string message, EntityUid sourcePlayer)
    {
        // first get the playerId. We want to be able to control these memories and clean them when someone says something
        // bad, so all memories must have an associated player with this implementation
        if (!_actor.TryGetSession(sourcePlayer, out var session))
            return;

        if (session is null)
            return;

        // check player playtime before committing memory
        var playtime = _playtimeManager.GetPlayTimes(session);

        if (!playtime.TryGetValue(PlayTimeTrackingShared.TrackerOverall, out var overallPlaytime))
            return;

        if (overallPlaytime < _config.GetCVar(CCVars.ParrotMinimumPlaytimeFilter))
            return;

        SaveMemoryDb(entity, message, session.UserId);
    }

    private async void SaveMemoryDb(
        EntityUid entity,
        string message,
        NetUserId sourcePlayerGuid)
    {
        // add a log line confirming that an entry was added to the database
        _adminLogger.Add(LogType.Chat, LogImpact.Low, $"Parroting entity {ToPrettyString(entity):entity} is saving the phrase \"{message}\" to database.");

        var currentRoundId = _ticker.RoundId;

        // actually save the message to the database
        await _db.AddParrotMemory(message, sourcePlayerGuid, currentRoundId);
    }

    /// <summary>
    /// Updates the messages stored in ParrotMemoryComponent by retrieving fresh ones from the database
    /// </summary>
    /// <param name="entity"></param>
    public async void RefreshMemoryFromDb(Entity<ParrotMemoryComponent, ParrotDbMemoryComponent> entity)
    {
        // get an enum for new messages
        var memories = _db.GetRandomParrotMemories(_config.GetCVar(CCVars.ParrotDbRefreshNumMemories));

        // There are some edge cases where the database may not be full enough yet to fill the memory.
        // Ensure that the memory is always filled up to capacity first, and only after start replacing
        // existing messages.
        var idx = 0;
        await foreach (var newMemory in memories)
        {
            var speechMemory = new SpeechMemory(new NetUserId(newMemory.SourcePlayer), newMemory.Text);

            // if the memory is not full yet, add to it
            if (entity.Comp1.SpeechMemories.Count < entity.Comp1.MaxSpeechMemory)
            {
                entity.Comp1.SpeechMemories.Add(speechMemory);
                continue;
            }

            // otherwise, replace old entries
            entity.Comp1.SpeechMemories[idx] = speechMemory;
            idx += 1;
        }
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var gameTime = _gameTiming.CurTime;

        var query = EntityQueryEnumerator<ParrotMemoryComponent, ParrotDbMemoryComponent>();
        while (query.MoveNext(out var uid, out var memory, out var dbMemory))
        {
            if (dbMemory.NextRefresh > gameTime)
                continue;

            dbMemory.NextRefresh += _config.GetCVar(CCVars.ParrotDbRefreshInterval);

            RefreshMemoryFromDb((uid, memory, dbMemory));
        }
    }
}
