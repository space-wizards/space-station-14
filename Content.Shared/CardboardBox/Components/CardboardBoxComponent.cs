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
public sealed class CardboardBoxComponent : Component
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
    [DataField("effect")]
    public string Effect = "Exclamation";

    /// <summary>
    /// Sound played upon effect creation
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("effectSound")]
    public SoundSpecifier? EffectSound;

    /// <summary>
    /// How far should the box opening effect go?
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("distance")]
    public float Distance = 6f;

    /// <summary>
    /// Current time + max effect cooldown to check to see if effect can play again
    /// Prevents effect spam
    /// </summary>
    [DataField("effectCooldown", customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan EffectCooldown = TimeSpan.FromSeconds(1f);

    /// <summary>
    /// How much time should pass + current time until the effect plays again
    /// Prevents effect spam
    /// </summary>
    [DataField("maxEffectCooldown", customTypeSerializer: typeof(TimeOffsetSerializer))]
    public static readonly TimeSpan MaxEffectCooldown = TimeSpan.FromSeconds(5f);
}

[Serializable, NetSerializable]
public sealed class PlayBoxEffectMessage : EntityEventArgs
{
    public EntityUid Source;
    public EntityUid Mover;

    public PlayBoxEffectMessage(EntityUid source, EntityUid mover)
    {
        Source = source;
        Mover = mover;
    }
}
