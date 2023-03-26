using Content.Server.Mind.Components;
using Content.Shared.GameTicking;

namespace Content.Server.Mind
{
    /// <summary>
    /// This is absolutely evil.
    /// It tracks all mind changes and logs all the Mind objects.
    /// This is so that when round end comes around, there's a coherent list of all Minds that were in play during the round.
    /// The Minds themselves contain metadata about their owners.
    /// Anyway, this is because disconnected people and ghost roles have been breaking round end statistics for way too long.
    /// </summary>
    public sealed class MindTrackerSystem : EntitySystem
    {
        [ViewVariables]
        public readonly HashSet<Mind> AllMinds = new();

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<RoundRestartCleanupEvent>(Reset);
            SubscribeLocalEvent<MindComponent, MindAddedMessage>(OnMindAdded);
        }

        void Reset(RoundRestartCleanupEvent ev)
        {
            AllMinds.Clear();
        }

        void OnMindAdded(EntityUid uid, MindComponent mc, MindAddedMessage args)
        {
            var mind = mc.Mind;
            if (mind != null)
                AllMinds.Add(mind);
        }
    }
}

