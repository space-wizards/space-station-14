using Content.Shared.Alert;
using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;
using Content.Shared.Interaction;

namespace Content.Server.Atmos.Components;

/// <summary>
/// Component to handle non-breathing gas interactions.
/// Detects gasses around entities and applies effects. (this is currently for damage to borgs but ¯\_(ツ)_/¯)
/// </summary>
[RegisterComponent]
public sealed partial class InGasComponent : Component
{

    /// <summary>
    ///     ID of gas to check for as an int. Defaults to water.
    /// </summary>
    [DataField("gasID"), ViewVariables(VVAccess.ReadWrite)]
    public int GasId = 9;

    ///  <summary>
    ///     amount of gas needed to trigger effect in mols.
    /// </summary>
    [DataField("gasThreshold"), ViewVariables(VVAccess.ReadWrite)]
    public float GasThreshold = 0.1f;

    /// <summary>
    ///   Whether the entity is damaged by water.
    ///   By default things are not
    /// </summary>
    [DataField("damagedByGas"), ViewVariables(VVAccess.ReadWrite)]
    public bool DamagedByGas = false;
    /// <summary>
    /// Damage caused by gas contact
    /// </summary>
    [DataField("damage"), ViewVariables(VVAccess.ReadWrite)]
    public DamageSpecifier Damage = default!;

    ///<summary>
    /// Prevents gibbing from gas damage, same purpose as the barotrauma one
    /// </summary>
    [DataField("maxDamage"), ViewVariables(VVAccess.ReadWrite)]
    public FixedPoint2 MaxDamage = 200;

    /// <summary>
    /// Used to track when damage starts/stops. Used in logs + the alert.
    /// </summary>
    [DataField]
    public bool TakingDamage = false;


     /// <summary>
    /// Tracks whether something is underwater specifically.
    /// </summary>
    [DataField]
    public bool InWater = false;

    /// <summary>
    /// The alert to send when the entity is damaged by gas.
    /// </summary>
    [DataField]
    public ProtoId<AlertPrototype> DamageAlert = "ShortCircuit";

    [DataField]
    public ProtoId<AlertCategoryPrototype> BreathingAlertCategory = "Breathing";
}
