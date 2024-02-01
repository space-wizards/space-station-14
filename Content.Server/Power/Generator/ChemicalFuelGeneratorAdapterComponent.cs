using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Power.Generator;

/// <summary>
/// This is used for chemical fuel input into generators.
/// </summary>
[RegisterComponent, Access(typeof(GeneratorSystem))]
public sealed partial class ChemicalFuelGeneratorAdapterComponent : Component
{
    /// <summary>
    /// A dictionary relating a reagent to accept as fuel to a value to multiply reagent amount by to get fuel amount.
    /// </summary>
    [DataField]
    public Dictionary<ProtoId<ReagentPrototype>, float> Reagents = new();

    /// <summary>
    /// The name of <see cref="Solution"/>.
    /// </summary>
    [DataField("solution")]
    [ViewVariables(VVAccess.ReadWrite)]
    public string SolutionName = "tank";

    /// <summary>
    /// The solution on the <see cref="SolutionContainerManagerComponent"/> to use.
    /// </summary>
    [DataField("solutionRef")]
    public Entity<SolutionComponent>? Solution = null;

    /// <summary>
    /// How much reagent (can be fractional) is left in the generator.
    /// Stored in units of <see cref="FixedPoint2.Epsilon"/>.
    /// </summary>
    [DataField]
    public Dictionary<ProtoId<ReagentPrototype>, float> FractionalReagents = new();
}
