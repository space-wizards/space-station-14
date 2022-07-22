using Content.Shared.CharacterAppearance;
using Content.Shared.Markings;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Humanoid;

[RegisterComponent]
public sealed class SharedHumanoidComponent : Component
{
    /// <summary>
    ///     Current species. Dictates things like base body sprites,
    ///     base humanoid to spawn, etc.
    /// </summary>
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
    [DataField("initial")]
    public string Initial { get; } = default!;

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

    // ^^^ Attempting this, let's see how well this goes

    /*
    /// <summary>
    ///     Current sprite accessories on this humanoid, like hair.
    /// </summary>
    public Dictionary<HumanoidVisualLayers, List<string>> CurrentAccessories = new();
    */

    /// <summary>
    ///     All current markings on this humanoid, by visual layer.
    ///
    ///     - This is updated on the client by calls to OnChangeData in VisualizerSystem
    /// </summary>
    [ViewVariables]
    public MarkingsSet CurrentMarkings = new();
}

[Prototype("humanoidMarkingStartingSet")]
public sealed class HumanoidMarkingStartingSet : IPrototype
{
    [IdDataField]
    public string ID { get; } = default!;

    [DataField("markings")]
    public List<Marking> Markings = new();
}
