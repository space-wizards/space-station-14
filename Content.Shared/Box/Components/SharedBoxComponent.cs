using Robust.Shared.Audio;
using Robust.Shared.Serialization;

namespace Content.Shared.Box.Components;
/// <summary>
/// Allows a user to control an EntityStorage entity while inside of it.
/// Used for big cardboard box entities.
/// </summary>
public abstract class SharedBoxComponent : Component
{
    /// <summary>
    /// The person in control of this box
    /// </summary>
    [ViewVariables]
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
