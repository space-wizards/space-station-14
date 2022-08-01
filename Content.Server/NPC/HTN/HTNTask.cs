using Robust.Shared.Prototypes;

namespace Content.Server.NPC.HTN;

public abstract class HTNTask : IPrototype
{
    [IdDataField] public string ID { get; } = default!;

    /// <summary>
    /// A descriptor of the field, to be used for debugging.
    /// </summary>
    [DataField("desc")] public string? Description;
}
