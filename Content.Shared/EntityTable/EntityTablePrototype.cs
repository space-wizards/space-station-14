using Content.Shared.EntityTable.EntitySelectors;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityTable;

/// <summary>
/// This is a prototype for...
/// </summary>
[Prototype]
public sealed partial class EntityTablePrototype : IPrototype
{
    /// <inheritdoc/>
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    /// The Entity Table associated with this prototype.
    /// </summary>
    [DataField(required: true)]
    public EntityTableSelector Table = default!;
}
