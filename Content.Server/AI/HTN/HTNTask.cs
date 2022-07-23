using Robust.Shared.Prototypes;

namespace Content.Server.AI.HTN;

[ImplicitDataDefinitionForInheritors()]
public abstract class HTNTask : IPrototype
{
    [IdDataFieldAttribute] public string ID { get; } = default!;
}
