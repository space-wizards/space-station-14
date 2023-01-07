using Robust.Shared.Utility;

namespace Content.Shared.Stacks
{
    ///
    /// <summary>
    /// Visualizer for items that come in stacks and have different appearance
    /// depending on the size of the stack. The visualizer can work by switching between different icons
    /// in <c>_spriteLayers</c> or if the sprite layers are supposed to be composed as transparent layers.
    /// The former behavior is default and the latter behavior can be defined in prototypes.
    ///
    /// Note, the entity doesn't necessarily need to have a StackComponent for this to work.
    /// Some prototypes (e.g. CigPackBase) use this to visualize how full a Storage is.
    ///
    /// <example>
    /// <para>To define a Stack Visualizer prototype insert the following
    /// snippet (you can skip Appearance if already defined)
    /// </para>
    /// <code>
    /// - type: Appearance
    ///   visuals:
    ///     - type: StackVisualizer
    ///       stackLayers:
    ///         - goldbar_10
    ///         - goldbar_20
    ///         - goldbar_30
    /// </code>
    /// </example>
    /// <example>
    /// <para>Defining a stack visualizer with composable transparent layers</para>
    /// <code>
    ///   - type: StackVisualizer
    ///     composite: true
    ///     stackLayers:
    ///       - cigarette_1
    ///       - cigarette_2
    ///       - cigarette_3
    ///       - cigarette_4
    ///       - cigarette_5
    ///       - cigarette_6
    /// </code>
    /// </example>
    ///  <seealso cref="_spriteLayers"/>
    /// </summary>
    [RegisterComponent]
    public sealed class StackVisualizerComponent : Component
    {
        /// <summary>
        /// Sprite layers used in stack visualizer. Sprites first in layer correspond to lower stack states
        /// e.g. <code>_spriteLayers[0]</code> is lower stack level than <code>_spriteLayers[1]</code>.
        /// </summary>
        [DataField("stackLayers")] public readonly List<string> SpriteLayers = new();

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
        [DataField("composite")] public bool IsComposite;

        /// <summary>
        /// Optional RSI path to use for the <code>stackLayers<code>. If not specified, will look in the entity's sprite.
        /// </summary>
        [DataField("sprite")] public ResourcePath? SpritePath;
    }
}
