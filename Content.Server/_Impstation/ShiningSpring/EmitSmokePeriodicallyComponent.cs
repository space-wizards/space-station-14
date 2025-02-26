using Content.Shared.Chemistry.Components;
using Robust.Shared.Prototypes;

namespace Content.Server._Impstation.ShiningSpring;

/// <summary>
/// Used to have an entity emit smoke periodically.
/// </summary>
[RegisterComponent]
public sealed partial class EmitSmokePeriodicallyComponent : Component
{
    /// <summary>
    /// How long the smoke stays for, after it has spread.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float Duration = 10;

    /// <summary>
    /// How much the smoke will spread.
    /// </summary>
    [DataField(required: true), ViewVariables(VVAccess.ReadWrite)]
    public int SpreadAmount;

    /// <summary>
    /// Smoke entity to spawn.
    /// Defaults to smoke but you can use foam if you want.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public EntProtoId SmokePrototype = "Smoke";

    /// <summary>
    /// Solution to add to each smoke cloud.
    /// </summary>
    /// <remarks>
    /// When using repeating trigger this essentially gets multiplied so dont do anything crazy like omnizine or lexorin.
    /// </remarks>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public Solution Solution = new();

    /// <summary>
    /// time between smoke emissions
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float EmissionPeriod = 0.1f;

    /// <summary>
    /// time since the entity last emitted smoke
    /// </summary>
    [DataField, ViewVariables]
    public float timer = 0.1f;
}
