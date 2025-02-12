using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.CardboardBox.Components;
/// <summary>
/// Allows a user to control an EntityStorage entity while inside of it.
/// Used for big cardboard box entities.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class CardboardBoxComponent : Component
{
    /// <summary>
    /// The person in control of this box
    /// </summary>
    [DataField("mover")]
    public EntityUid? Mover;

    /// <summary>
    /// The entity used for the box opening effect
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField]
    public string Effect = "Exclamation";

    /// <summary>
    /// Sound played upon effect creation
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("effectSound")]
    public SoundSpecifier? EffectSound;

	/// <summary>
	/// Whether to prevent the box from making the sound and effect
	/// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
	[DataField]
	public bool Quiet = false;

    /// <summary>
    /// How far should the box opening effect go?
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField]
    public float Distance = 6f;

    /// <summary>
    /// Time at which the sound effect can next be played.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)))]
    public TimeSpan EffectCooldown;

    /// <summary>
    /// Time between sound effects. Prevents effect spam
    /// </summary>
    [DataField]
    public TimeSpan CooldownDuration = TimeSpan.FromSeconds(5f);
}

[Serializable, NetSerializable]
public sealed class PlayBoxEffectMessage : EntityEventArgs
{
    public NetEntity Source;
    public NetEntity Mover;

    public PlayBoxEffectMessage(NetEntity source, NetEntity mover)
    {
        Source = source;
        Mover = mover;
    }
}
