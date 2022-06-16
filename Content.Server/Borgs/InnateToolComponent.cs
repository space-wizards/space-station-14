using Content.Shared.Storage;

namespace Content.Server.Borgs
{
    [RegisterComponent]
    public sealed class InnateToolComponent : Component
    {
        [DataField("tools")] public List<EntitySpawnEntry> Tools = new();
        public List<EntityUid> ToolUids = new();
    }
}
