using Content.Server.Power.Components;
using Content.Shared.Radar;
using Robust.Server.GameObjects;
using Robust.Shared.Map;

namespace Content.Server.Radar;

public sealed class RadarConsoleSystem : SharedRadarConsoleSystem
{
    [Dependency] private readonly IMapManager _mapManager = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RadarConsoleComponent, PowerChangedEvent>(OnRadarPowerChange);
        SubscribeLocalEvent<RadarConsoleComponent, AnchorStateChangedEvent>(OnRadarAnchorChange);
    }

    private void OnRadarAnchorChange(EntityUid uid, RadarConsoleComponent component, ref AnchorStateChangedEvent args)
    {
        UpdateState(component);
    }

    private void OnRadarPowerChange(EntityUid uid, RadarConsoleComponent component, PowerChangedEvent args)
    {
        UpdateState(component);
    }

    protected override void UpdateState(RadarConsoleComponent component)
    {
        // TODO: send null if no power or unanchored.
        var radarState = new RadarConsoleBoundInterfaceState(component.Range, component.Owner);
        Get<UserInterfaceSystem>().GetUiOrNull(component.Owner, RadarConsoleUiKey.Key)?.SetState(radarState);
    }
}
