using Content.Shared.Radiation.Components;
using Robust.Shared.Map;
using Robust.Shared.Serialization;
using FloodFillSystem = Content.Shared.FloodFill.FloodFillSystem;

namespace Content.Shared.Radiation.Systems;

public abstract partial class SharedRadiationSystem : EntitySystem
{

    public override void Initialize()
    {
        base.Initialize();
        InitRadBlocking();
    }

}

[Serializable, NetSerializable]
public sealed class RadiationUpdate : EntityEventArgs
{
    public readonly Dictionary<EntityUid, Dictionary<Vector2i, float>> RadiationMap;
    public List<(Matrix3, Dictionary<Vector2i, float>)> SpaceMap;

    public RadiationUpdate(Dictionary<EntityUid, Dictionary<Vector2i, float>> radiationMap,
        List<(Matrix3, Dictionary<Vector2i, float>)> spaceMap)
    {
        RadiationMap = radiationMap;
        SpaceMap = spaceMap;
    }
}
