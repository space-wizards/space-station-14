using System.Numerics;
using Content.Shared.Shuttles.BUIStates;
using Content.Shared.Shuttles.Components;
using Content.Shared.Shuttles.Systems;
using Robust.Shared.Map;

namespace Content.Server.Shuttles.Systems;

public sealed class RadarConsoleSystem : SharedRadarConsoleSystem
{
    [Dependency] private readonly ShuttleConsoleSystem _console = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _uiSystem = default!;

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

        if (_uiSystem.HasUi(uid, RadarConsoleUiKey.Key))
        {
            NavInterfaceState state;
            var docks = _console.GetAllDocks();
            var tracked = _console.GetTracked();

            if (coordinates != null && angle != null)
            {
                state = _console.GetNavState(uid, docks, tracked, coordinates.Value, angle.Value);
            }
            else
            {
                state = _console.GetNavState(uid, docks, tracked);
            }

            state.RotateWithEntity = !component.FollowEntity;

            _uiSystem.SetUiState(uid, RadarConsoleUiKey.Key, new NavBoundUserInterfaceState(state));
        }
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = AllEntityQuery<RadarConsoleComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            UpdateState(uid, comp);
        }
    }
}
