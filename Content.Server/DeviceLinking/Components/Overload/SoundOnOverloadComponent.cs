using Content.Server.DeviceLinking.Systems;
using Robust.Shared.Audio;

namespace Content.Server.DeviceLinking.Components.Overload;

/// <summary>
/// Plays a sound when a device link overloads.
/// An overload happens when a device link sink is invoked to many times per tick
/// and it raises a <see cref="Content.Shared.DeviceLinking.Events.DeviceLinkOverloadedEvent"/>
/// </summary>
[RegisterComponent]
[Access(typeof(DeviceLinkOverloadSystem))]
public sealed partial class SoundOnOverloadComponent : Component
{
    /// <summary>
    /// Sound to play when the device overloads
    /// </summary>
    [DataField("sound")]
    public SoundSpecifier? OverloadSound = new SoundPathSpecifier("/Audio/Items/Defib/defib_zap.ogg");

    /// <summary>
    /// Modifies the volume the sound is played at
    /// </summary>
    [DataField("volumeModifier")]
    public float VolumeModifier;
}
