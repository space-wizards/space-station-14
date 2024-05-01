using Content.Shared.FixedPoint;

namespace Content.Shared.ReagentOnItem;

[ImplicitDataDefinitionForInheritors]
public abstract partial class ReagentOnItemComponent : Component
{

    /// <summary>
    ///     This is the amount of reagent left on the item (Similar to fire stacks).
    ///     At zero it should remove itself from the item!
    /// </summary>
    [DataField]
    public FixedPoint2 EffectStacks;

    /// <summary>
    ///     This is the maximum stacks that the component can have.
    /// </summary>
    [DataField]
    public FixedPoint2 MaxStacks = 15;
}
