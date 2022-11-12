using Robust.Shared.Prototypes;

namespace Content.Server.NPC.HTN;

public abstract class HTNTask : IPrototype
{
    [IdDataField] public string ID { get; } = default!;
}
