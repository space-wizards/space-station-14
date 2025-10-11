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
    public string SolutionName = "battery";

    /// <summary>
    /// Maximum charge of the battery in joules (ie. watt seconds)
    /// </summary>
    [DataField]
    [GuidebookData]
    public float MaxCharge;

    /// <summary>
    /// Current charge of the battery in joules (ie. watt seconds)
    /// </summary>
    [DataField("startingCharge")]
    public float CurrentCharge;

    /// <summary>
    /// The price per one joule. Default is 1 credit for 10kJ.
    /// </summary>
    [DataField]
    public float PricePerJoule = 0.0001f;
}
