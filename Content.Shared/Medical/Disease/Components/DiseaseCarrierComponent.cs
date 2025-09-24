using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Medical.Disease;

/// <summary>
/// Networked component storing active diseases and immunity tokens.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class DiseaseCarrierComponent : Component
{
    /// <summary>
    /// Active diseases and their current stage.
    /// </summary>
    [DataField, AutoNetworkedField]
    public Dictionary<string, int> ActiveDiseases = [];

    /// <summary>
    /// Optional incubation end times per disease.
    /// Before this time, disease won't spread or show symptoms.
    /// </summary>
    [DataField, AutoNetworkedField]
    public Dictionary<string, TimeSpan> IncubatingUntil = [];

    /// <summary>
    /// Delay between disease processing ticks.
    /// </summary>
    [DataField]
    public TimeSpan TickDelay = TimeSpan.FromSeconds(2);

    /// <summary>
    /// Time when the next disease processing tick occurs.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan NextTick;

    /// <summary>
    /// Prototype IDs the entity is immune to and their immunity strength (0-1).
    /// Value represents the probability to block infection attempts for that disease.
    /// </summary>
    [DataField, AutoNetworkedField]
    public Dictionary<string, float> Immunity = [];

    /// <summary>
    /// Map of symptom prototype IDs to a suppression end time. Used to temporarily
    /// suppress (treat) symptoms without curing the underlying disease.
    /// </summary>
    [DataField, AutoNetworkedField]
    public Dictionary<string, TimeSpan> SuppressedSymptoms = [];

    /// <summary>
    /// Track components that were added by a disease so that cures can roll them back safely.
    /// </summary>
    [DataField]
    public Dictionary<string, HashSet<string>> AddedComponents = [];
}
