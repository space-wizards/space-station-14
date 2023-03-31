using NetSerializer;
using Robust.Shared.GameStates;
using Robust.Shared.Network;
using Robust.Shared.Replays;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using System.Linq;
using static Robust.Shared.Replays.ReplayMessage;

namespace Content.Replay.Manager;

// This partial class contains codes for generating "checkpoint" states, which are basically just full states that allow
// the client to jump to some point in time without having to re-process the whole replay up to that point.
// I.e., so that when jumping to tick 1001 the client only has to apply states for tick 1000 and 1001, instead of 0, 1, 2, ....
public sealed partial class ReplayManager
{
    public CheckpointState GetLastCheckpoint(ReplayData data, int index)
    {
        var target = CheckpointState.DummyState(index);
        var checkpointIndex = Array.BinarySearch(data.Checkpoints, target);

        if (checkpointIndex < 0)
            checkpointIndex = Math.Max(0, (~checkpointIndex) - 1);

        var checkpoint = data.Checkpoints[checkpointIndex];
        DebugTools.Assert(checkpoint.Index <= index);
        DebugTools.Assert(checkpointIndex == data.Checkpoints.Length - 1 || data.Checkpoints[checkpointIndex + 1].Index > index);
        return checkpoint;
    }

    public CheckpointState GetNextCheckpoint(ReplayData data, int index)
    {
        var target = CheckpointState.DummyState(index);
        var checkpointIndex = Array.BinarySearch(data.Checkpoints, target);

        if (checkpointIndex < 0)
            checkpointIndex = Math.Max(0, (~checkpointIndex) - 1);

        checkpointIndex = Math.Clamp(checkpointIndex + 1, 0, data.Checkpoints.Length - 1);

        var checkpoint = data.Checkpoints[checkpointIndex];
        DebugTools.Assert(checkpoint.Index >= index || checkpointIndex == data.Checkpoints.Length - 1);
        return checkpoint;
    }

    private CheckpointState[] GenerateCheckpoints(HashSet<string> initialCvars, List<GameState> states, List<ReplayMessage> messages)
    {
        // given a set of states [0 to X], [X to X+1], [X+1 to X+2] ... we want to generate additional states like [0
        // to x+60 ], [0 to x+120], etc. This will make scrubbing/jumping to a state much faster, but requires some
        // pre-processing all of the states.
        // TODO REPLAYS maybe only generate checkpoints as the replay gets played back, instead of pre-processing?
        //
        // This whole mess of a function uses a painful amount of LINQ conversion. but sadly the networked data is
        // generally sent as a list of values, which makes sense if the list contains simple state delta data that all
        // needs to be applied. But here we need to inspect existing states and combine/merge them, so things generally
        // need to be converted into a dictionary.But even with that requirement there are a bunch of performance
        // improvements to be made even without just de-LINQuifing or changing the networked data.
        //
        // TODO REPLAYS Add dynamic checkpoints.
        // If we end up using long (e.g., 5 minute) checkpoint intervals, that might still mean that scrubbing/rewinding
        // short time periods will be super stuttery. So its probably worth keeping a dynamic checkpoint following the
        // users current tick. E.g. while a replay is being replayed, keep a dynamic checkpoint that is ~30 secs behind
        // the current tick. that way the user can always go back up to ~30 seconds without having to go back to the
        // last checkpoint.
        //
        // Alternatively maybe just generate reverse states? I.e. states containing data that is required to go from
        // tick X to X-1? (currently any ent that had any changes will reset ALL of its components, not just the states
        // that actually need resetting. basically: iterate forwards though states. anytime a new  comp state gets
        // applied, for the reverse state simply add the previously applied component state.

        Dictionary<string, object> cvars = new();
        foreach (var cvar in initialCvars)
        {
            cvars[cvar] = _netConf.GetCVar<object>(cvar);
        }

        var timeBase = _timing.TimeBase;
        var checkPoints = new List<CheckpointState>(1 + states.Count / _checkpointInterval);
        var state0 = states[0];
        checkPoints.Add(new CheckpointState(state0, timeBase, cvars, 0));

        var entSpan = state0.EntityStates.Span;
        Dictionary<EntityUid, EntityState> entStates = new(entSpan.Length);
        foreach (var entState in entSpan)
        {
            entStates.Add(entState.Uid, entState);
        }

        var playerSpan = state0.PlayerStates.Span;
        Dictionary<NetUserId, PlayerState> playerStates = new(playerSpan.Length);
        foreach (var player in playerSpan)
        {
            playerStates.Add(player.UserId, player);
        }

        HashSet<EntityUid> deletions = new();

        int ticksSinceLastCheckpoint = 0;
        int spawnedTracker = 0;
        int stateTracker = 0;
        for (int i = 1; i < states.Count; i++)
        {
            var curState = states[i];
            UpdatePlayerStates(curState.PlayerStates.Span, playerStates);
            UpdateDeletions(curState.EntityDeletions, entStates, deletions);
            UpdateEntityStates(curState.EntityStates.Span, entStates, ref spawnedTracker, ref stateTracker);
            UpdateCvars(messages[i], cvars, ref timeBase);
            ticksSinceLastCheckpoint++;

            DebugTools.Assert(!deletions.Intersect(entStates.Keys).Any());

            if (ticksSinceLastCheckpoint < _checkpointInterval && spawnedTracker < _checkpointEntitySpawnThreshold && stateTracker < _checkpointEntityStateThreshold)
                continue;

            ticksSinceLastCheckpoint = 0;
            spawnedTracker = 0;
            stateTracker = 0;
            var newState = new GameState(GameTick.Zero,
                curState.ToSequence,
                default,
                entStates.Values.ToArray(),
                playerStates.Values.ToArray(),
                Array.Empty<EntityUid>()); // for full states, deletions are implicit by simply not being in the state
            checkPoints.Add(new CheckpointState(newState, timeBase, cvars, i));
        }

        return checkPoints.ToArray();
    }

