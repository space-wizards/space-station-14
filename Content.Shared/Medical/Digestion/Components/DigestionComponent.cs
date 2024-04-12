using Content.Shared.FixedPoint;
using Content.Shared.Whitelist;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Medical.Digestion.Components;

/// <summary>
/// This is used for...
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class DigestionComponent : Component
{
    //Container ID that contains entities that are being digested
    public const string ContainerId = "DigestionContainer";
    //Solution ID that contains digester reagent and the reagents from a digested entity
    public const string SolutionId = "DigestionSolution";

    /// <summary>
    /// The entity that contains the bloodstream we are absorbing chemicals into
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? TargetBloodstream;


    /// <summary>
    /// The entity that contains the bloodstream we are absorbing chemicals into
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityWhitelist DigestibleEntities;


    /// <summary>
    /// The ID for the bloodstream solution we are absorbing chemicals into
    /// </summary>
    [DataField, AutoNetworkedField]
    public string BloodstreamId = "Bloodstream";

    /// <summary>
    /// Maximum chemical volume in the digester
    /// </summary>
    [DataField, AutoNetworkedField]
    public FixedPoint2 MaximumVolume = 900;

    /// <summary>
    /// The reagent secreted by this digester and used to digest entities.
    /// </summary>
    [DataField(required: true), AutoNetworkedField] //TODO: required
    public string DigesterReagent = "Water"; // TODO: ProtoID, implement hydrochloric acid

    /// <summary>
    /// The optimal ratio of digester reagent to be present in the digester
    /// </summary>
    [DataField, AutoNetworkedField]
    public FixedPoint2 OptimalDigesterPercentage = 0.3;

    /// <summary>
    /// What solution percentage of maximum should the digester reagent stop being regenerated to prevent overflows.
    /// This prevents bursting the digester from natural reagent regeneration.
    /// </summary>
    [DataField, AutoNetworkedField]
    public FixedPoint2 DigesterRegenCutoffPercentage = 0.9;

    /// <summary>
    /// How much Digester reagent to regenerate per second until the optimal amount is present
    /// </summary>
    [DataField, AutoNetworkedField]
    public FixedPoint2 DigesterRegenRate = 0.5;

    /// <summary>
    /// Cached volume of digestionSolution
    /// </summary>
    [DataField, AutoNetworkedField]
    public FixedPoint2 Volume = 0;

    /// <summary>
    ///     The next time that reagents will be metabolized.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan NextUpdate;

    [DataField, AutoNetworkedField]
    public TimeSpan UpdateInterval = TimeSpan.FromSeconds(1.0f);
}
