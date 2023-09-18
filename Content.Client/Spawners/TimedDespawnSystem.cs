using Content.Shared.Spawners.EntitySystems;

namespace Content.Client.Spawners;

public sealed class TimedDespawnSystem : SharedTimedDespawnSystem
{
    protected override bool CanDelete(EntityUid uid)
    {
        return IsClientSide(uid);
    }
}
