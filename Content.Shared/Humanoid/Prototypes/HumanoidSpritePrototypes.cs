using Content.Shared.Humanoid.Markings;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared.Humanoid.Prototypes;

/// <summary>
///     Base sprites for a species (e.g., what replaces the empty tagged layer,
///     or settings per layer)
/// </summary>
[Prototype("speciesBaseSprites")]
public sealed class HumanoidSpeciesBaseSpritesPrototype : IPrototype
{
     [IdDataField]
     public string ID { get; } = default!;

     /// <summary>
     ///     Sprites that this species will use on the given humanoid
     ///     visual layer. If a key entry is empty, it is assumed that the
     ///     visual layer will not be in use on this species, and will
     ///     be ignored.
     /// </summary>
     [DataField("sprites", required: true)]
     public Dictionary<HumanoidVisualLayers, string> Sprites = new();
}

[Prototype("humanoidBaseSprite")]
public sealed class HumanoidSpeciesSpriteLayer : IPrototype
{
    [IdDataField]
    public string ID { get; } = default!;
    /// <summary>
    ///     The base sprite for this sprite layer. This is what
    ///     will replace the empty layer tagged by the enum
    ///     tied to this layer.
    ///
    ///     If this is null, no sprite will be displayed, and the
    ///     layer will be invisible until otherwise set.
    /// </summary>
    [DataField("baseSprite")]
    public SpriteSpecifier? BaseSprite { get; }

    /// <summary>
    ///     If this layer is only replaceable or not. If it is not
    ///     replaceable, sprite accessories will instead
    ///     replace this. Otherwise, they can be added on
    ///     top of this layer, and this layer can be
    ///     replaced
    ///
    ///     This should auto-set the attached HumanoidVisualLayer
    ///     marking point limit to 1
    /// </summary>
    [DataField("replaceOnly")]
    public bool ReplaceOnly { get; }

    /// <summary>
    ///     The alpha of this layer. Ensures that
    ///     this layer will start with this percentage
    ///     of alpha.
    ///
    ///     Future sprite accessories can potentially
    ///     replace layers and will probably do a
    ///     change to a layer's alpha, so this only
    ///     ensures that when the entity is created,
    ///     the entity will start with this layer alpha
    ///     set.
    /// </summary>
    [DataField("layerAlpha")]
    public float LayerAlpha { get; } = 1.0f;

    /// <summary>
    ///     If this sprite layer should allow markings or not.
    /// </summary>
    [DataField("allowsMarkings")]
    public bool AllowsMarkings { get; } = true;

    /// <summary>
    ///     If this layer should always match the
    ///     skin tone in a character profile.
    /// </summary>
    [DataField("matchSkin")]
    public bool MatchSkin { get; } = true;

    /// <summary>
    ///     If any markings that go on this layer should
    ///     match the skin tone of this part, including
    ///     alpha.
    /// </summary>
    [DataField("markingsMatchSkin")]
    public bool MarkingsMatchSkin { get; }
}

[Prototype("humanoidMarkingStartingSet")]
public sealed class HumanoidMarkingStartingSet : IPrototype
{
    [IdDataField]
    public string ID { get; } = default!;

    [DataField("customBaseLayers")]
    public Dictionary<HumanoidVisualLayers, CustomBaseLayerInfo> CustomBaseLayers = new();

    [DataField("markings")]
    public List<Marking> Markings = new();
}
