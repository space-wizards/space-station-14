using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;

namespace Content.Shared.Lathe;

/// <summary>
/// Makes a lathe work faster by consuming space lube with each recipe.
/// </summary>
[RegisterComponent, Access(typeof(LatheLubeSystem))]
public sealed partial class LatheLubeComponent : Component
{
    /// <summary>
    /// Solution that will be checked.
    /// Any non-lube is ignored.
    /// </summary>
    [DataField]
    public string Solution = "lube";

    /// <summary>
    /// Reagent to use as lubricant.
    /// </summary>
    [DataField]
    public ProtoId<ReagentPrototype> Reagent = "SpaceLube";

    /// <summary>
    /// How much reagent from the solution to use up for reach recipe.
    /// </summary>
    [DataField]
    public FixedPoint2 Cost = 5;

    /// <summary>
    /// How much to reduce production time by with full lube consumption.
    /// With less lube it gets closer to 1x speed.
    /// </summary>
    [DataField]
    public float Reduction = 0.75f;
}
