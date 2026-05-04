using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._Offbrand.Analyzers;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true), AutoGenerateComponentPause]
[Access(typeof(AnalyzerSystem))]
public sealed partial class AnalyzerComponent : Component
{
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoPausedField, AutoNetworkedField]
    public TimeSpan NextUpdate = TimeSpan.Zero;

    [DataField]
    public TimeSpan UpdateInterval = TimeSpan.FromSeconds(1);

    [DataField, AutoNetworkedField]
    public bool IsUpdating;

    [DataField, AutoNetworkedField]
    public bool ShouldUpdate;

    [DataField, AutoNetworkedField]
    public EntityUid? Target;

    [DataField, AutoNetworkedField]
    public float? ScanRange = 3f;
}

[ByRefEvent]
public readonly record struct AnalyzerUpdatedEvent(EntityUid Target);

[ByRefEvent]
public readonly record struct AfterAnalyzerUpdatedEvent(EntityUid Target);
