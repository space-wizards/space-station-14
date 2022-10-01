using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Chameleon.Components;
[RegisterComponent, NetworkedComponent]
public sealed class SharedChameleonComponent : Component
{
    /// <summary>
    ///     Whether or not the entity previously had an interaction outline prior to cloaking.
    /// </summary>
    [DataField("hadOutline")]
    public bool HadOutline;

    /// <summary>
    /// Current level of stealth based on movement in the <exception cref="Chameleon SYstem"></exception>
    /// -1f and 1f are the current maximums for now.
    /// </summary>
    [ViewVariables]
    [DataField("stealthLevel")]
    public float StealthLevel;

    /// <summary>
    /// Rate that effects how fast an entity gains stealth
    /// If you want the entity to become invisible faster it should be higher than the <see cref="VisibilityRate"/>
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("invisibilityRate")]
    public float InvisibilityRate = 0.15f;

    /// <summary>
    /// Rate that effects how fast an entity loses stealth
    /// If you want the entity to become visible faster it should be higher than the <see cref="InvisibilityRate"/>
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("visibilityRate")]
    public float VisibilityRate = 0.2f;
}

[Serializable, NetSerializable]
public sealed class ChameleonComponentState : ComponentState
{
    public float StealthLevel;

    public ChameleonComponentState(float stealthLevel)
    {
        StealthLevel = stealthLevel;
    }
}
