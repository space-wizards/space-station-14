using Robust.Shared.Map;

namespace Content.Server.Medical.Surgery;

[RegisterComponent]
[Access(typeof(SurgeryRealmSystem))]
public sealed class SurgeryRealmToolComponent : Component
{
    [ViewVariables] public readonly HashSet<EntityUid> Victims = new();

    [ViewVariables] public MapCoordinates? Position;

    [DataField("heart")] public string HeartPrototype = "SurgeryRealmHeart";
}
