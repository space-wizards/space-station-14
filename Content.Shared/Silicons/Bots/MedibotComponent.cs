using Content.Shared.Chemistry.Reagent;
using Robust.Shared.Audio;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Silicons.Bots;

/// <summary>
/// Used by the server for NPC medibot injection.
/// Currently no clientside prediction done, only exists in shared for emag handling.
/// </summary>
[RegisterComponent]
[Access(typeof(MedibotSystem))]
public sealed class MedibotComponent : Component
{
    /// <summary>
    /// Med the bot will inject when UNDER the standard med damage threshold.
    /// </summary>
    [DataField("standardMed", customTypeSerializer: typeof(PrototypeIdSerializer<ReagentPrototype>))]
    public string StandardMed = "Tricordrazine";

    [DataField("standardMedAmount")]
    public float StandardMedAmount = 15f;

    /// <summary>
    /// Med the bot will inject when OVER the emergency med damage threshold.
    /// </summary>
    [DataField("emergencyMed", customTypeSerializer: typeof(PrototypeIdSerializer<ReagentPrototype>))]
    public string EmergencyMed = "Inaprovaline";

    [DataField("emergencyMedAmount")]
    public float EmergencyMedAmount = 15f;

    /// <summary>
    /// Sound played after injecting a patient.
    /// </summary>
    [DataField("injectSound")]
    public SoundSpecifier InjectSound = new SoundPathSpecifier("/Audio/Items/hypospray.ogg");

    public const float StandardMedDamageThreshold = 50f;
    public const float EmergencyMedDamageThreshold = 100f;
}
