using Content.Shared.Body.Surgery.Surgeon;
using Content.Shared.Body.Surgery.Target;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Shared.Body.Surgery.Operation.Effect
{
    [ImplicitDataDefinitionForInheritors]
    public interface IOperationEffect
    {
        void Execute(SurgeonComponent surgeon, SurgeryTargetComponent target);
    }
}
