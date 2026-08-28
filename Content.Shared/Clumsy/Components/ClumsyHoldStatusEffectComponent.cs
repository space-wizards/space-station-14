using Robust.Shared.GameStates;

namespace Content.Shared.Clumsy.Components;

/// <summary>
/// Afflicted entity will occasionally fail to hold on to any items they are given
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(ClumsyStatusEffectSystem))]
public sealed partial class ClumsyHoldStatusEffectComponent : Component
{
    /// <summary>
    /// How often to fail.
    /// </summary>
    [DataField]
    public float ClumsyChance = 0.5f;
    
    /// <summary>
    /// Popup played to the afflicted when they fail.
    /// </summary>
    /// <value> Parameters passed in:
    /// <list type="bullet">
    /// </list>
    /// </value>
    [DataField]
    public LocId? SelfFailedMessage = "clumsy-hold-fail-message-user";

    /// <summary>
    /// Popup played to others when the afflicted fails.
    /// </summary>
    /// <value> Parameters passed in:
    /// <list type="bullet">
    ///     <item><c>holder</c> - The entity failing to hold the thing.</item>
    /// </list>
    /// </value>
    [DataField]
    public LocId? OtherFailedMessage = "clumsy-hold-fail-message-others";
}