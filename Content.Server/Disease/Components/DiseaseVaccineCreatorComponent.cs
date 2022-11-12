using Content.Shared.Construction.Prototypes;
using Robust.Shared.Audio;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Disease.Components;

/// <summary>
///     As of now, the disease server and R&D server are one and the same.
///     So, this is a research client that can look for DiseaseServer
///     on its connected server and print vaccines of the diseases stored there.
/// </summary>
[RegisterComponent]
public sealed class DiseaseVaccineCreatorComponent : Component
{
    public DiseaseServerComponent? DiseaseServer = null;

    /// <summary>
    /// Biomass cost per vaccine, scaled off of the machine part. (So T1 parts effectively reduce the default to 4.)
    /// Reduced by the part rating.
    /// </summary>
    [DataField("BaseBiomassCost")]
    public int BaseBiomassCost = 5;

    /// <summary>
    /// Current biomass cost, derived from the above.
    /// </summary>
    public int BiomassCost = 4;

    /// <summary>
    /// The machine part that reduces biomass cost.
    /// </summary>
    [DataField("machinePartCost", customTypeSerializer: typeof(PrototypeIdSerializer<MachinePartPrototype>))]
    public string MachinePartCost = "Manipulator";

    /// <summary>
    /// Current vaccines queued.
    /// </summary>
    public int Queued = 0;

    [DataField("runningSound")]
    public SoundSpecifier RunningSoundPath = new SoundPathSpecifier("/Audio/Machines/vaccinator_running.ogg");

}
