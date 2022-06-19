using Content.Shared.Shuttles.Systems;
using Robust.Shared.GameStates;

namespace Content.Shared.Shuttles.Components;

[RegisterComponent, NetworkedComponent, Access(typeof(SharedRadarConsoleSystem))]
public sealed class RadarConsoleComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite)]
    public float RangeVV
    {
        get => MaxRange;
        set => IoCManager
            .Resolve<IEntitySystemManager>()
            .GetEntitySystem<SharedRadarConsoleSystem>()
            .SetRange(this, value);
    }

    [ViewVariables, DataField("maxRange")]
    public float MaxRange = 120f;
}
