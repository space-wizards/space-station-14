using Robust.Shared.GameStates;

namespace Content.Shared.StatusEffects.Components;

[RegisterComponent, NetworkedComponent, Access(typeof(SharedStatusEffectsSystem))]
public sealed class StatusEffectComponent : Component
{
    /// <summary>
    /// Anything less than 1 will make it so that the effect can have infinite stacks applied to it.
    /// </summary>
    [DataField("maxStacks")]
    public int MaxStacks = -1;

    /// <summary>
    /// TODO: Recomment this
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public int Stacks = 1;

    #region Timer

    /// <summary>
    /// Some effects may not want to be on a timer, so the option is left here. It is true by default.
    /// </summary>
    [DataField("isTimed")]
    public bool IsTimed = true;

    /// <summary>
    /// TODO: Recomment this, in seconds.
    /// </summary>
    [DataField("defaultLength")]
    public float DefaultLength = 0f;


    /// <summary>
    /// The time the effect ends.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan EndTime = new();

    #endregion
}

