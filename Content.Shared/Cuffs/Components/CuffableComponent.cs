using Content.Shared.Damage;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared.Cuffs.Components;

[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedCuffableSystem))]
public sealed partial class CuffableComponent : Component
{
    /// <summary>
    /// The current RSI for the handcuff layer
    /// </summary>
    [DataField("currentRSI"), ViewVariables(VVAccess.ReadWrite)]
    public string? CurrentRSI;

    /// <summary>
    /// How many of this entity's hands are currently cuffed.
    /// </summary>
    [ViewVariables]
    public int CuffedHandCount => Container.ContainedEntities.Count * 2;

    /// <summary>
    /// The last pair of cuffs that was added to this entity.
    /// </summary>
    [ViewVariables]
    public EntityUid LastAddedCuffs => Container.ContainedEntities[^1];

    /// <summary>
    ///     Container of various handcuffs currently applied to the entity.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    public Container Container = default!;

    /// <summary>
    /// Whether or not the entity can still interact (is not cuffed)
    /// </summary>
    [DataField("canStillInteract"), ViewVariables(VVAccess.ReadWrite)]
    public bool CanStillInteract = true;
}

[Serializable, NetSerializable]
public sealed class CuffableComponentState : ComponentState
{
    public readonly bool CanStillInteract;
    public readonly int NumHandsCuffed;
    public readonly string? RSI;
    public readonly string? IconState;
    public readonly Color? Color;

    public CuffableComponentState(int numHandsCuffed, bool canStillInteract, string? rsiPath, string? iconState, Color? color)
    {
        NumHandsCuffed = numHandsCuffed;
        CanStillInteract = canStillInteract;
        RSI = rsiPath;
        IconState = iconState;
        Color = color;
    }
}

[ByRefEvent]
public readonly record struct CuffedStateChangeEvent;

