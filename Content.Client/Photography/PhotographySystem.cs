using Content.Client.Outline;
using Content.Client.Paper.UI;
using Content.Shared.Interaction;
using Content.Shared.Photography;
using Robust.Client.GameObjects;
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
    [Dependency] private readonly SpriteSystem _spriteSystem = default!;
    private readonly Dictionary<EntityUid, OwnedTexture> _textureCache = new();

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<CameraComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<PhotographComponent, AfterAutoHandleStateEvent>(OnPhotoStateUpdated);
        SubscribeLocalEvent<PhotographComponent, ComponentRemove>(OnPhotoRemoved);
    }

    private void OnPhotoRemoved(EntityUid uid, PhotographComponent comp, ComponentRemove args)
    {
        if (_textureCache.Remove(uid, out var texture))
        {
            texture.Dispose();
        }
    }

    private void OnPhotoStateUpdated(EntityUid uid, PhotographComponent comp, ref AfterAutoHandleStateEvent args)
    {
        if (comp.RawData == null || comp.RawData.Length == 0 || !TryComp<SpriteComponent>(uid, out var sprite))
            return;

        if (!_textureCache.TryGetValue(uid, out var texture))
        {
            using var stream = new MemoryStream(comp.RawData);
            texture = _clyde.LoadTextureFromPNGStream(stream);
            _textureCache[uid] = texture;
        }

        if (_spriteSystem.LayerMapTryGet((uid, sprite), PaperVisualLayers.Writing, out var layer, false))
        {
            _spriteSystem.LayerSetVisible((uid, sprite), layer, true);
            _spriteSystem.LayerSetTexture((uid, sprite), layer, texture);

            var scaleX = 8f / texture.Width;
            var scaleY = 8f / texture.Height;
            _spriteSystem.LayerSetScale((uid, sprite), layer, new Vector2(scaleX, scaleY));

            var offsetX = 0f / 32f;
            var offsetY = 1f / 32f;
            _spriteSystem.LayerSetOffset((uid, sprite), layer, new Vector2(offsetX, offsetY));
        }
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

        float qualityScale = camera.ImageRes;

        int renderSize = (int)(boxSizePixels * qualityScale);

        // Створюємо в'юпорт меншого розміру
        var cameraViewport = _clyde.CreateViewport(new Vector2i(renderSize, renderSize), "CameraLens");

        cameraViewport.Eye = new Robust.Shared.Graphics.Eye
        {
            Position = playerEye.Position,
            Offset = mapCoords.Position - playerEye.Position.Position,
            // Головна магія: ми "віддаляємо" камеру пропорційно втраті пікселів, 
            // щоб захопити ту саму площу ігрового світу.
            Zoom = new Vector2(1f / qualityScale, 1f / qualityScale),
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

            // Зберігаємо як PNG (ImageSharp автоматично стисне цей маленький файл)
            worldImage.SaveAsPng(ms);
            var photoBytes = ms.ToArray();

            var ev = new CameraPhotoCapturedEvent(GetNetEntity(cameraUid), photoBytes);
            RaiseNetworkEvent(ev);
        });
    }
}
