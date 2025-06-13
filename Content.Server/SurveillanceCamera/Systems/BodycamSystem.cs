using Content.Server.Emp;
using Content.Shared.Emp;
using Content.Shared.IdentityManagement;
using Content.Shared.SurveillanceCamera;

namespace Content.Server.SurveillanceCamera.Systems;

/// <inheritdoc cref="SharedBodycamSystem"/>
public sealed class BodycamSystem: SharedBodycamSystem
{
    [Dependency] private readonly SurveillanceCameraSystem _camera = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<BodycamComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<BodycamComponent, EmpPulseEvent>(OnEmp);
        SubscribeLocalEvent<BodycamComponent, SurveillanceCameraReactivateAfterEmpAttemptEvent>(OnEmpReactivate);
    }

    private void OnMapInit(EntityUid uid, BodycamComponent comp, MapInitEvent args)
    {
        // disable the body cam on map init
        // otherwise you would be able to connect to them before they are worn
        if (HasComp<SurveillanceCameraComponent>(uid))
            _camera.SetActive(uid, false);
    }

    private void OnEmp(EntityUid uid, BodycamComponent comp, ref EmpPulseEvent args)
    {
        args.Affected = true;
        args.Disabled = true;
        comp.State = BodycamState.Disabled;
        Dirty(uid, comp);
    }

    // The surveillance camera system will automatically try and turn cameras back on after being emp-ed
    // But we want our bodycams to stay off until someone manually flicks it back on
    // So we cancel the event to automatically reactivate the camera
    private void OnEmpReactivate(EntityUid uid, BodycamComponent comp, ref SurveillanceCameraReactivateAfterEmpAttemptEvent args)
    {
        args.Cancelled = true;
    }

    /// <inheritdoc cref="SharedBodycamSystem.SwitchOn"/>
    protected override bool SwitchOn(EntityUid uid, BodycamComponent comp, EntityUid user)
    {
        if (!base.SwitchOn(uid, comp, user))
            return false;
        _camera.SetActive(uid, true);
        var bodycamName = Loc.GetString("bodycam-name", ("wearer", Identity.Name(comp.Wearer!.Value, EntityManager))); // we know wearer will never be null if we got this far
        _camera.SetName(uid, bodycamName);
        return true;
    }

    /// <inheritdoc cref="SharedBodycamSystem.SwitchOff"/>
    protected override bool SwitchOff(EntityUid uid, BodycamComponent comp, EntityUid? user, EntityUid? unequipper)
    {
        if (!base.SwitchOff(uid, comp, user, unequipper))
            return false;
        _camera.SetActive(uid, false);
        return true;
    }
}
