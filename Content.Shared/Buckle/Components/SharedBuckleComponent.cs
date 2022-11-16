using Content.Shared.Interaction;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Buckle.Components;

[NetworkedComponent]
[Access(typeof(SharedBuckleSystem))]
public abstract class SharedBuckleComponent : Component
{
    /// <summary>
    ///     The range from which this entity can buckle to a <see cref="SharedStrapComponent"/>.
    /// </summary>
    [ViewVariables]
    [DataField("range")]
    public float Range { get; protected set; } = SharedInteractionSystem.InteractionRange / 1.4f;

    /// <summary>
    ///     True if the entity is buckled, false otherwise.
    /// </summary>
    public bool Buckled { get; set; }

    public EntityUid? LastEntityBuckledTo { get; set; }

    public bool DontCollide { get; set; }
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
