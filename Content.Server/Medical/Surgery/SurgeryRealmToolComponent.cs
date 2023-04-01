using Robust.Server.Player;
using Robust.Shared.Map;

namespace Content.Server.Medical.Surgery;

[RegisterComponent]
[Access(typeof(SurgeryRealmSystem))]
public sealed class SurgeryRealmToolComponent : Component
{
    [ViewVariables] public readonly HashSet<IPlayerSession> Victims = new();

    [ViewVariables] public MapCoordinates? Position;

    [DataField("heart")] public string HeartPrototype = "SurgeryRealmHeart";

    public int Fight;
}
