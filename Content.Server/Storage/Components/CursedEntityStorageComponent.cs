using Robust.Shared.Audio;

namespace Content.Server.Storage.Components;

[RegisterComponent]
public sealed partial class CursedEntityStorageComponent : Component
{
    [DataField("cursedSound")]
    public SoundSpecifier CursedSound = new SoundPathSpecifier("/Audio/Effects/teleport_departure.ogg");
}
