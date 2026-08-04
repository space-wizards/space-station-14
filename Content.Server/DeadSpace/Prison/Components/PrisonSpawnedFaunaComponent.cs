using Robust.Shared.Prototypes;

namespace Content.Server.DeadSpace.Prison.Components;

[RegisterComponent, Access(typeof(PrisonFaunaPopulationSystem), typeof(PrisonSystem))]
public sealed partial class PrisonSpawnedFaunaComponent : Component
{
    [DataField]
    public EntityUid Map;

    [DataField]
    public EntProtoId Prototype = default;

    [DataField]
    public Vector2i Sector;

    [DataField]
    public int SentenceReductionMinutes = 1;
}
