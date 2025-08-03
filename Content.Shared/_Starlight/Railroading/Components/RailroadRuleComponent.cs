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