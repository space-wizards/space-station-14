using Content.Shared.SurveillanceCamera;

namespace Content.Server.SurveillanceCamera;

[RegisterComponent]
public sealed partial class SurveillanceRecordsServerComponent : Component
{
    [ViewVariables]
    public Queue<CameraSightingRecord> Records = new();

    [DataField]
    public int MaxRecords = 2000;

    [DataField]
    public TimeSpan Retention = TimeSpan.FromMinutes(15);
}
