using Content.Server.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Components;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server.Chemistry.Components;

/// <summary>
/// Passively increases a solution's quantity of a reagent.
/// </summary>
[RegisterComponent, AutoGenerateComponentPause]
[Access(typeof(SolutionRegenerationSystem))]
public sealed partial class SolutionRegenerationComponent : Component
{
    /// <summary>
    /// The name of the solution to add to.
    /// </summary>
    [DataField("solution", required: true), ViewVariables(VVAccess.ReadWrite)]
    public string SolutionName = string.Empty;

    /// <summary>
    /// The solution to add reagents to.
    /// </summary>
    [DataField("solutionRef")]
    public Entity<SolutionComponent>? Solution = null;

    /// <summary>
    /// The reagent(s) to be regenerated in the solution.
    /// </summary>
    [DataField("generated", required: true), ViewVariables(VVAccess.ReadWrite)]
    public Solution Generated = default!;

    /// <summary>
    /// How long it takes to regenerate once.
    /// </summary>
    [DataField("duration"), ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan Duration = TimeSpan.FromSeconds(1);

    /// <summary>
    /// The time when the next regeneration will occur.
    /// </summary>
    [DataField("nextChargeTime", customTypeSerializer: typeof(TimeOffsetSerializer)), ViewVariables(VVAccess.ReadWrite)]
    [AutoPausedField]
    public TimeSpan NextRegenTime = TimeSpan.FromSeconds(0);
}
