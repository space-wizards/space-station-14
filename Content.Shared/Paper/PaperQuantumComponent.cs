using Content.Shared.Damage;
using Content.Shared.Explosion;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Paper;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, Access(typeof(SharedPaperQuantumSystem))]
public sealed partial class PaperQuantumComponent : Component
{
    [DataField]
    public EntProtoId QuantumPaperProto = "PaperQuantum";

    [DataField]
    public string SplitVerb = "paper-quantum-split-verb";

    [DataField]
    public TimeSpan SplitDuration = TimeSpan.FromSeconds(5f);

    [DataField]
    public string PopupSplitSelf = "paper-quantum-popup-split-self";

    [DataField]
    public string PopupSplitOther = "paper-quantum-popup-split-other";

    [DataField]
    public string EntangledName1 = "paper-quantum-entangled-name-1";

    [DataField]
    public string EntangledName2 = "paper-quantum-entangled-name-2";

    [DataField]
    public string EntangledDesc = "paper-quantum-entangled-desc";

    [DataField]
    public string DisentangleVerb = "paper-quantum-disentangle-verb";

    [DataField]
    public string PopupDisentangleSelf = "paper-quantum-popup-disentangle-self";

    [DataField]
    public string PopupDisentangleOther = "paper-quantum-popup-disentangle-other";

    [DataField]
    public TimeSpan DisentangleDuration = TimeSpan.FromSeconds(5f);

    [DataField]
    public string DisentangledName = "paper-quantum-disentangled-name";

    [DataField]
    public string DisentangledDesc = "paper-quantum-disentangled-desc";

    [DataField]
    public EntProtoId BluespaceStampEffectProto = "EffectFlashBluespaceMini";

    [DataField]
    public int TeleportWeight = 4;

    [DataField]
    public float FaxTeleportWeightPenaltyCoeff = 0.5f;

    [DataField]
    public DamageSpecifier Damage = default!;

    [DataField]
    public string PopupTraumaSelf = "paper-quantum-popup-trauma-self";

    [DataField]
    public string PopupTraumaOther = "paper-quantum-popup-trauma-other";

    [DataField, AutoNetworkedField]
    public NetEntity? Entangled;
}
