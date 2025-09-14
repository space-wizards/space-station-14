using Content.Shared.Roles;
using Robust.Shared.Prototypes;

namespace Content.Shared._Starlight.Railroading;

[RegisterComponent]
public sealed partial class RailroadRuleComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite)]
    [NonSerialized]
    public RailroadStage Stage = RailroadStage.PreSpawnDelay;

    [ViewVariables]
    [NonSerialized]
    public float Timer;

    [ViewVariables]
    [NonSerialized]
    public ushort SpawnIndex = 0;

    [NonSerialized]
    public Queue<Entity<RailroadableComponent>> IssuanceQueue = [];

    [NonSerialized]
    public Queue<EntProtoId<RailroadCardComponent>> DynamicCards = [];

    [NonSerialized]
    public List<Entity<RailroadCardComponent, RuleOwnerComponent>> Pool = [];

    // Since special job cards would just get lost in the huge pool of general cards,
    // a priority queue has been made, only 1 card will be taken from it, placed in the center.
    [NonSerialized]
    public Dictionary<ProtoId<JobPrototype>, List<Entity<RailroadCardComponent, RuleOwnerComponent>>> PoolByJob = [];

    [DataField]
    public List<EntProtoId<RailroadCardComponent>> Cards = [];

    [DataField]
    public float PreSpawnDelay = 300;

    [DataField]
    public float Delay = 300;

    public enum RailroadStage
    {
        PreSpawnDelay,
        StaticSpawn,
        DynamicSpawn,
        CardShuffle,
        PreCardIssuance,
        CardIssuance,
        CycleDelay,
    }
}