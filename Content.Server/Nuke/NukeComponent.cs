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
        public int Timer = 5;

        [DataField("slot")]
        public string DiskSlotName = "DiskSlot";

        [DataField("blastRadius")]
        public int BlastRadius = 200;

        public float RemainingTime;
        public bool DiskInserted = false;
        public NukeStatus Status = NukeStatus.AWAIT_DISK;
        public string EnteredCode = "";
    }
}
