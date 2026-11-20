using Content.Shared.Damage;
using Content.Shared.Throwing;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared.Clumsy.Components;

/// <summary>
/// Afflicted entity will occasionally fail to catch items thrown at it.
/// </summary>
/// <seealso cref="CatchableComponent"/>
[RegisterComponent, NetworkedComponent]
[Access(typeof(ClumsyStatusEffectSystem))]
public sealed partial class ClumsyCatchStatusEffectComponent : Component
{
    /// <summary>
    /// How often to fail.
    /// </summary>
    [DataField]
    public float ClumsyChance = 0.5f;

    /// <summary>
    /// Sound played upon failure.
    /// </summary>
    [DataField]
    public SoundSpecifier? ClumsySound = new SoundPathSpecifier("/Audio/Items/bikehorn.ogg");

    /// <summary>
    /// Damage taken after failing.
    /// </summary>
    [DataField]
    public DamageSpecifier? FailDamage;

    /// <summary>
    /// Popup played to the afflicted when they fail.
    /// </summary>
    /// <value> Parameters passed in:
    /// <list type="bullet">
    ///     <item><c>item</c> - The item failed to be caught.</item>
    /// </list>
    /// </value>
    [DataField]
    public LocId? SelfFailedMessage = "clumsy-catch-fail-message-user";

    /// <summary>
    /// Popup played to others when the afflicted fails.
    /// </summary>
    /// <value> Parameters passed in:
    /// <list type="bullet">
    ///     <item><c>item</c> - The item failed to be caught.</item>
    ///     <item><c>catcher</c> - The entity failing the catch.</item>
    /// </list>
    /// </value>
    [DataField]
    public LocId? OtherFailedMessage = "clumsy-catch-fail-message-others";
}
