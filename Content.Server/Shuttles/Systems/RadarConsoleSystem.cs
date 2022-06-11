using Content.Shared.Shuttles.BUIStates;
using Content.Shared.Shuttles.Components;
using Content.Shared.Shuttles.Systems;
using Robust.Server.GameObjects;
using Robust.Shared.Map;

namespace Content.Server.Shuttles.Systems;

public sealed class RadarConsoleSystem : SharedRadarConsoleSystem
{
    [Dependency] private readonly UserInterfaceSystem _uiSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RadarConsoleComponent, ComponentStartup>(OnRadarStartup);
    }

    private void OnRadarStartup(EntityUid uid, RadarConsoleComponent component, ComponentStartup args)
    {
        UpdateState(component);
    }

    protected override void UpdateState(RadarConsoleComponent component)
    {
        var radarState = new RadarConsoleBoundInterfaceState(component.MaxRange, component.Owner, new List<DockingInterfaceState>());
        _uiSystem.GetUiOrNull(component.Owner, RadarConsoleUiKey.Key)?.SetState(radarState);
    }
}
