using Content.Shared.FixedPoint;

namespace Content.Client.Damage;

[RegisterComponent]
public sealed partial class DamageVisualsComponent : Component
{
    /// <summary>
    ///     Damage thresholds between damage state changes.
    ///
    ///     If there are any negative thresholds, or there is
    ///     less than one threshold, the visualizer is marked
    ///     as invalid.
    /// </summary>
    /// <remarks>
    ///     A 'zeroth' threshold is automatically added,
    ///     and this list is automatically sorted for
    ///     efficiency beforehand. As such, the zeroth
    ///     threshold is not required - and negative
    ///     thresholds are automatically caught as
    ///     invalid. The zeroth threshold automatically
    ///     sets all layers to invisible, so a sprite
    ///     isn't required for it.
    /// </remarks>
    [DataField("thresholds", required: true)]
    public List<FixedPoint2> Thresholds = new();

    /// <summary>
    ///     Layers to target, by layerMapKey.
    ///     If a target layer map key is invalid
    ///     (in essence, undefined), then the target
    ///     layer is removed from the list for efficiency.
    ///
    ///     If no layers are valid, then the visualizer
    ///     is marked as invalid.
    ///
    ///     If this is not defined, however, the visualizer
    ///     instead adds an overlay to the sprite.
    /// </summary>
    /// <remarks>
    ///     Layers can be disabled here by passing
    ///     the layer's name as a key to SetData,
    ///     and passing in a bool set to either 'false'
    ///     to disable it, or 'true' to enable it.
    ///     Setting the layer as disabled will make it
    ///     completely invisible.
    /// </remarks>
    [DataField("targetLayers")] public List<Enum>? TargetLayers;

    /// <summary>
    ///     The actual sprites for every damage group
    ///     that the entity should display visually.
    ///
    ///     This is keyed by a damage group identifier
    ///     (for example, Brute), and has a value
    ///     of a DamageVisualizerSprite (see below)
    /// </summary>
    [DataField("damageOverlayGroups")] public  Dictionary<string, DamageVisualizerSprite>? DamageOverlayGroups;

    /// <summary>
    ///     Sets if you want sprites to overlay the
    ///     entity when damaged, or if you would
    ///     rather have each target layer's state
    ///     replaced by a different state
    ///     within its RSI.
    ///
    ///     This cannot be set to false if:
    ///     - There are no target layers
    ///     - There is no damage group
    /// </summary>
    [DataField("overlay")] public  bool Overlay = true;

    /// <summary>
    ///     A single damage group to target.
    ///     This should only be defined if
    ///     overlay is set to false.
    ///     If this is defined with damageSprites,
    ///     this will be ignored.
    /// </summary>
    /// <remarks>
    ///     This is here because otherwise,
    ///     you would need several permutations
    ///     of group sprites depending on
    ///     what kind of damage combination
    ///     you would want, on which threshold.
    /// </remarks>
    [DataField("damageGroup")] public  string? DamageGroup;

    /// <summary>
    ///     Set this if you want incoming damage to be
    ///     divided.
    /// </summary>
    /// <remarks>
    ///     This is more useful if you have similar
    ///     damage sprites in between entities,
    ///     but with different damage thresholds
    ///     and you want to avoid duplicating
    ///     these sprites.
    /// </remarks>
    [DataField("damageDivisor")] public float Divisor = 1;

    /// <summary>
    ///     Set this to track all damage, instead of specific groups.
    /// </summary>
    /// <remarks>
    ///     This will only work if you have damageOverlay
    ///     defined - otherwise, it will not work.
    /// </remarks>
    [DataField("trackAllDamage")] public  bool TrackAllDamage;
    /// <summary>
    ///     This is the overlay sprite used, if _trackAllDamage is
    ///     enabled. Supports no complex per-group layering,
    ///     just an actually simple damage overlay. See
    ///     DamageVisualizerSprite for more information.
    /// </summary>
    [DataField("damageOverlay")] public  DamageVisualizerSprite? DamageOverlay;

    public readonly List<Enum> TargetLayerMapKeys = new();
    public bool Disabled = false;
    public bool Valid = true;
    public FixedPoint2 LastDamageThreshold = FixedPoint2.Zero;
    public readonly Dictionary<object, bool> DisabledLayers = new();
    public readonly Dictionary<object, string> LayerMapKeyStates = new();
    public readonly Dictionary<string, FixedPoint2> LastThresholdPerGroup = new();
    public string TopMostLayerKey = default!;
}

// deals with the edge case of human damage visuals not
// being in color without making a Dict<Dict<Dict<Dict<Dict<Dict...
[DataDefinition]
public sealed partial class DamageVisualizerSprite
{
    /// <summary>
    ///     The RSI path for the damage visualizer
    ///     group overlay.
    /// </summary>
    /// <remarks>
    ///     States in here will require one of four
    ///     forms:
    ///
    ///     If tracking damage groups:
    ///     - {base_state}_{group}_{threshold} if targeting
    ///       a static layer on a sprite (either as an
    ///       overlay or as a state change)
    ///     - DamageOverlay_{group}_{threshold} if not
    ///       targeting a layer on a sprite.
    ///
    ///     If not tracking damage groups:
    ///     - {base_state}_{threshold} if it is targeting
    ///       a layer
    ///     - DamageOverlay_{threshold} if not targeting
    ///       a layer.
    /// </remarks>
    [DataField("sprite", required: true)] public  string Sprite = default!;

    /// <summary>
    ///     The color of this sprite overlay.
    ///     Supports only hexadecimal format.
    /// </summary>
    [DataField("color")] public  string? Color;
}
