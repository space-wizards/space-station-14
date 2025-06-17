using Content.Shared.Alert;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Changeling.Components;

/// <summary>
/// Component given to entities that use changeling biomass.
/// </summary>
[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState, AutoGenerateComponentPause]
public sealed partial class ChangelingBiomassComponent : Component
{
    /// <summary>
    /// Whether biomass should be able to go above the maximum.
    /// </summary>
    [DataField]
    public bool SoftCapMaximum = true;

    /// <summary>
    /// The maximum biomass.
    /// Counts as 100% for the alert sprites.
    /// </summary>
    [DataField]
    public float MaxBiomass = 100f;

    /// <summary>
    /// The current biomass the user has.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float CurrentBiomass = 100f;

    /// <summary>
    /// Minimum biomass, the value cannot go lower than this.
    /// </summary>
    [DataField]
    public float MinBiomass = 0f;

    /// <summary>
    /// The amount of biomass that gets substracted every UpdateInterval.
    /// </summary>
    [DataField]
    public float BiomassDecay = 0.41f; // Takes slightly above 20 minutes to decay from full biomass.

    /// <summary>
    /// The amount of states the biomass alert has.
    /// </summary>
    [DataField]
    public int BiomassLayerStates = 16;

    /// <summary>
    /// Should the entity be gibbed when it runs out of biomass?
    /// </summary>
    [DataField]
    public bool GibOnEmpty = true; // TODO: Should be replaced with true form.

    // TODO: Symptoms to happen when your biomass gets lower. Such as popups, vomiting etc.

    [DataField]
    public ProtoId<AlertPrototype> BiomassAlert = "ChangelingBiomassAlert";

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoPausedField]
    public TimeSpan NextUpdate = TimeSpan.Zero;

    [DataField]
    public TimeSpan UpdateInterval = TimeSpan.FromSeconds(5);

    // Prevent third party tools from figuring out who a changeling is based on the fact they own this component.
    public override bool SendOnlyToOwner => true;
}

