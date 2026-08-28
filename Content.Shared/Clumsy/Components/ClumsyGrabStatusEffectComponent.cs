using Robust.Shared.GameStates;

namespace Content.Shared.Clumsy.Components;

/// <summary>
/// Afflicted entity will occasionally fail to pick up items/hold on to any items they are given
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(ClumsyStatusEffectSystem))]
public sealed partial class ClumsyGrabStatusEffectComponent : Component
{
    /// <summary>
    /// How often they fail.
    /// </summary>
    [DataField]
    public float ClumsyChance = 0.5f;
    
    /// <summary>
    /// Popup played to the afflicted when they fail to grab the item.
    /// </summary>
    /// <value> Parameters passed in:
    /// <list type="bullet">
    ///     <item><c>item</c> - The item which got dropped.</item>
    /// </list>
    /// </value>
    [DataField]
    public LocId? SelfFailedMessage = "clumsy-grab-fail-message-user";

    /// <summary>
    /// Popup played to others when the afflicted fails to grab the item.
    /// </summary>
    /// <value> Parameters passed in:
    /// <list type="bullet">
    ///     <item><c>item</c> - The item which got dropped.</item>
    /// </list>
    /// </value>
    [DataField]
    public LocId? OtherFailedMessage = "clumsy-grab-fail-message-others";
}