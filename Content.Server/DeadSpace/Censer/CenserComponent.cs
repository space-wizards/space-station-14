using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Damage;

namespace Content.Server.DeadSpace.Censer;

[RegisterComponent]
public sealed partial class CenserComponent : Component
{
    /// <summary>
    /// Solution name in SolutionContainerManager
    /// </summary>
    [DataField]
    public string SolutionName = "сenser";

    /// <summary>
    /// The reagent that is consumed
    /// </summary>
    [DataField]
    public ProtoId<ReagentPrototype> Reagent = "Holywater";

    /// <summary>
    /// The amount of reagent that is consumed
    /// </summary>
    [DataField]
    public FixedPoint2 Consumption = FixedPoint2.New(50.0f);

    /// <summary>
    /// DoAfter delay
    /// </summary>
    [DataField]
    public TimeSpan UsingDelay = TimeSpan.FromSeconds(10);

    /// <summary>
    /// The damage that will be dealt at the end of use
    /// </summary>
    [DataField("heailng", required: true)]
    [ViewVariables]
    public DamageSpecifier Damage = default!;
}
