using Content.Shared.Alert;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Shared.StatusEffect
{
    [Prototype("statusEffect")]
    public class StatusEffectPrototype : IPrototype
    {
        [DataField("id", required: true)]
        public string ID { get; } = default!;

        [DataField("alert")]
        public AlertType? Alert { get; }
    }
}
