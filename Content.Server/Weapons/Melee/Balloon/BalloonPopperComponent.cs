using Content.Shared.Tag;
using Robust.Shared.Audio;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Weapons.Melee.Balloon;

/// <summary>
/// This is used for weapons that pop balloons on attack.
/// </summary>
[RegisterComponent]
public sealed partial class BalloonPopperComponent : Component
{
    /// <summary>
    /// The tag that marks something as a balloon.
    /// </summary>
    [DataField("balloonTag", customTypeSerializer: typeof(PrototypeIdSerializer<TagPrototype>))]
    public string BalloonTag = "Balloon";

    /// <summary>
    /// The sound played when a balloon is popped.
    /// </summary>
    [DataField("popSound")]
    public SoundSpecifier PopSound = new SoundPathSpecifier("/Audio/Effects/balloon-pop.ogg");
}
