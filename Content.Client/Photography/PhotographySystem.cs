using Content.Client.Outline;
using Content.Shared.Interaction;
using Content.Shared.Photography;
using Robust.Client.Graphics;
using Robust.Shared.Map;
using Robust.Shared.Timing;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System.IO;
using System.Numerics;

namespace Content.Client.Photography;

public sealed class PhotographySystem : EntitySystem
{
    [Dependency] private readonly IEyeManager _eyeManager = default!;
    [Dependency] private readonly SharedTransformSystem _transformSystem = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly InteractionOutlineSystem _interactionOutlineSystem = default!;
    [Dependency] private readonly IClyde _clyde = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<CameraComponent, AfterInteractEvent>(OnAfterInteract);
    }

    private void OnAfterInteract(Entity<CameraComponent> ent, ref AfterInteractEvent args)
    {
        if (args.Handled)
            return;
        args.Handled = true;

        if (!_timing.IsFirstTimePredicted || !args.ClickLocation.IsValid(EntityManager))
            return;

        CaptureWorldImage(ent.Owner, args.ClickLocation, ent.Comp);
    }

    private void CaptureWorldImage(EntityUid cameraUid, EntityCoordinates targetCoords, CameraComponent camera)
    {
        var mapCoords = _transformSystem.ToMapCoordinates(targetCoords);
        var playerEye = _eyeManager.CurrentEye;

        var dpi = 32;

        int boxSizePixels = camera.TargetWidth * dpi;

        var cameraViewport = _clyde.CreateViewport(new Vector2i(boxSizePixels, boxSizePixels), "CameraLens");

        cameraViewport.Eye = new Robust.Shared.Graphics.Eye
        {
            Position = playerEye.Position,
            Offset = mapCoords.Position - playerEye.Position.Position,
            Zoom = new Vector2(1f, 1f),
            Rotation = playerEye.Rotation
        };

        _interactionOutlineSystem.SetEnabled(false);

        cameraViewport.Render();

        _interactionOutlineSystem.SetEnabled(true);

        cameraViewport.RenderTarget.CopyPixelsToMemory<Rgba32>(worldImage =>
        {
            cameraViewport.Dispose();
            if (worldImage == null) return;

            using var ms = new MemoryStream();
            worldImage.SaveAsBmp(ms);
            var photoBytes = ms.ToArray();

            var fontSize = camera.ImageSize;

            var ev = new CameraPhotoCapturedEvent(GetNetEntity(cameraUid), photoBytes, fontSize);
            RaiseNetworkEvent(ev);
        });
    }
}
