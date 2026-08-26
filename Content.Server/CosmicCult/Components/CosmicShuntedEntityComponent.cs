using Content.Shared.CosmicCult.Components;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server.CosmicCult.Components;

[RegisterComponent, AutoGenerateComponentPause]
public sealed partial class CosmicShuntedEntityComponent : Component
{
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))] [AutoPausedField]
    public TimeSpan ExitVoidTime;

    [DataField] public bool ReadyToReturn;

    [DataField] public bool ConvertOnReturn;

    [DataField] public EntityUid OriginalBody;

    [DataField] public TimeSpan ShuntedDuration;

    public Entity<CosmicCultistComponent> ShuntCaster;

    public Entity<CosmicCultistComponent> WispGrabber;
}
