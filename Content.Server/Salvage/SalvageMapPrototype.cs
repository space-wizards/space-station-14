using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Server.Salvage
{
    [Prototype("salvageMap")]
    public sealed class SalvageMapPrototype : IPrototype
    {
        [ViewVariables]
        [IdDataField]
        public string ID { get; } = default!;

        /// <summary>
        /// Relative directory path to the given map, i.e. `Maps/Salvage/test.yml`
        /// </summary>
        [DataField("mapPath", required: true)]
        public ResourcePath MapPath { get; } = default!;

        /// <summary>
        /// Map rectangle in world coordinates (to check if it fits)
        /// </summary>
        [DataField("bounds", required: true)]
        public Box2 Bounds { get; } = Box2.UnitCentered;

        /// <summary>
        /// Name for admin use
        /// </summary>
        [DataField("name")]
        public string Name { get; } = "";
    }
}
