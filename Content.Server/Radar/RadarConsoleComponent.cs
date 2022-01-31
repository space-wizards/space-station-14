using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.Radar;

[RegisterComponent, ComponentProtoName("RadarConsole")]
public class RadarConsoleComponent : Component
{
    [DataField("range")]
    public float Range = 256f;
}
