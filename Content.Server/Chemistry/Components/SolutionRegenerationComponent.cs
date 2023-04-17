using Content.Server.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Chemistry.Components;

/// <summary>
/// Passively increases a solution's quantity of a reagent.
/// </summary>
[RegisterComponent]
[Access(typeof(SolutionRegenerationSystem))]
public sealed class SolutionRegenerationComponent : Component
{
    /// <summary>
    /// The solution to have reagent added to.
    /// </summary>
    [DataField("solution", required: true), ViewVariables(VVAccess.ReadWrite)]
    public string Solution = string.Empty;

    /// <summary>
    /// The reagent to be regenerated in the solution.
    /// </summary>
    [DataField("reagent", required: true, customTypeSerializer: typeof(PrototypeIdSerializer<ReagentPrototype>)), ViewVariables(VVAccess.ReadWrite)]
    public string Reagent = string.Empty;

    /// <summary>
    /// Quantity of the reagent regenerated every time.
    /// </summary>
    [DataField("quantity"), ViewVariables(VVAccess.ReadWrite)]
    public FixedPoint2 Quantity = FixedPoint2.New(1f);

    /// <summary>
    /// How long it takes to regenerate once.
    /// </summary>
    [DataField("duration"), ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan Duration = TimeSpan.FromSeconds(1);

    /// <summary>
    /// The time when the next regeneration will occur.
    /// </summary>
    [DataField("nextChargeTime", customTypeSerializer: typeof(TimeOffsetSerializer)), ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan NextRegenTime = TimeSpan.FromSeconds(0);
}
