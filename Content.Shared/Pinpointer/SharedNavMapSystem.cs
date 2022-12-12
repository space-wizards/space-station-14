using Robust.Shared.Serialization;

namespace Content.Shared.Pinpointer;

public abstract class SharedNavMapSystem : EntitySystem
{
    [Serializable, NetSerializable]
    protected sealed class NavMapComponentState : ComponentState
    {
        public Dictionary<Vector2i, List<Vector2[]>> TileData = new();
    }
}
