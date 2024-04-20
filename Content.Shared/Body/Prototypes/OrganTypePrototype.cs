using Robust.Shared.Prototypes;

namespace Content.Shared.Body.Prototypes;

[Prototype]
public sealed partial class OrganTypePrototype : IPrototype
{
    /// <inheritdoc/>
    [IdDataField]
    public string ID { get; } = default!;
}
