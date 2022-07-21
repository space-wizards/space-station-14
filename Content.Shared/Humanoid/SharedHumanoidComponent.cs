using Content.Shared.CharacterAppearance;

namespace Content.Shared.Humanoid;

[RegisterComponent]
public sealed class SharedHumanoidComponent : Component
{
    [DataField("species")]
    public string Species { get; } = default!;

    /// <summary>
    ///     The initial sprites that this humanoid should
    ///     start with. Processed after the humanoid's
    ///     base sprites are processed, and includes things
    ///     like hair or markings.
    ///
    ///     This should be for humanoid variants that need
    ///     special sprites on spawn.
    /// </summary>
    [DataField("initialSprites")]
    public string InitialSprites { get; } = default!;

    /// <summary>
    ///     Skin color of this humanoid. This should probably
    ///     be enforced against the species if manually set...
    /// </summary>
    [DataField("skinColor")]
    public Color SkinColor { get; } = default!;

    // TODO: Accessories and Markings should probably be merged into SpriteAccessory 2.0?
    // The distinction is really vague, and markings allows for things like species restriction
    // and multi-layered sprites in its prototype. It was already discussed that sprite accessory
    // (which is literally just hairs) would be eclipsed by markings, so merging the two together
    // wouldn't be a bad idea.

    /// <summary>
    ///     Current sprite accessories on this humanoid, like hair.
    /// </summary>
    public Dictionary<HumanoidVisualLayers, List<string>> CurrentAccessories = new();

    /// <summary>
    ///     All current markings on this humanoid, by visual layer.
    ///     This still eventually will boil down into a MarkingsSet,
    ///     unless I change the markings UI to change the categories.
    ///
    ///     This should probably stay in its own component...
    ///     We'll see.
    /// </summary>
    [ViewVariables]
    public Dictionary<HumanoidVisualLayers, List<string>> CurrentMarkings = new();
}
