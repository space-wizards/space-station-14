using System.ComponentModel.DataAnnotations;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Shared.Damage
{
    public class DamageTypePrototype : IPrototype
    {
        [field: DataField(tag: "id", required: true)]
        public string ID { get; } = default!;
    }
}