    private void UpdateDeletions(NetListAsArray<EntityUid> entityDeletions, Dictionary<EntityUid, EntityState> entStates, HashSet<EntityUid> deletions)
    {
        foreach (var ent in entityDeletions.Span)
        {
            entStates.Remove(ent);
            deletions.Add(ent);
        }
    }

    private void UpdateCvars(ReplayMessage replayMessage, Dictionary<string, object> cvars, ref (TimeSpan, GameTick) timeBase)
    {
        foreach (var message in replayMessage.Messages)
        {
            if (message is not CvarChangeMsg cvarMsg)
                continue;

            foreach (var (name, value) in cvarMsg.ReplicatedCvars)
            {
                cvars[name] = value;
            }

            timeBase = cvarMsg.TimeBase;
        }
    }

    private void UpdateEntityStates(ReadOnlySpan<EntityState> span, Dictionary<EntityUid, EntityState> entStates,  ref int spawnedTracker, ref int stateTracker)
    {
        foreach (var entState in span)
        {
            if (!entStates.TryGetValue(entState.Uid, out var oldEntState))
            {
                entStates[entState.Uid] = entState;
                spawnedTracker++;

#if DEBUG
                foreach (var state in entState.ComponentChanges.Span)
                {
                    DebugTools.Assert(state.State is not IComponentDeltaState delta || delta.FullState);
                }
#endif
                continue;
            }

            stateTracker++;
            DebugTools.Assert(oldEntState.Uid == entState.Uid);
            var newCompStates = entState.ComponentChanges.Value.ToDictionary(x => x.NetID);
            var combinedCompStates = oldEntState.ComponentChanges.Value.ToList();

            // remove any deleted components
            if (entState.NetComponents != null)
            {
                for (var index = combinedCompStates.Count - 1; index >= 0; index--)
                {
                    if (!entState.NetComponents.Contains(combinedCompStates[index].NetID))
                        combinedCompStates.RemoveSwap(index);
                }
            }

            for (var index = combinedCompStates.Count - 1; index >= 0; index--)
            {
                var existing = combinedCompStates[index];

                if (!newCompStates.TryGetValue(existing.NetID, out var newCompState))
                    continue;

                if (newCompState.State is not IComponentDeltaState delta || delta.FullState)
                {
                    combinedCompStates[index] = newCompState;
                    continue;
                }

                DebugTools.Assert(existing.State is IComponentDeltaState fullDelta && fullDelta.FullState);
                combinedCompStates[index] = new ComponentChange(existing.NetID, delta.CreateNewFullState(existing.State), newCompState.LastModifiedTick);
            }

            entStates[entState.Uid] = new EntityState(entState.Uid, combinedCompStates, entState.EntityLastModified, entState.NetComponents ?? oldEntState.NetComponents);

#if DEBUG
            foreach (var state in combinedCompStates)
            {
                DebugTools.Assert(state.State is not IComponentDeltaState delta || delta.FullState);
            }
#endif
        }
    }

    private void UpdatePlayerStates(ReadOnlySpan<PlayerState> span, Dictionary<NetUserId, PlayerState> playerStates)
    {
        foreach (var player in span)
        {
            playerStates[player.UserId] = player;
        }
    }
}
