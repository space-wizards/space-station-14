using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Chameleon.Components;
[NetworkedComponent]
public abstract class SharedChameleonComponent : Component
{
    /// <summary>
    ///     Whether or not the entity previously had an interaction outline prior to cloaking.
    /// </summary>
    [ViewVariables]
    [DataField("hadOutline")]
    public bool HadOutline;

    [ViewVariables]
    [DataField("speed")]
    public float Speed;
}

[Serializable, NetSerializable]
public sealed class ChameleonComponentState : ComponentState
{
    public bool HadOutline;
    public float Speed;

    public ChameleonComponentState(bool hadOutline, float speed)
    {
        HadOutline = hadOutline;
        Speed = speed;
    }
}
