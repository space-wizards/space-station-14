using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server.DeadSpace.Heartbeat;

[RegisterComponent, AutoGenerateComponentPause]
public sealed partial class CriticalSufferingComponent : Component
{
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan NextSymptom;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan NextJitter;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan NextVomit;

    public CriticalSymptom LastSymptom = CriticalSymptom.None;
    public bool VomitPending;
}

public enum CriticalSymptom : byte
{
    None,
    Gasp,
    Groan,
    Cough,
    Retch,
}
