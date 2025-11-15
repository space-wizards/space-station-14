using Content.Shared.AbstractAnalyzer;
using Content.Shared.Paper;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Botany.Components;

/// <inheritdoc/>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentPause]
public sealed partial class PlantAnalyzerComponent : AbstractAnalyzerComponent
{
    /// <inheritdoc/>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoPausedField]
    public override TimeSpan NextUpdate { get; set; } = TimeSpan.Zero;

    /// <summary>
    /// When will the analyzer be ready to print again?
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoPausedField]
    public TimeSpan PrintReadyAt = TimeSpan.Zero;

    /// <summary>
    /// How often can the analyzer print?
    /// </summary>
    [DataField]
    public TimeSpan PrintCooldown = TimeSpan.FromSeconds(5);

    /// <summary>
    /// The sound that's played when the analyzer prints off a report.
    /// </summary>
    [DataField]
    public SoundSpecifier SoundPrint = new SoundPathSpecifier("/Audio/Machines/short_print_and_rip.ogg");

    /// <summary>
    /// What the machine will print.
    /// </summary>
    [DataField]
    public EntProtoId<PaperComponent> MachineOutput = "PlantAnalyzerReportPaper";
}
