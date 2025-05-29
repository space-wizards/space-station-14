using Robust.Shared.Audio;

namespace Content.Server.Storage.Components;

[RegisterComponent]
public sealed partial class CursedEntityStorageComponent : Component
{
    [DataField]
    public SoundSpecifier CursedSound = new SoundPathSpecifier("/Audio/Effects/teleport_departure.ogg", AudioParams.Default.WithVariation(0.125f));
}
