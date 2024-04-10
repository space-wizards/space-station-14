using Content.Shared.Storage;

namespace Content.Server.Tools.Innate
{
    [RegisterComponent]
    public sealed partial class InnateToolComponent : Component
    {
        /// <summary>
        /// Tools id list that'll be innate, unremoveable, will be in hands if possible
        /// </summary>
        [DataField]
        public List<EntitySpawnEntry> Tools = new();
        public List<EntityUid> ToolUids = new();
        public List<string> HandIds = new();
        public List<string> ToSpawn = new();
    }
}
