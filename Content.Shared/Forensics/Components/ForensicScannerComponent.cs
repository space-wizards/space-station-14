using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Forensics.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true), AutoGenerateComponentPause]
public sealed partial class ForensicScannerComponent : Component
{
    /// <summary>
    /// A list of fingerprint GUIDs that the forensic scanner found from the <see cref="ForensicsComponent"/> on an entity.
    /// </summary>
    [DataField, AutoNetworkedField]
    public List<string> Fingerprints = [];

    /// <summary>
    /// A list of glove fibers that the forensic scanner found from the <see cref="ForensicsComponent"/> on an entity.
    /// </summary>
    [DataField, AutoNetworkedField]
    public List<string> Fibers = [];

    /// <summary>
    /// DNA that the forensic scanner found from the <see cref="DnaComponent"/> on an entity.
    /// </summary>
    [DataField, AutoNetworkedField]
    public List<string> DNAs = [];

    /// <summary>
    /// DNA that the forensic scanner found from the solution containers in an entity.
    /// </summary>
    [DataField, AutoNetworkedField]
    public List<string> SolutionDNAs = new();

    /// <summary>
    /// Residue that the forensic scanner found from the <see cref="ForensicsComponent"/> on an entity.
    /// </summary>
    [DataField, AutoNetworkedField]
    public List<string> Residues = [];

    /// <summary>
    /// What is the name of the entity that was scanned last?
    /// </summary>
    /// <remarks>
    /// This will be used for the title of the printout and displayed to players.
    /// </remarks>
    [DataField, AutoNetworkedField]
    public string LastScannedName = string.Empty;

    /// <summary>
    /// When will the scanner be ready to print again?
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField, AutoNetworkedField]
    public TimeSpan PrintReadyAt = TimeSpan.Zero;

    /// <summary>
    /// The time (in seconds) that it takes to scan an entity.
    /// </summary>
    [DataField]
    public float ScanDelay = 3.0f;

    /// <summary>
    /// How often can the scanner print out reports?
    /// </summary>
    [DataField]
    public TimeSpan PrintCooldown = TimeSpan.FromSeconds(5);

    /// <summary>
    /// The sound that's played when there's a match between a scan and an
    /// inserted forensic pad.
    /// </summary>
    [DataField]
    public SoundSpecifier SoundMatch = new SoundPathSpecifier("/Audio/Machines/Nuke/angry_beep.ogg");

    /// <summary>
    /// The sound that's played when there's no match between a scan and an
    /// inserted forensic pad.
    /// </summary>
    [DataField]
    public SoundSpecifier SoundNoMatch = new SoundPathSpecifier("/Audio/Machines/airlock_deny.ogg");

    /// <summary>
    /// The sound that's played when the scanner prints off a report.
    /// </summary>
    [DataField]
    public SoundSpecifier SoundPrint = new SoundPathSpecifier("/Audio/Machines/short_print_and_rip.ogg");

    /// <summary>
    /// What the machine will print
    /// </summary>
    [DataField(customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string MachineOutput = "ForensicReportPaper";
}
