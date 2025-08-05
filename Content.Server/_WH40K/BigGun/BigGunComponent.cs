using Robust.Shared.Audio;

namespace Content.Server.WH40K.BigGun;

[RegisterComponent, AutoGenerateComponentState]
public sealed partial class BigGunComponent : Component
{
    [DataField]
    public SoundSpecifier GunShootFailSound = new SoundPathSpecifier("/Audio/Effects/gib1.ogg");
}
