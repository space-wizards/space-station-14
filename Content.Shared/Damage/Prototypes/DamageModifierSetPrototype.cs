using Robust.Shared.Prototypes;

namespace Content.Shared.Damage.Prototypes
{
    /// <summary>
    ///     A version of DamageModifierSet that can be serialized as a prototype, but is functionally identical.
    /// </summary>
    /// <remarks>
    ///     Done to avoid removing the 'required' tag on the ID and passing around a 'prototype' when we really
    ///     just want normal data to be deserialized.
    /// </remarks>
    [Prototype("damageModifierSet")]
    public sealed class DamageModifierSetPrototype : DamageModifierSet, IPrototype
    {
        [ViewVariables]
        [IdDataField]
        public string ID { get; } = default!;
    }
}
