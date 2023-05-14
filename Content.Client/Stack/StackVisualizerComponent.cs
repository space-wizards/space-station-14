using Robust.Shared.Utility;

namespace Content.Client.Stack;

/// <summary>
/// Visualizer for items that come in stacks and have different appearance
/// depending on the size of the stack. Visualizer can work by switching between different
/// icons in <c>_spriteLayers</c> or if the sprite layers are supposed to be composed as transparent layers.
/// The former behavior is default and the latter behavior can be defined in prototypes.
///
/// <example>
/// <para>To define a Stack Visualizer prototype insert the following
/// snippet (you can skip Appearance if already defined)
/// </para>
/// <code>
/// - components:
///   - type: StackVisualizer
///     stackLayers:
///       - goldbar_10
///       - goldbar_20
///       - goldbar_30
/// </code>
/// </example>
/// <example>
/// <para>Defining a stack visualizer with composable transparent layers</para>
/// <code>
/// - components:
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
[Access(typeof(StackVisualizerSystem))]
public sealed class StackVisualizerComponent : Component
{
    /// <summary>
    /// Default IconLayer stack.
    /// </summary>
    public const int IconLayer = 0;

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

    [DataField("sprite")] public ResPath? SpritePath;
}
