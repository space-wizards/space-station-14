using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.List;

namespace Content.Shared.Damage.Prototypes
{
    /// <summary>
    ///     A Group of <see cref="DamageTypePrototype"/>s.
    /// </summary>
    /// <remarks>
    ///     These groups can be used to specify supported damage types of a <see cref="DamageContainerPrototype"/>, or
    ///     to change/get/set damage in a <see cref="DamageableComponent"/>.
    /// </remarks>
    [Prototype("damageGroup")]
    [Serializable, NetSerializable]
    public sealed class DamageGroupPrototype : IPrototype
    {
        [IdDataFieldAttribute] public string ID { get; } = default!;

        [DataField("damageTypes", required: true, customTypeSerializer: typeof(PrototypeIdListSerializer<DamageTypePrototype>))]
        public List<string> DamageTypes { get; } = default!;
    }
}
