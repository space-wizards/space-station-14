using Robust.Shared.Prototypes;

namespace Content.Shared.StatusEffects.Prototypes;
public sealed class StatusEffectWhitelistPrototype : IPrototype
{
    /// <inheritdoc/>
    [IdDataField]
    public string ID { get; } = default!;

    /// <summary>
    /// All the effects the entity is allowed to have.
    /// </summary>
    [DataField("effects")]
    public List<string> Effects { get; } = new();
}
