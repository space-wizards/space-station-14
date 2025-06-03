using Content.Shared.IdentityManagement;
using Content.Shared.SurveillanceCamera;

namespace Content.Server.SurveillanceCamera.Systems;

public sealed class BodycamSystem: SharedBodycamSystem
{
    [Dependency] private readonly SurveillanceCameraSystem _camera = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<BodycamComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(EntityUid uid, BodycamComponent comp, MapInitEvent args)
    {
        // disable the body cam on map init
        // otherwise you would be able to connect to them before they are worn
        _camera.SetActive(uid, false);
    }

    /// <inheritdoc cref="SharedBodycamSystem.SwitchOn"/>
    protected override void SwitchOn(EntityUid uid, BodycamComponent comp, EntityUid user)
    {
        if (comp.Wearer == null)
            return;

        base.SwitchOn(uid, comp, user);
        _camera.SetActive(uid, true);
        var bodycamName = Loc.GetString("bodycam-name", ("wearer", Identity.Name(comp.Wearer.Value, EntityManager)));
        _camera.SetName(uid, bodycamName);
    }

    /// <inheritdoc cref="SharedBodycamSystem.SwitchOff"/>
    protected override void SwitchOff(EntityUid uid, BodycamComponent comp, EntityUid user, bool causedByPlayer)
    {
        base.SwitchOff(uid, comp, user, causedByPlayer);
        _camera.SetActive(uid, false);
    }
}
