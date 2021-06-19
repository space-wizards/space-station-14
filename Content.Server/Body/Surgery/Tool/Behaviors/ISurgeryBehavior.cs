using Content.Shared.Body.Surgery.Surgeon;
using Content.Shared.Body.Surgery.Target;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.Body.Surgery.Tool.Behaviors
{
    [ImplicitDataDefinitionForInheritors]
    public interface ISurgeryBehavior
    {
        bool CanPerform(SurgeonComponent surgeon, SurgeryTargetComponent target);

        bool Perform(SurgeonComponent surgeon, SurgeryTargetComponent target);

        /// <summary>
        ///     Called when a delay is started to perform this behaviour.
        /// </summary>
        /// <param name="surgeon">The surgeon that will perform the operation.</param>
        /// <param name="target">The target of the operation.</param>
        void OnPerformDelayBegin(SurgeonComponent surgeon, SurgeryTargetComponent target)
        {
        }

        /// <summary>
        ///     Called when the operation succeeds.
        /// </summary>
        /// <param name="surgeon">The surgeon that performed the operation.</param>
        /// <param name="target">The target of the operation.</param>
        void OnPerformSuccess(SurgeonComponent surgeon, SurgeryTargetComponent target)
        {
        }

        /// <summary>
        ///     Called when the operation fails.
        /// </summary>
        /// <param name="surgeon">The surgeon that failed the operation.</param>
        /// <param name="target">The target of the operation.</param>
        void OnPerformFail(SurgeonComponent surgeon, SurgeryTargetComponent target)
        {
        }
    }
}
