using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.CardboardBox.Components;

/// <summary>
/// Allows a user to control an EntityStorage entity while inside of it.
/// Used for big cardboard box entities.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
public sealed partial class CardboardBoxComponent : Component
{
    /// <summary>
    /// The person in control of this box
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? Mover;

    /// <summary>
    /// The entity used for the box opening effect
    /// </summary>
    [DataField]
    public EntProtoId Effect = "Exclamation";

    /// <summary>
    /// Sound played upon effect creation
    /// </summary>
    [DataField]
    public SoundSpecifier? EffectSound;

	/// <summary>
	/// Whether to prevent the box from making the sound and effect
	/// </summary>
    [DataField]
	public bool Quiet;

    /// <summary>
    /// How far should the box opening effect go?
    /// </summary>
    [DataField]
    public float Distance = 6f;

    /// <summary>
    /// Time at which the sound effect can next be played.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan EffectCooldown;

    /// <summary>
    /// Time between sound effects. Prevents effect spam
    /// </summary>
    [DataField]
    public TimeSpan CooldownDuration = TimeSpan.FromSeconds(5f);
}

/// <summary>
/// Message to play the box effect.
/// </summary>
[Serializable, NetSerializable]
public sealed class PlayBoxEffectMessage(NetEntity source, NetEntity mover) : EntityEventArgs
{
    public NetEntity Source = source;
    public NetEntity Mover = mover;
}
