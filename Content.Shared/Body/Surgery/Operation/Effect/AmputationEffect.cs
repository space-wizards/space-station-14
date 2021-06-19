using Content.Shared.Body.Part;
using Content.Shared.Body.Surgery.Surgeon;
using Content.Shared.Body.Surgery.Target;

namespace Content.Shared.Body.Surgery.Operation.Effect
{
    public class AmputationEffect : IOperationEffect
    {
        public void Execute(SurgeonComponent surgeon, SurgeryTargetComponent target)
        {
            if (target.Owner.TryGetComponent(out SharedBodyPartComponent? part))
            {
                part.Body?.RemovePart(part);
            }
        }
    }
}
