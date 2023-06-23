using Robust.Shared.Audio;

namespace Content.Server.CartridgeLoader.Cartridges;

[RegisterComponent]
public sealed class DigitalIanCartridgeComponent : Component
{
    [DataField("soundFeed")]
    public SoundSpecifier SoundFeed = new SoundPathSpecifier("/Audio/Items/eatfood.ogg");

    [DataField("soundPet")]
    public SoundSpecifier SoundPet = new SoundPathSpecifier("/Audio/Animals/small_dog_bark_happy.ogg");
}
