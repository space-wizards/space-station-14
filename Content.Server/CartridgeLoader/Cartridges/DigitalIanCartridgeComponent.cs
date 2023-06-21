using Content.Shared.CartridgeLoader.Cartridges;
using Robust.Shared.Audio;
using Robust.Shared.Utility;

namespace Content.Server.CartridgeLoader.Cartridges;

[RegisterComponent]
public sealed class DigitalIanCartridgeComponent : Component
{
    [DataField("soundFeed")]
    public SoundSpecifier SoundFeed = new SoundPathSpecifier("/Audio/Items/eatfood.ogg");

    [DataField("soundpet")]
    public SoundSpecifier SoundPet = new SoundPathSpecifier("/Audio/Animals/small_dog_bark_happy.ogg");

}
