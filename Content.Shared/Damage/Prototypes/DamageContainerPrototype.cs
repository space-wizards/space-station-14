using Content.Shared.Damage.Components;
using Robust.Shared.Prototypes;

namespace Content.Shared.Damage.Prototypes
{
    /// <summary>
    ///     A damage container which can be used to specify support for various damage types.
    /// </summary>
    /// <remarks>
    ///     This is effectively just a list of damage types that can be specified in YAML files using both damage types
    ///     and damage groups. Currently this is only used to specify what damage types a <see
    ///     cref="DamageableComponent"/> should support.
    /// </remarks>
    [Prototype]
    public sealed partial class DamageContainerPrototype : IPrototype
    {
        [ViewVariables]
        [IdDataField]
        public string ID { get; private set; } = default!;

        /// <summary>
        ///     List of damage groups that are supported by this container.
        /// </summary>
        [DataField]
        public List<ProtoId<DamageGroupPrototype>> SupportedGroups = new();

        /// <summary>
        ///     Partial List of damage types supported by this container. Note that members of the damage groups listed
        ///     in <see cref="SupportedGroups"/> are also supported, but they are not included in this list.
        /// </summary>
        [DataField]
        public List<ProtoId<DamageTypePrototype>> SupportedTypes = new();
    }
}
