using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server.DeadSpace.Prison.Components;

[RegisterComponent, AutoGenerateComponentPause]
public sealed partial class PrisonFaunaPopulationComponent : Component
{
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoPausedField]
    public TimeSpan NextSpawnTime = TimeSpan.Zero;

    [DataField]
    public int InitialSpawnRemaining;

    public readonly Dictionary<Vector2i, TimeSpan> SectorCooldowns = new();
}
