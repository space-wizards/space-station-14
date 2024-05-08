using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Medical.Blood.Components;



/// <summary>
/// This is a very simplified bleeding system that is intended for non-medically simulated entities.
/// It does not track blood-pressure, pulse, or have any blood type logic.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class BloodstreamComponent : Component
{
    /// <summary>
    ///     The next time that bleeds will be checked.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan NextUpdate;

    [DataField, AutoNetworkedField]
    public TimeSpan UpdateInterval = TimeSpan.FromSeconds(1.0f);

    /// <summary>
    /// How much blood is currently being lost by bleed per tick, this number must be positive
    /// </summary>
    [DataField, AutoNetworkedField]
    public FixedPoint2 Bloodloss = 0;

    [DataField, AutoNetworkedField]
    public FixedPoint2 BleedPuddleThreshold = 1.0f;

    /// <summary>
    /// How much blood is regenerated per tick, this number must be positive
    /// </summary>
    [DataField, AutoNetworkedField]
    public FixedPoint2 Regen = 0;

    /// <summary>
    /// What volume should we cut off blood regeneration at
    /// if this is negative use maxVolume
    /// </summary>
    [DataField, AutoNetworkedField]
    public FixedPoint2 RegenCutoffVolume = -1;

    /// <summary>
    /// Cached/starting volume for this bloodstream
    /// If this starts as negative it will use MaxVolume
    /// </summary>
    [DataField, AutoNetworkedField]
    public FixedPoint2 Volume = -1;

    /// <summary>
    /// The maximum volume for this bloodstream
    /// </summary>
    [DataField, AutoNetworkedField] //TODO: Required
    public FixedPoint2 MaxVolume = 200;

    public const string BloodSolutionId = "bloodstream";

    public const string DissolvedReagentSolutionId = "bloodReagents";

    public const string SpillSolutionId = "bloodSpill";

    /// <summary>
    /// The reagent prototypeId that this entity uses for blood
    /// </summary>
    [DataField, AutoNetworkedField] //TODO: required
    public string? BloodReagent = "Blood";

    /// <summary>
    /// This is the primary blood reagent in this bloodstream
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly), AutoNetworkedField]
    public ReagentId BloodReagentId = default;

    /// <summary>
    /// The bloodstream solution
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? BloodSolutionEnt = null;

    /// <summary>
    /// The bloodstream solution
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? BloodRegentsSolutionEnt = null;

    /// <summary>
    /// The solution to "drip" onto the ground or soak into clothing
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? SpillSolutionEnt  = null;
}
