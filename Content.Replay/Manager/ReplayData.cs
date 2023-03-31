using Robust.Shared.GameStates;
using Robust.Shared.Replays;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Replay.Manager;

public sealed class ReplayData
{
    public readonly List<GameState> States;
    public readonly List<ReplayMessage> Messages;
    public readonly GameTick TickOffset;
    public readonly TimeSpan StartTime;
    public readonly TimeSpan Duration;
    public readonly CheckpointState[] Checkpoints;

    public int CurrentIndex;
    public GameTick LastApplied;

    public GameTick CurTick => new GameTick((uint) CurrentIndex + TickOffset.Value);
    public GameState CurState => States[CurrentIndex];
    public GameState? NextState => CurrentIndex + 1 < States.Count ? States[CurrentIndex + 1] : null;
    public ReplayMessage CurMessages => Messages[CurrentIndex];

    // TODO figure out a way to undo prototype and resource uploading. Currently uploading disables time rewinding....
    // or if I can't figure out a way: at least allow people to just rewind anyways but after first accepting a warning pop-up.
    // I imagine most of the time it won't cause issues, because admins will upload NEW prototypes instead of overwriting old ones.
    public bool RewindUnsafe = false;
    public bool OverrideUnsafeRewind = false;
    public bool RewindDisabled => RewindUnsafe && !OverrideUnsafeRewind;

    public ReplayData(
        List<GameState> states,
        List<ReplayMessage> messages,
        GameTick tickOffset,
        TimeSpan startTime,
        TimeSpan duration,
        CheckpointState[] checkpointStates)
    {
        States = states;
        Messages = messages;
        TickOffset = tickOffset;
        StartTime = startTime;
        Duration = duration;
        Checkpoints = checkpointStates;
    }
}

public readonly struct CheckpointState : IComparable<CheckpointState>
{
    public GameTick Tick => State.ToSequence;
    public readonly GameState State;
    public readonly (TimeSpan, GameTick) TimeBase;
    public readonly int Index;
    public readonly Dictionary<string, object> Cvars;

    public CheckpointState(GameState state, (TimeSpan, GameTick) time, Dictionary<string, object> cvars, int index)
    {
        State = state;
        TimeBase = time;
        Cvars = cvars.ShallowClone();
        Index = index;
    }

    /// <summary>
    ///     Get a dummy state for use with binary searches.
    /// </summary>
    public static CheckpointState DummyState(int index)
    {
        return new CheckpointState(index);
    }

    private CheckpointState(int index)
    {
        Index = index;
        State = default!;
        TimeBase = default!;
        Cvars = default!;
    }

    public int CompareTo(CheckpointState other) => Index.CompareTo(other.Index);
}
