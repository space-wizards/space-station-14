using Content.Shared.Damage;
using Content.Shared.Weapons.Ranged.Components;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared.Clumsy.Components;

/// <summary>
/// Afflicted entity will occasionally fail to fire a gun.
/// </summary>
/// <seealso cref="GunComponent"/>
[RegisterComponent, NetworkedComponent]
[Access(typeof(ClumsyStatusEffectSystem))]
public sealed partial class ClumsyGunStatusEffectComponent : Component
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
    /// Noise to play after failing to shoot a gun. Boom!
    /// </summary>
    /// <remarks>This should probably be on the gun itself, but that's a tall order.</remarks>
    [DataField]
    public SoundSpecifier? GunShootFailSound = new SoundPathSpecifier("/Audio/Weapons/Guns/Gunshots/bang.ogg");

    /// <summary>
    /// How much damage to take after failing.
    /// </summary>
    [DataField]
    public DamageSpecifier? FailDamage;

    /// <summary>
    /// How long to be stunned after failing.
    /// </summary>
    [DataField]
    public TimeSpan StunDuration = TimeSpan.FromSeconds(2.5);

    /// <summary>
    /// Popup played to the afflicted when they fail.
    /// </summary>
    [DataField]
    public LocId? SelfFailedMessage = "clumsy-gun-fail-message"; //todo

    /// <summary>
    /// Popup played to others when the afflicted fails.
    /// </summary>
    [DataField]
    public LocId? OtherFailedMessage = "clumsy-gun-fail-message"; //todo
}
