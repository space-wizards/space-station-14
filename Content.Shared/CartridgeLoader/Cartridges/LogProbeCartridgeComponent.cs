using Content.Shared.Paper;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.CartridgeLoader.Cartridges;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
[Access(typeof(LogProbeCartridgeSystem))]
public sealed partial class LogProbeCartridgeComponent : Component
{
    /// <summary>
    /// The name of the scanned entity, sent to clients when they open the UI.
    /// </summary>
    [DataField, AutoNetworkedField]
    public string EntityName = string.Empty;

    /// <summary>
    /// The list of pulled access logs
    /// </summary>
    [DataField, AutoNetworkedField]
    public List<PulledAccessLog> PulledAccessLogs = new();

    /// <summary>
    /// The sound to make when we scan something with access
    /// </summary>
    [DataField]
    public SoundSpecifier SoundScan = new SoundPathSpecifier("/Audio/Machines/scan_finish.ogg", AudioParams.Default.WithVariation(0.25f));

    /// <summary>
    /// Paper to spawn when printing logs.
    /// </summary>
    [DataField]
    public EntProtoId<PaperComponent> PaperPrototype = "PaperAccessLogs";

    [DataField]
    public SoundSpecifier PrintSound = new SoundPathSpecifier("/Audio/Machines/diagnoser_printing.ogg");

    /// <summary>
    /// How long you have to wait before printing logs again.
    /// </summary>
    [DataField]
    public TimeSpan PrintCooldown = TimeSpan.FromSeconds(5);

    /// <summary>
    /// When anyone is allowed to spawn another printout.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan NextPrintAllowed = TimeSpan.Zero;
}
