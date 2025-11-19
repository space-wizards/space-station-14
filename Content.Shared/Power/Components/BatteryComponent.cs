using Content.Shared.Power.EntitySystems;
using Content.Shared.Guidebook;

namespace Content.Shared.Power.Components;

/// <summary>
/// Battery node on the pow3r network. Needs other components to connect to actual networks.
/// </summary>
[RegisterComponent]
[Virtual]
[Access(typeof(SharedBatterySystem))]
public partial class BatteryComponent : Component
{
    /// <summary>
    /// Maximum charge of the battery in joules (i.e. watt seconds)
    /// </summary>
    [DataField]
    [GuidebookData]
    public float MaxCharge;

    /// <summary>
    /// Current charge of the battery in joules (ie. watt seconds)
    /// </summary>
    [DataField("startingCharge")] // TODO: rename this datafield to currentCharge
    public float CurrentCharge;

    /// <summary>
    /// The price per one joule. Default is 1 speso for 10kJ.
    /// </summary>
    [DataField]
    public float PricePerJoule = 0.0001f;
}
