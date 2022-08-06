using Content.Shared.Spawners.EntitySystems;

namespace Content.Server.Spawners.EntitySystems;

public sealed class TimedDespawnSystem : SharedTimedDespawnSystem
{
    protected override bool CanDelete(EntityUid uid)
    {
        return true;
    }
}
