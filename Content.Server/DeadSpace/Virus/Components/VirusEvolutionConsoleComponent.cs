// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Shared.DeviceLinking;
using Robust.Shared.Prototypes;

namespace Content.Server.DeadSpace.Virus.Components;

[RegisterComponent]
public sealed partial class VirusEvolutionConsoleComponent : Component
{
    [DataField]
    [ViewVariables(VVAccess.ReadOnly)]
    public ProtoId<SourcePortPrototype> VirusSolutionAnalyzerPort = "VirusSolutionAnalyzerSender";

    [DataField]
    [ViewVariables(VVAccess.ReadOnly)]
    public ProtoId<SourcePortPrototype> VirusDiagnoserDataServerPort = "VirusDiagnoserDataServerSender";

    [ViewVariables(VVAccess.ReadOnly)]
    public EntityUid? VirusDiagnoserDataServer = null;

    [ViewVariables(VVAccess.ReadOnly)]
    public EntityUid? VirusSolutionAnalyzer = null;

    [DataField]
    public float MaxDistanceForDataServer = 50f;

    [DataField]
    public float MaxDistanceForOther = 4f;

    [ViewVariables(VVAccess.ReadOnly)]
    public bool DataServerInRange = true;

    [ViewVariables(VVAccess.ReadOnly)]
    public bool SolutionAnalyzerInRange = true;

    [DataField]
    public bool IsTaipan = false;
}

