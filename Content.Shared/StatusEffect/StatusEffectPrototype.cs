using Content.Shared.Alert;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Shared.StatusEffect
{
    [Prototype("statusEffect")]
    public sealed class StatusEffectPrototype : IPrototype
    {
        [DataField("id", required: true)]
        public string ID { get; } = default!;

        [DataField("alert")]
        public AlertType? Alert { get; }

        /// <summary>
        ///     Whether a status effect should be able to apply to any entity,
        ///     regardless of whether it is in ALlowedEffects or not.
        /// </summary>
        [DataField("alwaysAllowed")]
        public bool AlwaysAllowed { get; }
    }
}
