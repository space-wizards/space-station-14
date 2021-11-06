using Content.Shared.Nuke;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.Nuke
{
    [RegisterComponent]
    public class NukeComponent : Component
    {
        public override string Name => "Nuke";

        [DataField("timer")]
        public int Timer = 120;

        public int RemainingTime;
        public bool IsArmed = false;
        public bool DiskInserted = false;
        public NukeStatus Status = NukeStatus.AWAIT_DISK;
        public string EnteredCode = "";
    }
}
