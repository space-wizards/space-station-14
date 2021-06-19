using System.Linq;
using Content.Shared.Body.Part;
using Content.Shared.Body.Surgery.Surgeon;
using Content.Shared.Body.Surgery.Target;

namespace Content.Shared.Body.Surgery.Operation.Effect
{
    public class OrganExtractionEffect : IOperationEffect
    {
        public void Execute(SurgeonComponent surgeon, SurgeryTargetComponent target)
        {
            // TODO BODY choose organ
            if (surgeon.Mechanism == null ||
                !target.Owner.TryGetComponent(out SharedBodyPartComponent? part) ||
                part.Mechanisms.FirstOrDefault() is not { } mechanism)
            {
                return;
            }

            part.RemoveMechanism(mechanism);
        }
    }
}
