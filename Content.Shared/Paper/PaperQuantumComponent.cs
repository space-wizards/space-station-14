using Content.Shared.Damage;
using Content.Shared.Explosion;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Paper;

/// <summary>
/// Extends the paper with the pseudo-quantum functionality.
/// Quantum papers can be split in two immutable copies which synchronize stamps placed on them
/// and allow to teleport items up to some small weight by lighting one of the entangled copies.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, Access(typeof(SharedPaperQuantumSystem))]
public sealed partial class PaperQuantumComponent : Component
{
    /// <summary>
    /// Prototype to use for another quantum paper, instantiated during the quantum split.
    /// </summary>
    [DataField]
    public EntProtoId QuantumPaperProto = "PaperQuantum";

    /// <summary>
    /// Verb for performing quantum split — an operation that can be used once
    /// on the paper to separate it into two entangled copies.
    /// </summary>
    [DataField]
    public string SplitVerb = "paper-quantum-split-verb";

    /// <summary>
    /// How long does it take to perform quantum split?
    /// </summary>
    [DataField]
    public TimeSpan SplitDuration = TimeSpan.FromSeconds(4f);

    /// <summary>
    /// Popup to show to the player when he starts the quantum split.
    /// </summary>
    [DataField]
    public string PopupSplitSelf = "paper-quantum-popup-split-self";

    /// <summary>
    /// Popup to show to other players when someone starts the quantum split.
    /// </summary>
    [DataField]
    public string PopupSplitOther = "paper-quantum-popup-split-other";

    /// <summary>
    /// Updated name of the original paper after the quantum split.
    /// </summary>
    [DataField]
    public string EntangledName1 = "paper-quantum-entangled-1-name";

    /// <summary>
    /// New name for the QuantumPaperProto entity, instantiated as the result of the quantum split.
    /// </summary>
    [DataField]
    public string EntangledName2 = "paper-quantum-entangled-2-name";

    /// <summary>
    /// Description of entangled papers.
    /// </summary>
    [DataField]
    public string EntangledDesc = "paper-quantum-entangled-desc";

    /// <summary>
    /// Verb to disentangle the paper. Disentangling removes PaperQuantum and Superposed components.
    /// </summary>
    [DataField]
    public string DisentangleVerb = "paper-quantum-disentangle-verb";

    /// <summary>
    /// Popup to show to the player when he starts disentangling the paper.
    /// </summary>
    [DataField]
    public string PopupDisentangleSelf = "paper-quantum-popup-disentangle-self";

    /// <summary>
    /// Popup to show to other players when someone starts disentangling the paper.
    /// </summary>
    [DataField]
    public string PopupDisentangleOther = "paper-quantum-popup-disentangle-other";

    /// <summary>
    /// How long does it take to perform disentangling?
    /// </summary>
    [DataField]
    public TimeSpan DisentangleDuration = TimeSpan.FromSeconds(4f);

    /// <summary>
    /// Updated name of the disentangled paper.
    /// </summary>
    [DataField]
    public string DisentangledName = "paper-quantum-disentangled-name";

    /// <summary>
    /// Updated description of the disentangled paper.
    /// </summary>
    [DataField]
    public string DisentangledDesc = "paper-quantum-disentangled-desc";

    /// <summary>
    /// Effect prototype, used when one of the entangled papers is stamped or ignited.
    /// </summary>
    [DataField]
    public EntProtoId BluespaceEffectProto = "EffectFlashBluespaceMini";

    /// <summary>
    /// What weight can the quantum paper transport to the entangle copy when lit on fire.
    /// Total amount of slots.
    /// </summary>
    [DataField]
    public int TeleportWeight = 4;

    /// <summary>
    /// What penalty is applied to the TeleportWeight on each transfer of the entanglement from one entity to another.
    /// Examples of "transfer" include faxing and copying.
    /// New TeleportWeight is calculated as old TeleportWeight, multiplied by the penalty coeff.
    /// 0.5 stands for "halve teleport weight on each transfer" (4 -> 2 -> 1 -> 0).
    /// </summary>
    [DataField]
    public float WeightPenaltyCoeffOnTransfer = 0.5f;

    /// <summary>
    /// What damage is dealt to a non-entity that standed too near to the teleportation source (during the ignition)
    /// and got "sucked in".
    /// </summary>
    [DataField]
    public DamageSpecifier Damage = default!;

    /// <summary>
    /// Popup shown to the victim of bluespacing.
    /// </summary>
    [DataField]
    public string PopupTraumaSelf = "paper-quantum-popup-trauma-self";

    /// <summary>
    /// Popup shown to other people near the victim of bluespacing.
    /// </summary>
    [DataField]
    public string PopupTraumaOther = "paper-quantum-popup-trauma-other";

    /// <summary>
    /// Reference to an entangled entity.
    /// </summary>
    [DataField, AutoNetworkedField]
    public NetEntity? Entangled;
}
