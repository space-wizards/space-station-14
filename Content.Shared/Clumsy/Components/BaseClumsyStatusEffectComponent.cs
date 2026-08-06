using Content.Shared.Damage;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared.Clumsy.Components;

/// <summary>
/// Common fields used by clumsy status effects.
/// </summary>
public abstract partial class BaseClumsyStatusEffectComponent : Component
{
    /// <summary>
    /// How often the entity will fail an interaction.
    /// </summary>
    [DataField]
    public float ClumsyChance = 0.5f;

    /// <summary>
    /// Sound play from the entity when interactions fail.
    /// </summary>
    [DataField]
    public SoundSpecifier? ClumsySound = new SoundPathSpecifier("/Audio/Items/bikehorn.ogg");

    /// <summary>
    /// Popup played to the afflicted when they fail.
    /// </summary>
    [DataField]
    public LocId? SelfFailedMessage = "clumsy-hypospray-fail-message"; //todo

    /// <summary>
    /// Popup played to others when the afflicted fails.
    /// </summary>
    [DataField]
    public LocId? OtherFailedMessage = "clumsy-gun-fail-message"; //todo
}
