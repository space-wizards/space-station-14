using Content.Shared.Body.Surgery.Components;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Shared.Body.Surgery.Operation.Effect;

[ImplicitDataDefinitionForInheritors]
public interface IOperationEffect
{
    void Execute(EntityUid user, OperationComponent operation);
}
