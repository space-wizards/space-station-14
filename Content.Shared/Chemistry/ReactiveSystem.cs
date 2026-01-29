using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;

namespace Content.Shared.Chemistry;

[UsedImplicitly]
public sealed class ReactiveSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _proto = default!;

    public void DoEntityReaction(EntityUid uid, Solution solution, ReactionMethod method)
    {
        foreach (var reagent in solution.Contents.ToArray())
        {
            ReactionEntity(uid, method, reagent);
        }
    }

    public void ReactionEntity(EntityUid uid, ReactionMethod method, ReagentQuantity reagentQuantity)
    {
        if (reagentQuantity.Quantity == FixedPoint2.Zero)
            return;

        // We throw if the reagent specified doesn't exist.
        if (!_proto.Resolve<ReagentPrototype>(reagentQuantity.Reagent.Prototype, out var proto))
            return;

        var ev = new ReactionEntityEvent(method, reagentQuantity, proto);
        RaiseLocalEvent(uid, ref ev);
    }
}
public enum ReactionMethod
{
Touch,
Injection,
Ingestion,
}

[ByRefEvent]
public readonly record struct ReactionEntityEvent(ReactionMethod Method, ReagentQuantity ReagentQuantity, ReagentPrototype Reagent);
