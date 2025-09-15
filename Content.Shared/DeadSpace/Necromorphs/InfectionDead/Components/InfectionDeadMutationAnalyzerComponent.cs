// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Audio;

namespace Content.Shared.DeadSpace.Necromorphs.InfectionDead.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class InfectionDeadMutationAnalyzerComponent : Component
{
    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public bool IsRunning = false;

    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public InfectionDeadStrainData StrainData = new InfectionDeadStrainData();

    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public EntityUid? User = null;

    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public TimeSpan RunningTime = TimeSpan.Zero;

    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public TimeSpan DurationRunning = TimeSpan.FromSeconds(5f);

    [DataField]
    [ViewVariables(VVAccess.ReadOnly)]
    public EntProtoId Paper { get; set; } = "MutationAnalyzerReportPaper";

    [DataField]
    [ViewVariables(VVAccess.ReadOnly)]
    public SoundSpecifier? PrintingSound = new SoundPathSpecifier("/Audio/Machines/diagnoser_printing.ogg");

    #region Visualizer

    [DataField]
    public string State = "icon";

    [DataField]
    public string WorkingState = "working";

    #endregion
}

