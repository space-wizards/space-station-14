using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;

namespace Content.Shared.Medical.Circulatory.Components;



/// <summary>
/// This is a very simplified bleeding system that is intended for non-humanoid/medically simulated entities.
/// It does not track blood-pressure, pulse, or have any blood type logic.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class BleedPoolComponent : Component
{
    [DataField, AutoNetworkedField]
    public TimeSpan NextUpdate = new TimeSpan();

    /// <summary>
    /// This is the value that blood gets changed by each update (aka Delta).
    /// This value can be positive or negative, for example: a positive value would be if the entity is receiving a transfusion
    /// </summary>
    [DataField, AutoNetworkedField]
    public FixedPoint2 BloodDelta = 0;

    /// <summary>
    /// The maximum volume for this blood pool
    /// </summary>
    [DataField, AutoNetworkedField] //TODO: Required
    public FixedPoint2 MaxVolume = 200;

    /// <summary>
    /// Reaching or going below this blood value results in this entity being set to crit
    /// </summary>
    [DataField, AutoNetworkedField] //TODO: Required
    public FixedPoint2 BloodlossCritThreshold = 50;

    /// <summary>
    /// Reaching or going below this blood value results in this entity being set to dead
    /// </summary>
    [DataField, AutoNetworkedField] //TODO: Required
    public FixedPoint2 BloodlossKillThreshold = 0;

    /// <summary>
    /// The reagent that this entity uses for blood, transfusions must be of this reagent type to have any affect.
    /// </summary>
    [DataField, AutoNetworkedField]
    public string BloodReagent = string.Empty;

    public const string BloodSolutionId = "bloodstream";

    public const string SpillSolutionId = "bloodSpill";

    /// <summary>
    /// The bloodstream solution
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? BloodSolution = null;

    /// <summary>
    /// The solution to "drip" onto the ground or soak into clothing
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? SpillSolution  = null;
}
