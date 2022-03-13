using Content.Server.Storage;

namespace Content.Server.Drone.Components
{
    [RegisterComponent]
    public sealed class DroneComponent : Component
    {
        [DataField("tools")] public List<EntitySpawnEntry> Tools = new();
        public List<EntityUid> ToolUids = new();
        public bool AlreadyAwoken = false;
        public float InteractionBlockRange = 2.5f;
    }
}
