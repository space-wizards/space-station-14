using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Shared.Body.Surgery.Operation.Step.Insertion;

[ImplicitDataDefinitionForInheritors]
public interface IInsertionHandler
{
    public bool TryInsert(EntityUid part, EntityUid item);
}
