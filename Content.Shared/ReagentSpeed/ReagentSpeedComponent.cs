using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;

namespace Content.Shared.ReagentSpeed;

/// <summary>
/// Makes a device work faster by consuming reagents on each use.
/// Other systems must use <see cref="ReagentSpeedSystem.ApplySpeed"/> for this to do anything.
/// </summary>
[RegisterComponent, Access(typeof(ReagentSpeedSystem))]
public sealed partial class ReagentSpeedComponent : Component
{
    /// <summary>
    /// Solution that will be checked.
    /// Anything that isn't in <c>Modifiers</c> is left alone.
    /// </summary>
    [DataField(required: true)]
    public string Solution = string.Empty;

    /// <summary>
    /// How much reagent from the solution to use up for each use.
    /// This is per-modifier-reagent and not shared between them.
    /// </summary>
    [DataField]
    public FixedPoint2 Cost = 5;

    /// <summary>
    /// Reagents and how much they modify speed at full purity.
    /// Small number means faster large number means slower.
    /// </summary>
    [DataField(required: true)]
    public Dictionary<ProtoId<ReagentPrototype>, float> Modifiers = new();
}
