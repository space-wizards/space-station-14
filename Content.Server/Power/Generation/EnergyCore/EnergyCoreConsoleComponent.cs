using Content.Shared.DeviceLinking;
using Robust.Shared.Prototypes;

namespace Content.Server.Power.Generation.EnergyCore;

[RegisterComponent, Access(typeof(EnergyCoreConsoleSystem))]
public sealed partial class EnergyCoreConsoleComponent : Component
{
    [DataField]
    public ProtoId<SourcePortPrototype> SourcePort = "EnergyCoreSender";

    [DataField]
    public ProtoId<SinkPortPrototype> SinkPort = "EnergyCoreReceiver";
}
