using System.Numerics;
using Content.Server.UserInterface;
using Content.Shared.Shuttles.BUIStates;
using Content.Shared.Shuttles.Components;
using Content.Shared.Shuttles.Systems;
using Content.Shared.PowerCell;
using Content.Shared.Movement.Components;
using Robust.Server.GameObjects;
using Robust.Shared.Map;

namespace Content.Server.Shuttles.Systems;

public sealed class RadarConsoleSystem : SharedRadarConsoleSystem
{
    [Dependency] private readonly ShuttleConsoleSystem _console = default!;
    [Dependency] private readonly UserInterfaceSystem _uiSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RadarConsoleComponent, ComponentStartup>(OnRadarStartup);
    }

    private void OnRadarStartup(EntityUid uid, RadarConsoleComponent component, ComponentStartup args)
    {
        UpdateState(uid, component);
    }

    protected override void UpdateState(EntityUid uid, RadarConsoleComponent component)
    {
        var xform = Transform(uid);
        var onGrid = xform.ParentUid == xform.GridUid;
        EntityCoordinates? coordinates = onGrid ? xform.Coordinates : null;
        Angle? angle = onGrid ? xform.LocalRotation : null;

        if (component.FollowEntity)
        {
            coordinates = new EntityCoordinates(uid, Vector2.Zero);
            angle = Angle.Zero;
        }

        if (_uiSystem.TryGetUi(uid, RadarConsoleUiKey.Key, out var bui))
        {
            NavInterfaceState state;
            var docks = _console.GetAllDocks();

            if (coordinates != null && angle != null)
            {
                state = _console.GetNavState(uid, docks, coordinates.Value, angle.Value);
            }
            else
            {
                state = _console.GetNavState(uid, docks);
            }

            _uiSystem.SetUiState(bui, new NavBoundUserInterfaceState(state));
        }
    }
}
