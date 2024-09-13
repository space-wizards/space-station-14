using Content.Shared.Storage.EntitySystems;
using Content.Shared.Whitelist;

namespace Content.Shared.Storage.Components
{

    /// <summary>
    /// Storage that spawns and counts a single item.
    /// Usually used for things like matchboxes, cigarette packs,
    /// cigar cases etc.
    /// </summary>
    /// <code>
    ///  - type: ItemCounter
    ///    amount: 6 # Note: this field can be omitted.
    ///    count:
    ///      tags: [Cigarette]
    /// </code>
    [RegisterComponent]
    [Access(typeof(SharedItemCounterSystem))]
    public sealed partial class ItemCounterComponent : Component
    {
        [DataField("count", required: true)]
        public EntityWhitelist Count { get; set; } = default!;

        [DataField("amount")]
        public int? MaxAmount { get; set; }

        /// <summary>
        /// Default IconLayer stack.
        /// </summary>
        [DataField("baseLayer")]
        [ViewVariables(VVAccess.ReadWrite)]
        public string BaseLayer = "";

        /// <summary>
        /// Determines if the visualizer uses composite or non-composite layers for icons. Defaults to false.
        ///
        /// <list type="bullet">
        /// <item>
        /// <description>false: they are opaque and mutually exclusive (e.g. sprites in a cable coil). <b>Default value</b></description>
        /// </item>
        /// <item>
        /// <description>true: they are transparent and thus layered one over another in ascending order first</description>
        /// </item>
        /// </list>
        ///
        /// </summary>
        [DataField("composite")]
        [ViewVariables(VVAccess.ReadWrite)]
        public bool IsComposite;

        /// <summary>
        /// Sprite layers used in counter visualizer. Sprites first in layer correspond to lower stack states
        /// e.g. <code>_spriteLayers[0]</code> is lower stack level than <code>_spriteLayers[1]</code>.
        /// </summary>
        [DataField("layerStates")]
        [ViewVariables(VVAccess.ReadWrite)]
        public List<string> LayerStates = new();
    }
}
