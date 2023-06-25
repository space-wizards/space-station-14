using Content.Server.Power.EntitySystems;

namespace Content.Server.Power.Components;

/// <summary>
///     Battery node on the pow3r network. Needs other components to connect to actual networks.
/// </summary>
[RegisterComponent]
public sealed class BatteryComponent : Component
{
    /// <summary>
    /// Maximum charge of the battery in joules (ie. watt seconds)
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("maxCharge")]
    [Access(typeof(BatterySystem), Other = AccessPermissions.Read)]
    public float MaxCharge;

    /// <summary>
    /// Current charge of the battery in joules (ie. watt seconds)
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("startingCharge")]
    [Access(typeof(BatterySystem), Other = AccessPermissions.Read)]
    public float Charge;

    /// <summary>
    /// True if the battery is fully charged.
    /// </summary>
    [ViewVariables] public bool IsFullyCharged => MathHelper.CloseToPercent(Charge, MaxCharge);

    /// <summary>
    /// The price per one joule. Default is 1 credit for 10kJ.
    /// </summary>
    [DataField("pricePerJoule")]
    [ViewVariables(VVAccess.ReadWrite)]
    public float PricePerJoule = 0.0001f;
}

/// <summary>
///     Raised when a battery's charge or capacity changes (capacity affects relative charge percentage).
/// </summary>
[ByRefEvent]
public readonly record struct ChargeChangedEvent(float Charge, float MaxCharge);
