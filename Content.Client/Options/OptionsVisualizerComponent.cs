using Content.Shared.CCVar;

namespace Content.Client.Options;

/// <summary>
/// Allows specifying sprite alternatives depending on the client's accessibility options.
/// </summary>
/// <remarks>
/// A list of layer mappings is given that the component applies to,
/// and it will pick one entry to apply based on the settings configuration. Example:
///
/// <code>
/// - type: Sprite
///   sprite: Effects/optionsvisualizertest.rsi
///   layers:
///   - state: none
///     map: [ "layer" ]
/// - type: OptionsVisualizer
///   visuals:
///     layer:
///     - options: Default
///       data: { state: none }
///     - options: Test
///       data: { state: test }
///     - options: ReducedMotion
///       data: { state: motion }
///     - options: [Test, ReducedMotion]
///       data: { state: both }
/// </code>
/// </remarks>
/// <seealso cref="OptionsVisualizerSystem"/>
/// <seealso cref="OptionVisualizerOptions"/>
[RegisterComponent]
public sealed partial class OptionsVisualizerComponent : Component
{
    /// <summary>
    /// A mapping storing data about which sprite layer keys should be controlled.
    /// </summary>
    /// <remarks>
    /// Each layer stores an array of possible options. The last entry with a
    /// <see cref="LayerDatum.Options"/> matching the active user preferences will be picked.
    /// This allows choosing a priority if multiple entries are matched.
    /// </remarks>
    [DataField(required: true)]
    public Dictionary<string, LayerDatum[]> Visuals = default!;

    /// <summary>
    /// A single option for a layer to be selected.
    /// </summary>
    [DataDefinition]
    public sealed partial class LayerDatum
    {
        /// <summary>
        /// Which options must be set by the user to make this datum match.
        /// </summary>
        [DataField]
        public OptionVisualizerOptions Options { get; set; }

        /// <summary>
        /// The sprite layer data to set on the sprite when this datum matches.
        /// </summary>
        [DataField]
        public PrototypeLayerData Data { get; set; }
    }
}

[Flags]
public enum OptionVisualizerOptions
{
    /// <summary>
    /// Corresponds to no special options being set, can be used as a "default" state.
    /// </summary>
    Default = 0,

    /// <summary>
    /// Corresponds to the <see cref="CCVars.DebugOptionVisualizerTest"/> CVar being set.
    /// </summary>
    Test = 1 << 0,

    /// <summary>
    /// Corresponds to the <see cref="CCVars.ReducedMotion"/> CVar being set.
    /// </summary>
    ReducedMotion = 1 << 1,
}
