using Content.Shared.Body.Surgery.Operation.Step;
using Content.Shared.Body.Surgery.Surgeon;
using Content.Shared.Body.Surgery.Target;
using Content.Shared.Notification.Managers;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Body.Surgery.Tool.Behaviors
{
    public class StepSurgery : ISurgeryBehavior
    {
        private SurgerySystem SurgerySystem => EntitySystem.Get<SurgerySystem>();

        [DataField("step", customTypeSerializer: typeof(PrototypeIdSerializer<SurgeryStepPrototype>))]
        private string? StepId { get; } = default;

        private SurgeryStepPrototype? Step => StepId == null
            ? null
            : IoCManager.Resolve<IPrototypeManager>().Index<SurgeryStepPrototype>(StepId);

        public bool CanPerform(SurgeonComponent surgeon, SurgeryTargetComponent target)
        {
            return Step != null && SurgerySystem.CanAddSurgeryTag(target, Step.ID);
        }

        public bool Perform(SurgeonComponent surgeon, SurgeryTargetComponent target)
        {
            return Step != null && SurgerySystem.TryAddSurgeryTag(target, Step.ID);
        }

        public void OnPerformDelayBegin(SurgeonComponent surgeon, SurgeryTargetComponent target)
        {
            var step = Step;

            if (step == null)
            {
                return;
            }

            SurgerySystem.DoBeginPopups(surgeon, target.Owner, step.LocId);
        }

        public void OnPerformSuccess(SurgeonComponent surgeon, SurgeryTargetComponent target)
        {
            var step = Step;

            if (step == null)
            {
                return;
            }

            SurgerySystem.DoSuccessPopups(surgeon, target.Owner, step.LocId);
        }

        public void OnPerformFail(SurgeonComponent surgeon, SurgeryTargetComponent target)
        {
            surgeon.Owner.PopupMessage(Loc.GetString("surgery-step-not-useful"));
        }
    }
}
