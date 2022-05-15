using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Species;

[Prototype("species")]
public sealed class SpeciesPrototype : IPrototype
{
    /// <summary>
    /// Prototype ID of the species.
    /// </summary>
    [IdDataFieldAttribute]
    public string ID { get; } = default!;

    /// <summary>
    /// User visible name of the species.
    /// </summary>
    [DataField("name", required: true)]
    public string Name { get; } = default!;

    /// <summary>
    /// Whether the species is available "at round start" (In the character editor)
    /// </summary>
    [DataField("roundStart", required: true)]
    public bool RoundStart { get; } = false;

    /// <summary>
    /// Prototype used by the species as a body.
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

    [DataField("maleLastNames")]
    public string MaleLastNames { get; } = "names_last_male";

    [DataField("femaleLastNames")]
    public string FemaleLastNames { get; } = "names_last_female";

    [DataField("naming")]
    public SpeciesNaming Naming { get; } = SpeciesNaming.FirstLast;
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
