using Content.Shared.Construction.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Construction.Components
{
    [RegisterComponent, NetworkedComponent]
    public sealed class MachinePartComponent : Component
    {
        [DataField("part", required: true, customTypeSerializer: typeof(PrototypeIdSerializer<MachinePartPrototype>))]
        public string PartType { get; private set; } = default!;

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("rating")]
        public int Rating { get; private set; } = 1;

        /// <summary>
        ///     This number is used in tests to ensure that you can't use high quality machines for arbitrage. In
        ///     principle there is nothing wrong with using higher quality parts, but you have to be careful to not
        ///     allow them to be put into a lathe or something like that.
        /// </summary>
        public const int MaxRating = 4;
    }
}
