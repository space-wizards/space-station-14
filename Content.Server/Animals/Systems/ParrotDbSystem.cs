using System.Threading.Tasks;
using Content.Server.Administration.Logs;
using Content.Server.Animals.Components;
using Content.Server.Database;
using Content.Server.GameTicking;
using Content.Shared.Database;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Players.PlayTimeTracking;
using Robust.Server.Player;
using Robust.Shared.Network;
using Robust.Shared.Timing;

namespace Content.Server.Animals.Systems;

/// <summary>
/// If an entity has a ParrotDbMemoryComponent, this system periodically fills the ParrotMemoryComponent with
/// entries from the database, creating an inter-round parrot.
/// </summary>
public sealed partial class ParrotDbSystem : EntitySystem
{
    [Dependency] private readonly GameTicker _ticker = default!;
    [Dependency] private readonly IAdminLogManager _adminLogger = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IServerDbManager _db = default!;
    [Dependency] private readonly ISharedPlaytimeManager _playtimeManager = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ParrotDbMemoryComponent, LearnEvent>(OnLearn);
    }

    private void OnLearn(Entity<ParrotDbMemoryComponent> entity, ref LearnEvent args)
    {
        TrySaveMessageDb(entity, args.Message, args.SourcePlayer);
    }

    /// <summary>
    /// Attempt to save a message to the database
    ///
    /// This contains a few checks to prevent garbage from filling the database
    /// </summary>
    private void TrySaveMessageDb(Entity<ParrotDbMemoryComponent> entity, string message, EntityUid sourcePlayer)
    {
        // return if the message source entity does not have a MindContainerComponent This should mean only
        // player-controllable entities can commit messages to the database.
        //
        // Polly is likely to have a ParrotDbMemoryComponent, and is likely to be near stuff like EngiDrobes, so this
        // should prevent the database filling up with "Afraid of radiation? Then wear yellow!" etc.
        if (!TryComp<MindContainerComponent>(sourcePlayer, out var mindContainer))
            return;

        // return if this mindcontainer has no mind. Could happen with cogni'd entities that aren't player controlled yet
        if (!mindContainer.HasMind)
            return;

        // return if the mind entity has no mind component. Should not happen
        if (!TryComp<MindComponent>(mindContainer.Mind, out var mindComponent))
            return;

        // get the player sessionID
        if (!_playerManager.TryGetSessionById(mindComponent.UserId, out var session))
            return;

        // check player playtime before committing message
        var playtime = _playtimeManager.GetPlayTimes(session);

        // return if the player is missing an overall playtime for whatever reason
        if (!playtime.TryGetValue(PlayTimeTrackingShared.TrackerOverall, out var overallPlaytime))
            return;

        // return if the player has too little playtime
        if (overallPlaytime < entity.Comp.MinimumSourcePlaytime)
            return;

        SaveMessageDb(entity, message, session.UserId);
    }

    public void SaveMessageDb(
        EntityUid entity,
        string message,
        Guid sourcePlayerGuid)
    {
        // add a log line confirming that an entry was added to the database
        _adminLogger.Add(LogType.Chat, LogImpact.Low, $"Parroting entity {ToPrettyString(entity):entity} is saving the phrase \"{message}\" to database.");

        var currentRoundId = _ticker.RoundId;

        // actually save the message to the database
        _db.AddParrotMemory(message, sourcePlayerGuid, currentRoundId);
    }

    /// <summary>
    /// Updates the messages stored in ParrotMemoryComponent by retrieving fresh ones from the database
    /// </summary>
    /// <param name="entity"></param>
    public async Task RefreshMemoryFromDb(Entity<ParrotMemoryComponent, ParrotDbMemoryComponent> entity)
    {
        // get an enum for new messages
        var memories = _db.GetRandomParrotMemories(entity.Comp1.MaxSpeechMemory);

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

            dbMemory.NextRefresh += dbMemory.RefreshInterval;

            Task.Run(async () => await RefreshMemoryFromDb((uid, memory, dbMemory)));
        }
    }
}
