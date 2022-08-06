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



    [Serializable, NetSerializable]
    public sealed class CustomBaseLayerInfo
    {
        public CustomBaseLayerInfo(string id, Color color)
        {
            ID = id;
            Color = color;
        }

        public string ID { get; }
        public Color Color { get; }
    }

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

    /// <summary>
    ///     Visual layers currently hidden. This will affect the base sprite
    ///     on this humanoid layer, and any markings that sit above it.
    ///
    ///     - This is updated on the client by OnChangeData
    /// </summary>
    [ViewVariables]
    public readonly HashSet<HumanoidVisualLayers> HiddenLayers = new();

    [DataField("sex")]
    public Sex Sex = Sex.Male;
}

[Prototype("humanoidMarkingStartingSet")]
public sealed class HumanoidMarkingStartingSet : IPrototype
{
    [IdDataField]
    public string ID { get; } = default!;

    [DataField("customBaseLayers")]
    public Dictionary<HumanoidVisualLayers, string> CustomBaseLayers = new();

    [DataField("markings")]
    public List<Marking> Markings = new();
}
