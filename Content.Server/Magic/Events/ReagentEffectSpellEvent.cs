using Content.Shared.Actions;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;

namespace Content.Server.Magic.Events;

/// <summary>
///     Spell that applies a reagent effect to an entity.
/// </summary>
public sealed class ReagentEffectSpellEvent : PerformEntityTargetActionEvent
{
    [DataField("effects", required: true)]
    public List<ReagentEffect> Effects = default!;

    [DataField("quantity")]
    public FixedPoint2 Quantity = FixedPoint2.New(10);

    [DataField("method")]
    public ReactionMethod Method = ReactionMethod.Touch;
}
