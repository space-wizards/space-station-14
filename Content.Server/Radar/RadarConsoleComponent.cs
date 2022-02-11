using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.Radar;

[RegisterComponent]
public class RadarConsoleComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("range")]
    public float Range = 256f;
}
