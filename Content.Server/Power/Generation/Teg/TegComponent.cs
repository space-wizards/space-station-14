namespace Content.Server.Power.Generation.Teg;

[RegisterComponent]
public sealed partial class TegComponent : Component
{
    /// </summary>
    /// Whether all of the component parts of the TEG are present.
    /// </summary>
    [ViewVariables]
    public bool IsFullyBuilt = false;


    // Illustration for how the TEG A/B circulators are laid out.
    // 
    // 
    //   Circulator B       Generator        Circulator A
    //       ^                  |                 |
    //       |                  V                 V
    // They have rotations like the arrows point out.
    // This means that circulators need West connectors and the generator needs East + West connectors.

    /// </summary>
    /// The central generator component.
    /// </summary>
    [ViewVariables]
    public EntityUid? Generator = null;

    /// <summary>
    /// The A-side circulator. This is the circulator that is in the direction FACING the center component's rotation.
    /// </summary>
    /// <remarks>
    /// Not filled in if there is no center piece to deduce relative rotation from.
    /// </remarks>
    [ViewVariables]
    public EntityUid? CirculatorA = null;

    /// <summary>
    /// The B-side circulator. This circulator is opposite <see cref="CirculatorA"/>.
    /// </summary>
    /// <remarks>
    /// Not filled in if there is no center piece to deduce relative rotation from.
    /// </remarks>
    [ViewVariables]
    public EntityUid? CirculatorB = null;
}
