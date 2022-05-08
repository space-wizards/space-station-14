using Content.Shared.Actions;
using Content.Shared.Sound;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Magic.Events;

public sealed class ForceWallSpellEvent : InstantActionEvent
{
    [DataField("wallPrototype", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string WallPrototype = "WallForce";

    [DataField("forceWallSound")]
    public SoundSpecifier ForceWallSound = new SoundPathSpecifier("/Audio/Magic/forcewall.ogg");
}
