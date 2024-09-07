using Robust.Shared.Player;

namespace Content.Shared.Replays;

public abstract class SharedReplayEventSystem : EntitySystem
{
    public virtual void RecordReplayEvent(ReplayEvent replayEvent, EntityUid? source = null) { }

    public virtual ReplayEventPlayer GetPlayerInfo(EntityUid player)
    {
        throw new InvalidOperationException(); // Overwritten in server side system
    }

    public virtual ReplayEventPlayer GetPlayerInfo(ICommonSession session)
    {
        throw new InvalidOperationException(); // Overwritten in server side system
    }
}
