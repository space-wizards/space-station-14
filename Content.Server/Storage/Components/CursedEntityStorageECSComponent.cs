using Content.Shared.Sound;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.Storage.Components
{
    [RegisterComponent]
    public class CursedEntityStorageECSComponent : Component
    {
        public override string Name => "CursedEntityStorageECS";

        [DataField("cursedSound")] public SoundSpecifier CursedSound { get; set; } = new SoundPathSpecifier("/Audio/Effects/teleport_departure.ogg");
        [DataField("cursedLockerSound")] public SoundSpecifier CursedLockerSound { get; set; } = new SoundPathSpecifier("/Audio/Effects/teleport_arrival.ogg");
    }
}
