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
using System.Text;

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

            if (worldImage == null)
                return;

            string generatedRichText = ProcessImageToRichText(
                worldImage,
                cropX: 0,
                cropY: 0,
                cropWidth: boxSizePixels,
                cropHeight: boxSizePixels,
                targetWidth: camera.TargetWidth * dpi,
                fontSize: camera.ImageSize
            );

            var ev = new CameraPhotoCapturedEvent(GetNetEntity(cameraUid), generatedRichText);
            RaiseNetworkEvent(ev);
        });
    }

    private string ProcessImageToRichText(Image<Rgba32> image, int cropX, int cropY, int cropWidth, int cropHeight, int targetWidth, float fontSize)
    {
        using var ms = new MemoryStream();
        image.SaveAsBmp(ms);
        byte[] bmpBytes = ms.ToArray();

        int dataOffset = BitConverter.ToInt32(bmpBytes, 10);
        int imgWidth = BitConverter.ToInt32(bmpBytes, 18);
        int imgHeight = Math.Abs(BitConverter.ToInt32(bmpBytes, 22));
        short bpp = BitConverter.ToInt16(bmpBytes, 28);

        int bytesPerPixel = bpp / 8;
        int rowStride = ((imgWidth * bytesPerPixel) + 3) & ~3;

        cropX = Math.Clamp(cropX, 0, imgWidth - 1);
        cropY = Math.Clamp(cropY, 0, imgHeight - 1);
        cropWidth = Math.Clamp(cropWidth, 1, imgWidth - cropX);
        cropHeight = Math.Clamp(cropHeight, 1, imgHeight - cropY);

        int targetHeight = (int)(cropHeight * ((float)targetWidth / cropWidth));

        var sb = new StringBuilder();
        sb.AppendLine($"[font=\"Picture\" size={fontSize}]");

        for (int y = 0; y < targetHeight; y++)
        {
            int srcY = cropY + (y * cropHeight / targetHeight);
            int bmpY = imgHeight - 1 - srcY;

            int count = 0;
            string currentHex = "";

            for (int x = 0; x < targetWidth; x++)
            {
                int srcX = cropX + (x * cropWidth / targetWidth);
                int pixelIndex = dataOffset + (bmpY * rowStride) + (srcX * bytesPerPixel);

                byte b = bmpBytes[pixelIndex];
                byte g = bmpBytes[pixelIndex + 1];
                byte r = bmpBytes[pixelIndex + 2];

                string hexColor = $"{r:X2}{g:X2}{b:X2}";

                if (x == 0)
                {
                    currentHex = hexColor;
                    count = 1;
                }
                else if (hexColor == currentHex)
                {
                    count++;
                }
                else
                {
                    AppendColorBlock(sb, currentHex, count);
                    currentHex = hexColor;
                    count = 1;
                }
            }
            AppendColorBlock(sb, currentHex, count);
            sb.AppendLine();
        }

        sb.AppendLine("[/font]");
        return sb.ToString();
    }

    private void AppendColorBlock(StringBuilder sb, string hexColor, int count)
    {
        sb.Append($"[color=#{hexColor}]");
        sb.Append('0', count);
        sb.Append("[/color]");
    }
}
