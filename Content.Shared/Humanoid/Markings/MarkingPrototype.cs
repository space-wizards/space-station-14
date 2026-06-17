using System.Linq;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared.Humanoid.Markings;

/// <summary>
///     Definition of a marking, a cosmetic sprite change on a humanoid character.
///     These are selectable in the character editor.
/// </summary>
[Prototype]
public sealed partial class MarkingPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = "uwu";

    public string Name { get; private set; } = default!;

    /// <summary>
    ///     The "body part" visual layer that this marking applies to.
    /// </summary>

    [DataField("bodyPart", required: true)]
    public HumanoidVisualLayers BodyPart { get; private set; } = default!;

    /// <summary>
    ///     A list of markings groups that are able to use this marking.
    /// </summary>
    [DataField]
    public List<ProtoId<MarkingsGroupPrototype>>? GroupWhitelist;

    /// <summary>
    ///     A restriction on which sexes may use this marking.
    /// </summary>
    [DataField("sexRestriction")]
    public Sex? SexRestriction { get; private set; }

    /// <summary>
    ///     Whether or not this marking's colors can be manually changed by the player.
    ///     If not, then it will be forced to use a certain color depending on <see cref="Coloring">.
    /// </summary>
    [DataField("forcedColoring")]
    public bool ForcedColoring { get; private set; } = false;

    /// <summary>
    ///     Parameters for the default colors that a marking's layers will use.
    /// </summary>
    [DataField("coloring")]
    public MarkingColors Coloring { get; private set; } = new();

    /// <summary>
    /// Do we need to apply any displacement maps to this marking? Set to false if your marking is incompatible
    /// with a standard human doll, and is used for some special races with unusual shapes
    /// </summary>
    [DataField]
    public bool CanBeDisplaced { get; private set; } = true;

    /// <summary>
    ///     A list of sprite layers associated with this marking.
    /// </summary>
    [DataField("sprites")]
    [Obsolete("Use Layers instead.")]
    public List<SpriteSpecifier> Sprites { get; private set; } = default!;

    /// <summary>
    ///     A list of layer metadata objects associated with this marking.
    /// </summary>
    [DataField("layers")]
    public List<MarkingLayerData> Layers { get; private set; } = default!;

    /// <summary>
    ///     Whether or not this marking prototype uses the new layer metadata system,
    ///     as opposed to the old "sprites" list.
    /// </summary>
    public bool UsesLayers()
    {
        return Layers != null && Layers.Count > 0;
    }

    /// <summary>
    ///     Whether or not this marking prototype has any layers with forced coloration.
    /// </summary>
    public bool HasForcedColorLayer()
    {
        return UsesLayers() && Layers.Any(layer => layer.ForcedColoring);
    }

    /// <summary>
    ///     Gets a number of adjustible colors associated with this marking.
    /// </summary>
    public int GetColorCount()
    {
        return UsesLayers() ? Layers.Count : Sprites.Count;
    }

    public Marking AsMarking()
    {
        return new Marking(ID, GetColorCount());
    }
}
