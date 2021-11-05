using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.Nuke
{
    [RegisterComponent]
    public class NukeComponent : Component
    {
        public override string Name => "Nuke";

        [DataField("minTime")]
        public int MinimumTime = 90;

        [DataField("maxTime")]
        public int MaximumTime = 3600;

        public int RemainingTime;
        public bool IsArmed = false;
        public bool DiskInserted = false;
    }
}
