using Content.Shared.NodeContainer.NodeGroups;

namespace Content.Shared.Power.Generation.Teg.Nodes;

/// <summary>
/// Node group that connects the central TEG with its two circulators.
/// </summary>
/// <seealso cref="TegNodeGenerator"/>
/// <seealso cref="TegNodeCirculator"/>
/// <seealso cref="TegSystem"/>
public sealed class TegNodeGroup : BaseNodeGroup
{
    /// <summary>
    /// If true, this TEG is fully built and has all its parts properly connected.
    /// </summary>
    [ViewVariables]
    public bool IsFullyBuilt;

    /// <summary>
    /// The central generator component.
    /// </summary>
    /// <seealso cref="TegGeneratorComponent"/>
    [ViewVariables]
    public TegNodeGenerator? Generator;

    // Illustration for how the TEG A/B circulators are laid out.
    // Circulator B       Generator        Circulator A
    //     ^                   ->               |
    //     |                                    V
    // They have rotations like the arrows point out.

    /// <summary>
    /// The A-side circulator. This is the circulator that is in the direction FACING the center component's rotation.
    /// </summary>
    /// <remarks>
    /// Not filled in if there is no center piece to deduce relative rotation from.
    /// </remarks>
    /// <seealso cref="TegCirculatorComponent"/>
    [ViewVariables]
    public TegNodeCirculator? CirculatorA;

    /// <summary>
    /// The B-side circulator. This circulator is opposite <see cref="CirculatorA"/>.
    /// </summary>
    /// <remarks>
    /// Not filled in if there is no center piece to deduce relative rotation from.
    /// </remarks>
    /// <seealso cref="TegCirculatorComponent"/>
    [ViewVariables]
    public TegNodeCirculator? CirculatorB;
}
