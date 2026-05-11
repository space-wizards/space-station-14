using Content.Shared.Damage.Components;
using Robust.Shared.Prototypes;

namespace Content.Shared.Damage.Prototypes
{
    /// <summary>
    ///     A Group of <see cref="DamageTypePrototype"/>s.
    /// </summary>
    /// <remarks>
    ///     These groups can be used to specify supported damage types of a <see cref="DamageContainerPrototype"/>, or
    ///     to change/get/set damage in a <see cref="DamageableComponent"/>.
    /// </remarks>
    [Prototype(2)]
    [Obsolete("Do not rely on DamageGroupPrototype for anything besides grouping logically similar damage in UIs")]
    public sealed partial class DamageGroupPrototype : IPrototype
    {
        [IdDataField] public string ID { get; private set; } = default!;

        [DataField(required: true)]
        private LocId Name { get; set; }

        [ViewVariables(VVAccess.ReadOnly)]
        public string LocalizedName => Loc.GetString(Name);

        [DataField(required: true)]
        public List<ProtoId<DamageTypePrototype>> DamageTypes { get; private set; } = default!;
    }
}
