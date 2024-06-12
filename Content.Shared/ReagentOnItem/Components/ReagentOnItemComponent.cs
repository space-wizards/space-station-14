using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

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

[Serializable, NetSerializable]
public sealed class ReagentOnItemComponentState : ComponentState
{
    public readonly FixedPoint2 EffectStacks;
    public readonly FixedPoint2 MaxStacks;

    public ReagentOnItemComponentState(FixedPoint2 effectStacks, FixedPoint2 maxStacks)
    {
        EffectStacks = effectStacks;
        MaxStacks = maxStacks;
    }
}
