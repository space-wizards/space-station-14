using Content.Shared.Access.Systems;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Markings;
using Content.Shared.Humanoid.Prototypes;
using Robust.Shared.Enums;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Set;

namespace Content.Server.CharacterAppearance.Components;

[RegisterComponent]
public sealed partial class RandomHumanoidAppearanceComponent : Component
{
    [DataField("randomizeName")] public bool RandomizeName = true;

    // Overrides //

    /// <summary>
    /// After randomizing, sets the character's sex to this, if applicable.
    /// </summary>
    [DataField] public Sex? Sex = null;
    /// <summary>
    /// After randomizing, sets the character's age to this, if applicable.
    /// </summary>
    [DataField] public int? Age = null;
    /// <summary>
    /// After randomizing, sets the character's gender to this, if applicable.
    /// </summary>
    [DataField] public Gender? Gender = null;

    /// <summary>
    /// After randomizing, sets the character's hair to this, if applicable.
    /// </summary>
    [DataField] public string? Hair = null;
    /// <summary>
    /// After randomizing, sets the character's hair color to this, if applicable.
    /// </summary>
    [DataField] public Color? HairColor = null;
    /// <summary>
    /// After randomizing, sets the character's facial hair to this, if applicable.
    /// </summary>
    [DataField] public string? FacialHair = null;
    /// <summary>
    /// After randomizing, sets the character's eye color to this, if applicable.
    /// </summary>
    [DataField] public Color? EyeColor = null;
    /// <summary>
    /// After randomizing, sets the character's skin color to this, if applicable.
    /// </summary>
    [DataField] public Color? SkinColor = null;
    /// <summary>
    /// After randomizing, adds the markings from this dict, if applicable.
    /// Will overwrite all randomized markings, if there are any. 
    /// Defined in YML as, for example:
    /// markings:
    ///   ArachnidTorsoFiddleback: [ "#daf7da" ]
    /// If the square brackets are empty (i.e. if the List<Color> has no members,) the color of that marking will be randomized.
    /// </summary>
    [DataField] public Dictionary<string, List<Color>>? Markings = null;
}
