using Content.Shared.Body.Surgery.Components;

namespace Content.Shared.Body.Surgery.Operation.Effect;

public sealed class AutopsyEffect : IOperationEffect
{
    public void Execute(EntityUid user, OperationComponent operation)
    {
        // TODO: store cause of death and then get it???? idfk
    }
}
