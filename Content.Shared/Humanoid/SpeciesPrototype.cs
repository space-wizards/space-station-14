using Content.Shared.CharacterAppearance;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.Utility;

namespace Content.Shared.Humanoid.Species;

[Prototype("species")]
public sealed class SpeciesPrototype : IPrototype
{
    /// <summary>
    /// Prototype ID of the species.
    /// </summary>
    [IdDataField]
    public string ID { get; } = default!;

    /// <summary>
    /// User visible name of the species.
    /// </summary>
    [DataField("name", required: true)]
    public string Name { get; } = default!;

    /// <summary>
    ///     Descriptor. Unused...? This is intended
    ///     for an eventual integration into IdentitySystem
    ///     (i.e., young human person, young lizard person, etc.)
    /// </summary>
    [DataField("descriptor")]
    public string Descriptor { get; } = "humanoid";

    /// <summary>
    /// Whether the species is available "at round start" (In the character editor)
    /// </summary>
    [DataField("roundStart", required: true)]
    public bool RoundStart { get; } = false;

    // The below two are to avoid fetching information about the species from the entity
    // prototype.

    // This one here is a utility field, and is meant to *avoid* having to duplicate
    // the massive SpriteComponent found in every species.
    // Species implementors can just override SpriteComponent if they want a custom
    // sprite layout, and leave this null. Keep in mind that this will disable
    // sprite accessories.

    [DataField("sprites")]
    public string SpriteSet { get; } = default!;

    /// <summary>
    ///     The limit of body markings that you can place on this species.
    /// </summary>
    [DataField("markingLimits")]
    public string MarkingPoints { get; } = default!;

    /// <summary>
    ///     Humanoid species variant used by this entity.
    /// </summary>
    [DataField("prototype", required: true, customTypeSerializer:typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string Prototype { get; } = default!;

    /// <summary>
    /// Prototype used by the species for the dress-up doll in various menus.
    /// </summary>
    [DataField("dollPrototype", required: true, customTypeSerializer:typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string DollPrototype { get; } = default!;

    /// <summary>
    /// Method of skin coloration used by the species.
    /// </summary>
    [DataField("skinColoration", required: true)]
    public SpeciesSkinColor SkinColoration { get; }

    [DataField("maleFirstNames")]
    public string MaleFirstNames { get; } = "names_first_male";

    [DataField("femaleFirstNames")]
    public string FemaleFirstNames { get; } = "names_first_female";

    [DataField("lastNames")]
    public string LastNames { get; } = "names_last";

    [DataField("naming")]
    public SpeciesNaming Naming { get; } = SpeciesNaming.FirstLast;
}

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

    /* Redundant, but I'll leave it here for now.
    /// <summary>
    ///     The color of this layer.
    /// </summary>
    [DataField("color")]
    public Color Color { get; } = Color.White;
    */

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
    public bool AllowsMarkings = true;

    /// <summary>
    ///     If this layer should always match the
    ///     skin tone in a character profile.
    /// </summary>
    [DataField("matchSkin")]
    public bool MatchSkin = true;

    /// <summary>
    ///     If any markings that go on this layer should
    ///     match the skin tone of this part, including
    ///     alpha.
    /// </summary>
    [DataField("markingsMatchSkin")]
    public bool MarkingsMatchSkin;
}

public enum SpeciesSkinColor : byte
{
    HumanToned,
    Hues,
    TintedHues, //This gives a color tint to a humanoid's skin (10% saturation with full hue range).
}

public enum SpeciesNaming : byte
{
    FirstLast,
    FirstDashFirst,
}
