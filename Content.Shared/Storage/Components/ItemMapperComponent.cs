using Content.Shared.Storage.EntitySystems;
using Robust.Shared.Utility;

namespace Content.Shared.Storage.Components
{
    /// <summary>
    /// <para><c>ItemMapperComponent</c> is a <see cref="Component"/> that maps string labels to an <see cref="Content.Shared.Whitelist.EntityWhitelist"/> of elements. Useful primarily  for visualization.</para>
    /// <para>
    /// To define a mapping, create a <c>mapLayers</c> map in configuration <c>ItemMapper</c> component and with <see cref="MapLayers"/> mapping.
    /// Each map layer maps layer name to an <see cref="Content.Shared.Whitelist.EntityWhitelist"/>, plus special modifiers for min and max item count.
    /// Min and max count are useful when you need to visualize a certain number of items, for example, to display one, two, three, or more items.
    /// </para>
    /// <para>
    /// If you need a more straightforward way to change appearance where only variable is how many items, rather than which items
    /// and how many, see <see cref="ItemCounterComponent"/>
    /// </para>
    /// <para>
    /// For a contrived example, create a tool-belt with a Power drill slot and two light bulb slots.
    /// To use this for visualization, we need <see cref="AppearanceComponent"/> a or VisualizerSystem, e.g.
    /// <code>
    ///   - type: Appearance
    ///      visuals:
    ///      - type: MappedItemVisualizer
    /// </code>
    /// To map <c>Powerdrill</c> <b><see cref="Content.Shared.Tag.TagComponent"/></b> to the given example, we need the following code.
    /// <code>
    /// - type: ItemMapper
    ///   mapLayers:
    ///     drill:
    ///       whitelist:
    ///         tags:
    ///         - Powerdrill
    /// #... to be continued
    /// </code>
    /// To map <c>Lightbulb</c> <b><see cref="Component"/></b> (not tag) to two different layers (for one and two light bulbs, respectively)
    /// <code>
    /// #... to be continued
    ///     lightbulb1:
    ///       minCount: 1
    ///       whitelist:
    ///         component:
    ///         - Lightbulb
    ///     lightbulb2:
    ///       minCount: 2
    ///       whitelist:
    ///         component:
    ///         - Lightbulb
    /// </code>
    /// The min count will ensure that <c>lightbulb1</c> layer is only displayed when one or more light bulbs are in the belt.
    /// And <c>lightbulb2</c> layer will only be shown when two or more light bulbs are inserted.
    /// </para>
    /// <seealso cref="Content.Shared.Whitelist.EntityWhitelist"/>
    /// <seealso cref="Content.Shared.Storage.Components.SharedMapLayerData"/>
    /// </summary>
    [RegisterComponent]
    [Access(typeof(SharedItemMapperSystem))]
    public sealed partial class ItemMapperComponent : Component
    {
        [DataField("mapLayers")] public  Dictionary<string, SharedMapLayerData> MapLayers = new();

        [DataField("sprite")] public ResPath? RSIPath;

        /// <summary>
        ///     If this exists, shown layers will only consider entities in the given containers.
        /// </summary>
        [DataField("containerWhitelist")]
        public HashSet<string>? ContainerWhitelist;

        /// <summary>
        ///     The list of map layer keys that are valid targets for changing in <see cref="MapLayers"/>
        ///     Can be initialized if already existing on the sprite, or inferred automatically
        /// </summary>
        [DataField("spriteLayers")]
        public List<string> SpriteLayers = new();
    }
}
