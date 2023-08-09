using Content.Shared.Interaction;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Buckle.Components;

[RegisterComponent, NetworkedComponent]
public sealed class BuckleComponent : Component
{
    /// <summary>
    ///     The range from which this entity can buckle to a <see cref="StrapComponent"/>.
    /// </summary>
    [DataField("range")]
    public float Range = SharedInteractionSystem.InteractionRange / 1.4f;

    /// <summary>
    ///     True if the entity is buckled, false otherwise.
    /// </summary>
    public bool Buckled { get; set; }

    public EntityUid? LastEntityBuckledTo { get; set; }

    public bool DontCollide { get; set; }

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

    /// <summary>
    /// Used for client rendering
    /// </summary>
    public int? OriginalDrawDepth { get; set; }
}

[Serializable, NetSerializable]
public sealed class BuckleComponentState : ComponentState
{
    public BuckleComponentState(bool buckled, EntityUid? lastEntityBuckledTo, bool dontCollide)
    {
        Buckled = buckled;
        LastEntityBuckledTo = lastEntityBuckledTo;
        DontCollide = dontCollide;
    }

    public bool Buckled { get; }
    public EntityUid? LastEntityBuckledTo { get; }
    public bool DontCollide { get; }
}

public sealed class BuckleChangeEvent : EntityEventArgs
{
    public EntityUid Strap;

    public EntityUid BuckledEntity;
    public bool Buckling;
}

[Serializable, NetSerializable]
public enum BuckleVisuals
{
    Buckled
}
