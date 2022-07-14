using Content.Shared.Audio;
using Content.Shared.Interaction;
using Content.Shared.Sound;

namespace Content.Server.Storage.Components;

[RegisterComponent]
public sealed class CursedEntityStorageComponent : Component
{
    [DataField("cursedSound")]
    public SoundSpecifier CursedSound = new SoundPathSpecifier("/Audio/Effects/teleport_departure.ogg");
}
