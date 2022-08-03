using System.Linq;
using Content.Shared.CharacterAppearance;
using Content.Shared.Markings;
using Content.Shared.Preferences;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Humanoid;

[RegisterComponent]
public abstract class SharedHumanoidComponent : Component
{
    /// <summary>
    ///     Current species. Dictates things like base body sprites,
    ///     base humanoid to spawn, etc.
    /// </summary>
    [DataField("species")]
    public string Species { get; set; } = default!;

    /// <summary>
    ///     The initial sprites that this humanoid should
    ///     start with. Processed after the humanoid's
    ///     base sprites are processed, and includes things
    ///     like hair or markings.
    ///
    ///     This should be for humanoid variants that need
    ///     special sprites on spawn.
    /// </summary>
    [DataField("initial")]
    public string Initial { get; } = default!;

    /// <summary>
    ///     Skin color of this humanoid. This should probably
    ///     be enforced against the species if manually set...
    ///
    ///     - This is updated on the client by OnChangeData
    /// </summary>
    [DataField("skinColor")]
    public Color SkinColor { get; set;  } = Color.FromHex("#C0967F");

    // Eye color is updated by OnChangeData by just setting the
    // color of the eye layer. It isn't stored in here, because
    // anything relevant should just target that layer server-side.

    // TODO: Accessories and Markings should probably be merged into SpriteAccessory 2.0?
    // The distinction is really vague, and markings allows for things like species restriction
    // and multi-layered sprites in its prototype. It was already discussed that sprite accessory
    // (which is literally just hairs) would be eclipsed by markings, so merging the two together
    // wouldn't be a bad idea.

    // ^^^ Attempting this, let's see how well this goes

    /* TODO: Goes in server
    /// <summary>
    ///     All current markings on this humanoid, by visual layer.
    ///
    ///     - This is updated on the client by OnChangeData
    /// </summary>
    [ViewVariables]
    public MarkingSet CurrentMarkings = new();
    */

    /// <summary>
    ///     Visual layers currently hidden. This will affect the base sprite
    ///     on this humanoid layer, and any markings that sit above it.
    ///
    ///     - This is updated on the client by OnChangeData
    /// </summary>
    [ViewVariables]
    public readonly HashSet<HumanoidVisualLayers> HiddenLayers = new();

    // Appearance loaded from a player profile: this should eventually be removed
    // and replaced with accessory layer set calls, accessory color set calls,
    // layer color set calls, etc.
    //
    // The actual back-end part isn't being removed, though. That's fine.
    public HumanoidCharacterAppearance Appearance = HumanoidCharacterAppearance.Default();

    // these three could probably have their own components?
    // i don't see these as being unique to human characters
    // see: most animals having somebody classify this stuff

    // also definable in component so that you can have mob variants
    // of humanoids
    [DataField("sex")]
    public Sex Sex = Sex.Male;
    [DataField("gender")]
    public Gender Gender = Gender.Epicene;
    [DataField("age")]
    public int Age = HumanoidCharacterProfile.MinimumAge;

}

public sealed class HumanoidComponentState : ComponentState
{
    public HumanoidComponentState(string species, Color skinColor, MarkingsSet markings, Sex sex, Gender gender, int age, HashSet<HumanoidVisualLayers> hiddenLayers)
    {
        Species = species;
        SkinColor = skinColor;
        Markings = markings;
        Sex = sex;
        Gender = gender;
        Age = age;
        HiddenLayers = hiddenLayers.ToList();
    }

    public string Species { get; }
    public Color SkinColor { get; }
    public MarkingsSet Markings { get; }
    public Sex Sex { get; }
    public Gender Gender { get; }
    public int Age { get; }
    public List<HumanoidVisualLayers> HiddenLayers { get; }
}

[Prototype("humanoidMarkingStartingSet")]
public sealed class HumanoidMarkingStartingSet : IPrototype
{
    [IdDataField]
    public string ID { get; } = default!;

    [DataField("markings")]
    public List<Marking> Markings = new();
}
