using Content.Shared.Climbing.Components;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared.Clumsy.Components;

/// <summary>
/// Afflicted entity will occasionally bonk their head when trying to climb something bonkable.
/// </summary>
/// <seealso cref="BonkableComponent"/>
[RegisterComponent, NetworkedComponent]
[Access(typeof(ClumsyStatusEffectSystem))]
public sealed partial class ClumsyVaultStatusEffectComponent : Component
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
    /// Popup played to the afflicted when they fail.
    /// </summary>
    [DataField]
    public LocId? SelfFailedMessage = "clumsy-vaulting-fail-message-user";

    /// <summary>
    /// Popup played to others when the afflicted fails.
    /// </summary>
    [DataField]
    public LocId? OtherFailedMessage = "clumsy-vaulting-fail-message-others";

    /// <summary>
    /// Popup played when this entity is forced to vault and fails.
    /// </summary>
    [DataField]
    public LocId? ForcedMessage = "clumsy-vaulting-fail-forced-message";
}
