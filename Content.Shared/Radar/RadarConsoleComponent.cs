using Robust.Shared.GameStates;

namespace Content.Shared.Radar;

[RegisterComponent, NetworkedComponent, Friend(typeof(SharedRadarConsoleSystem))]
public sealed class RadarConsoleComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite)]
    public float RangeVV
    {
        get => Range;
        set => IoCManager
            .Resolve<IEntitySystemManager>()
            .GetEntitySystem<SharedRadarConsoleSystem>()
            .SetRange(this, value);
    }

    [ViewVariables, DataField("range")]
    public float Range = 128f;

    [ViewVariables, DataField("minRange")]
    public float MinimumRange = 64f;
}
