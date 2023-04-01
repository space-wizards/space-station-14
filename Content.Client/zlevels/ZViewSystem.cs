using Content.Shared._Afterlight.ThirdDimension;
using Robust.Shared.Map;

namespace Content.Client.zlevels;

public sealed class ZViewSystem : SharedZViewSystem
{
    public override EntityUid SpawnViewEnt(EntityUid source, MapCoordinates loc)
    {
        throw new NotImplementedException();
    }

    public override bool CanSetup(EntityUid source)
    {
        return false;
    }
}
