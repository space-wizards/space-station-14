using Content.Server.Storage;
using Content.Shared.Storage;

namespace Content.Server.Drone.Components
{
    [RegisterComponent]
    public sealed class DroneComponent : Component
    {
        [DataField("tools")] public List<EntitySpawnEntry> Tools = new();
        public List<EntityUid> ToolUids = new();
        public bool AlreadyAwoken = false;
        public float InteractionBlockRange = 2.15f;

        /// <summary>
        /// If you are using drone component for
        /// something that shouldn't have restrictions set this to
        /// false.
        /// </summary>
        [DataField("applyLaws")]
        public bool ApplyLaws = true;
    }
}
