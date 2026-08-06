using Content.Shared.Damage;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared.Clumsy.Components;

/// <summary>
/// Afflicted entity will occasionally bonk their head when trying to climb something.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ClumsyVaultStatusEffectComponent : BaseClumsyStatusEffectComponent
{
    /// <summary>
    /// A sound played from the target when failing to vault.
    /// </summary>
    [DataField]
    public SoundSpecifier TableBonkSound = new SoundCollectionSpecifier("TrayHit");

    /// <summary>
    /// How much damage to take after failing.
    /// </summary>
    [DataField]
    public DamageSpecifier? FailDamage;

    /// <summary>
    /// Popup played to afflicted forced to vault and failing.
    /// </summary>
    [DataField]
    public LocId? SelfForcedMessage = "clumsy-vaulting-fail-forced-message";

    /// <summary>
    /// Popup played to others when this entity is forced to vault and fails.
    /// </summary>
    [DataField]
    public LocId? OtherForcedMessage = "clumsy-vaulting-fail-forced-message";
}
