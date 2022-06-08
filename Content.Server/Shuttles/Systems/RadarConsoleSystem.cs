using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Shared.Shuttles.BUIStates;
using Content.Shared.Shuttles.Components;
using Content.Shared.Shuttles.Systems;
using Robust.Server.GameObjects;

namespace Content.Server.Shuttles.Systems;

public sealed class RadarConsoleSystem : SharedRadarConsoleSystem
{
    [Dependency] private readonly UserInterfaceSystem _uiSystem = default!;

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
        var empty = !this.IsPowered(component.Owner, EntityManager) || !(Transform(component.Owner).Anchored);
        var radarState = new RadarConsoleBoundInterfaceState(component.Range, empty ? null : component.Owner);
        _uiSystem.GetUiOrNull(component.Owner, RadarConsoleUiKey.Key)?.SetState(radarState);
    }
}
