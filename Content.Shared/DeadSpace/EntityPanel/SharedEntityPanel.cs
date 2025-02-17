// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Robust.Shared.Serialization;

namespace Content.Shared.DeadSpace.EntityPanel;

[Serializable, NetSerializable]
public sealed partial class RequestEntityMenuEvent : EntityEventArgs
{
    public bool IsUseEvolutionSystem { get; set; } = false;
    public bool IsUseSpawnPointSystem { get; set; } = false;
    public readonly List<string> Prototypes = new();
    public int Target { get; }
    public RequestEntityMenuEvent(int target, bool isUseEvolutionSystem, bool isUseSpawnPointSystem)
    {
        Target = target;
        IsUseEvolutionSystem = isUseEvolutionSystem;
        IsUseSpawnPointSystem = isUseSpawnPointSystem;
    }
}

[Serializable, NetSerializable]
public sealed partial class SelectEntityEvent : EntityEventArgs
{
    public bool IsUseEvolutionSystem { get; set; } = false;
    public bool IsUseSpawnPointSystem { get; set; } = false;
    public string PrototypeId { get; }
    public int Target { get; }
    public SelectEntityEvent(int target, string prototypeId, bool isUseEvolutionSystem, bool isUseSpawnPointSystem)
    {
        Target = target;
        PrototypeId = prototypeId;
        IsUseEvolutionSystem = isUseEvolutionSystem;
        IsUseSpawnPointSystem = isUseSpawnPointSystem;
    }
}
