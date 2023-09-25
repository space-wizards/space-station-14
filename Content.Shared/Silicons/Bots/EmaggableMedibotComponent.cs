using Content.Shared.Chemistry.Reagent;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Silicons.Bots;

/// <summary>
/// Replaced the medibot's meds with these when emagged. Could be poison, could be fun.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(MedibotSystem))]
public sealed partial class EmaggableMedibotComponent : Component
{
    /// <summary>
    /// Med the bot will inject when UNDER the standard med damage threshold.
    /// </summary>
    [DataField("standardMed", customTypeSerializer: typeof(PrototypeIdSerializer<ReagentPrototype>)), ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public string StandardMed = "Tricordrazine";

    [DataField("standardMedAmount"), ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public float StandardMedAmount = 15f;

    /// <summary>
    /// Med the bot will inject when OVER the emergency med damage threshold.
    /// </summary>
    [DataField("emergencyMed", customTypeSerializer: typeof(PrototypeIdSerializer<ReagentPrototype>)), ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public string EmergencyMed = "Inaprovaline";

    [DataField("emergencyMedAmount"), ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public float EmergencyMedAmount = 15f;

    /// <summary>
    /// Sound to play when the bot has been emagged
    /// </summary>
    [DataField("sparkSound")] public SoundSpecifier SparkSound = new SoundCollectionSpecifier("sparks")
    {
        Params = AudioParams.Default.WithVolume(8f),
    };
}
