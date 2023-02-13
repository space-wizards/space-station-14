using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.Utility;

namespace Content.Shared.Shipyard.Prototypes
{
    [NetSerializable, Serializable]

    [Prototype("vessel")]

    public sealed class VesselPrototype : IPrototype
    {
        [ViewVariables]
        [IdDataField]
        public string ID { get; } = default!;

        /// <summary>
        ///     Vessel name.
        /// </summary>
        [ViewVariables]
        [DataField("name")] public string Name = string.Empty;

        /// <summary>
        ///     Short description of the vessel.
        /// </summary>
        [ViewVariables]
        [DataField("description")] public string Description = string.Empty;

        /// <summary>
        ///     The price of the vessel
        /// </summary>
        [DataField("price", required: true)]
        public int Price { get; }

        /// <summary>
        ///     The prototype category of the product. (e.g. Small, Medium, Large, Emergency, Special etc.)
        /// </summary>
        [DataField("category")]
        public string Category { get; } = string.Empty;

        /// <summary>
        ///     The prototype group of the product. (e.g. Civilian, Syndicate, Contraband etc.)
        /// </summary>
        [DataField("group")]
        public string Group { get; } = string.Empty;

        /// <summary>
        ///     Relative directory path to the given shuttle, i.e. `/Maps/Shuttles/yourshittle.yml`
        /// </summary>
        [DataField("shuttlePath", required: true)]
        public ResourcePath ShuttlePath { get; } = default!;
    }
}
