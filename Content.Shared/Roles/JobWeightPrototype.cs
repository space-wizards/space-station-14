using Robust.Shared.Prototypes;

namespace Content.Shared.Roles;

/// <summary>
/// Defines the order in which jobs are assigned to their round-start minimum slots and displayed.
/// Job weights do not affect assignments made after every station's minimum slots have been considered.
/// Map-specific profiles override the entries they contain; all other entries use <see cref="Default"/>.
/// </summary>
[Prototype]
public sealed partial class JobWeightPrototype : IPrototype
{
    /// <summary>
    /// The global fallback profile used by maps that do not define an override.
    /// </summary>
    public static readonly ProtoId<JobWeightPrototype> Default = "Default";

    [IdDataField]
    public string ID { get; private set; } = string.Empty;

    [DataField]
    public Dictionary<ProtoId<JobPrototype>, int> Weights { get; private set; } = new();
}
