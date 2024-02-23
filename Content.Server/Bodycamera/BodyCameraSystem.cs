using Content.Server.Access.Systems;
using Content.Server.SurveillanceCamera;
using Content.Shared.Bodycamera;
using Content.Shared.Inventory.Events;

namespace Content.Server.Bodycamera;

public sealed class BodyCameraSystem : SharedBodyCameraSystem
{
    [Dependency] private readonly SurveillanceCameraSystem _camera = default!;
    [Dependency] private readonly IdCardSystem _idCardSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BodyCameraComponent, ComponentStartup>(OnComponentStartup);
    }

    /// <summary>
    /// Sync the SurveillanceCameraComponent state (default enabled) to the BodyCamera state (default disabled)
    /// </summary>
    private void OnComponentStartup(Entity<BodyCameraComponent> bodyCamera, ref ComponentStartup args)
    {
        if (TryComp<SurveillanceCameraComponent>(bodyCamera, out var surveillanceCameraComponent))
            _camera.SetActive(bodyCamera, false, surveillanceCameraComponent);
    }

    /// <summary>
    /// Set the camera name to the equipee name and job role
    /// </summary>
    protected override void OnEquipped(Entity<BodyCameraComponent> bodyCamera, ref GotEquippedEvent args)
    {
        //Construct the camera name using the players name and job (from ID card)
        //Use defaults if no ID card is found
        var userName = Loc.GetString(bodyCamera.Comp.UnknownUser);
        var userJob = Loc.GetString(bodyCamera.Comp.UnknownJob);

        if (_idCardSystem.TryFindIdCard(args.Equipee, out var card))
        {
            if (card.Comp.FullName != null)
                userName = card.Comp.FullName;
            if (card.Comp.JobTitle != null)
                userJob = card.Comp.JobTitle;
        }

        _camera.SetName(bodyCamera, $"{userJob} - {userName}");

        base.OnEquipped(bodyCamera, ref args);
    }

    /// <summary>
    /// Enable the camera and start drawing power
    /// </summary>
    protected override bool TryEnable(Entity<BodyCameraComponent> bodyCamera, EntityUid user)
    {
        if (!base.TryEnable(bodyCamera, user))
            return false;

        _camera.SetActive(bodyCamera, true);
        return true;
    }

    /// <summary>
    /// Disable the camera and stop drawing power
    /// </summary>
    protected override bool TryDisable(Entity<BodyCameraComponent> bodyCamera, EntityUid user)
    {
        if (!base.TryDisable(bodyCamera, user))
            return false;

        _camera.SetActive(bodyCamera, false);
        return true;
    }

}
