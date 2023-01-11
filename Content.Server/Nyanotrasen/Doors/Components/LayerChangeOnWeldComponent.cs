using Content.Shared.Physics;

namespace Content.Server.Doors.Components
{
    [RegisterComponent]
    public sealed class LayerChangeOnWeldComponent : Component
    {
        [DataField("unweldedLayer")]
        public CollisionGroup UnweldedLayer = CollisionGroup.AirlockLayer;

        [DataField("weldedLayer")]
        public CollisionGroup WeldedLayer = CollisionGroup.WallLayer;
    }
}
