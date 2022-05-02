using Content.Shared.Actions;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Magic.Events;

public sealed class ForceWallEvent : InstantActionEvent
{
    [DataField("wallPrototype", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string WallPrototype = "WallCult";
}
