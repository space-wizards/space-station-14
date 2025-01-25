using Content.Server.AbstractAnalyzer;
using Content.Server.Botany.Systems;
using Content.Server.Medical.Components;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Botany.Components;

/// <inheritdoc/>
[RegisterComponent, AutoGenerateComponentPause]
[Access(typeof(PlantAnalyzerSystem))]
public sealed partial class PlantAnalyzerComponent : AbstractAnalyzerComponent
{
    /// <inheritdoc/>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoPausedField]
    public override TimeSpan NextUpdate { get; set; } = TimeSpan.Zero;

    /// <summary>
    /// When will the analyzer be ready to print again?
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    public TimeSpan PrintReadyAt = TimeSpan.Zero;

    /// <summary>
    /// How often can the analyzer print?
    /// </summary>
    [DataField("printCooldown")]
    public TimeSpan PrintCooldown = TimeSpan.FromSeconds(5);

    /// <summary>
    /// The sound that's played when the analyzer prints off a report.
    /// </summary>
    [DataField("soundPrint")]
    public SoundSpecifier SoundPrint = new SoundPathSpecifier("/Audio/Machines/short_print_and_rip.ogg");

    /// <summary>
    /// What the machine will print.
    /// </summary>
    [DataField("machineOutput", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string MachineOutput = "PlantAnalyzerReportPaper";
}
