using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Access
{
    /// <summary>
    ///     Defines a single access level that can be stored on ID cards and checked for.
    /// </summary>
    [Prototype("accessLevel")]
    public readonly record struct AccessLevelPrototype : IPrototype
    {
        [ViewVariables]
        [IdDataFieldAttribute]
        public string ID { get; } = default!;

        [DataField("name", customTypeSerializer: typeof(LocStringSerializer))]
        private readonly string? _name = null;

        /// <summary>
        ///     The player-visible name of the access level, in the ID card console and such.
        /// </summary>
        public string Name => _name ?? ID;
    }
}
