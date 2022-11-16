using Content.Shared.Buckle.Components;

namespace Content.Server.Buckle.Components;

/// <summary>
///     Component that handles sitting entities into <see cref="StrapComponent"/>s.
/// </summary>
[RegisterComponent]
[ComponentReference(typeof(SharedBuckleComponent))]
public sealed class BuckleComponent : SharedBuckleComponent
{
    /// <summary>
    ///     The amount of time that must pass for this entity to
    ///     be able to unbuckle after recently buckling.
    /// </summary>
    [DataField("delay")]
    public TimeSpan UnbuckleDelay = TimeSpan.FromSeconds(0.25f);

    /// <summary>
    ///     The time that this entity buckled at.
    /// </summary>
    [ViewVariables] public TimeSpan BuckleTime;

    /// <summary>
    ///     The strap that this component is buckled to.
    /// </summary>
    [ViewVariables]
    public StrapComponent? BuckledTo { get; set; }

    /// <summary>
    ///     The amount of space that this entity occupies in a
    ///     <see cref="StrapComponent"/>.
    /// </summary>
    [DataField("size")]
    public int Size = 100;
}
