using Content.Shared.Body.Surgery.Surgeon;
using Content.Shared.Body.Surgery.Target;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.Body.Surgery.Tool.Behaviors
{
    public class Cauterization : ISurgeryBehavior
    {
        private SurgerySystem SurgerySystem => EntitySystem.Get<SurgerySystem>();

        [DataField("locId")]
        private string? LocId { get; } = null;

        public bool CanPerform(SurgeonComponent surgeon, SurgeryTargetComponent target)
        {
            return SurgerySystem.IsPerformingSurgeryOn(surgeon, target);
        }

        public bool Perform(SurgeonComponent surgeon, SurgeryTargetComponent target)
        {
            return SurgerySystem.StopSurgery(surgeon, target);
        }

        public void OnPerformDelayBegin(SurgeonComponent surgeon, SurgeryTargetComponent target)
        {
            if (LocId == null)
            {
                return;
            }

            SurgerySystem.DoBeginPopups(surgeon, target.Owner, LocId);
        }

        public void OnPerformSuccess(SurgeonComponent surgeon, SurgeryTargetComponent target)
        {
            if (LocId == null)
            {
                return;
            }

            SurgerySystem.DoSuccessPopups(surgeon, target.Owner, LocId);
        }
    }
}
