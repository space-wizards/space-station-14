using Robust.Shared.Prototypes;

namespace Content.Server.AI.HTN;

[ImplicitDataDefinitionForInheritors()]
public abstract class HTNTask : IPrototype
{
    [IdDataFieldAttribute] public string ID { get; } = default!;

    /// <summary>
    /// A descriptor of the field, to be used for debugging.
    /// </summary>
    [DataField("desc")] public string? Description;
}
