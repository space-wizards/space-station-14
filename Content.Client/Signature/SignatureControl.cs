using System.Numerics;
using Content.Shared.Signature;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Shared.Input;

namespace Content.Client.Signature;

public sealed partial class SignatureControl : Control
{
    [Dependency] private IClyde _clyde = default!;

    public Vector2 CanvasSize { get; set; }

    public SignatureData? Data;

    public int BrushWriteSize = 1;
    public int BrushEraseSize = 2;

    public bool Editable { get; set; } = true;

    public Color BackgroundColor { get; set; } = Color.FromHex("#ffffff88");

    public Color BorderColor { get; set; } = Color.Black.WithAlpha(0.4f);

    public event Action<SignatureData?>? SignatureChanged;

    private SignatureDrawMode _currentMode = SignatureDrawMode.Write;
    private bool _isDrawing;

    private (int x, int y)? _lastPixel;

    private IRenderTexture? _canvas;
    private bool _dirty;

    public SignatureControl()
    {
        IoCManager.InjectDependencies(this);
    }

    protected override void Resized()
    {
        base.Resized();

        if (CanvasSize == Vector2.Zero)
            return;

        _dirty = true;
        _canvas?.Dispose();
        _canvas = null;
    }

    public void SetSignature(SignatureData? data)
    {
        if (CanvasSize == Vector2.Zero)
        {
            Data = data ?? new SignatureData(1, 1);
            return;
        }

        Data = data == null
            ? new SignatureData((int)CanvasSize.X, (int)CanvasSize.Y)
            : EnsureSize(data);

        _dirty = true;
        _canvas?.Dispose();
        _canvas = null;
    }

    private SignatureData EnsureSize(SignatureData original)
    {
        var w = (int)CanvasSize.X;
        var h = (int)CanvasSize.Y;

        if (original.Width == w && original.Height == h)
            return original.Clone();

        var clone = new SignatureData(w, h);
        original.CopyTo(clone);

        return clone;
    }

    protected override void KeyBindDown(GUIBoundKeyEventArgs args)
    {
        base.KeyBindDown(args);

        if (!Editable)
            return;

        Data ??= new SignatureData((int)CanvasSize.X, (int)CanvasSize.Y);

        if (args.Function == EngineKeyFunctions.UIClick)
            _currentMode = SignatureDrawMode.Write;
        else if (args.Function == EngineKeyFunctions.UIRightClick)
            _currentMode = SignatureDrawMode.Erase;
        else
            return;

        if (!TryLocalToPixel(args.RelativePixelPosition, out var px, out var py))
            return;

        _isDrawing = true;
        DrawPixel(px, py);
        _lastPixel = (px, py);
    }

    protected override void KeyBindUp(GUIBoundKeyEventArgs args)
    {
        base.KeyBindUp(args);

        if (!Editable ||
            args.Function != EngineKeyFunctions.UIClick &&
            args.Function != EngineKeyFunctions.UIRightClick)
            return;

        if (!_isDrawing)
            return;

        _isDrawing = false;
        _lastPixel = null;

        SignatureChanged?.Invoke(Data);
    }

    protected override void MouseMove(GUIMouseMoveEventArgs args)
    {
        base.MouseMove(args);

        if (!Editable || !_isDrawing)
            return;

        if (!TryLocalToPixel(args.RelativePixelPosition, out var px, out var py))
            return;

        if (_lastPixel is { } last)
            DrawLine(last.x, last.y, px, py);
        else
            DrawPixel(px, py);

        _lastPixel = (px, py);
    }

    private bool TryLocalToPixel(Vector2 local, out int px, out int py)
    {
        var size = PixelSize;
        if (size.X <= 0 || size.Y <= 0)
        {
            px = py = 0;
            return false;
        }

        var nx = Math.Clamp(local.X / size.X, 0f, 0.999999f);
        var ny = Math.Clamp(local.Y / size.Y, 0f, 0.999999f);

        px = (int)(nx * CanvasSize.X);
        py = (int)(ny * CanvasSize.Y);
        return true;
    }

    private void DrawPixel(int x, int y)
    {
        if (Data == null)
            return;

        var radius = _currentMode == SignatureDrawMode.Erase ? BrushEraseSize : BrushWriteSize;
        var half = radius / 2;

        var minX = Math.Max(0, x - half);
        var maxX = Math.Min(Data.Width  - 1, x + half);
        var minY = Math.Max(0, y - half);
        var maxY = Math.Min(Data.Height - 1, y + half);

        var write = _currentMode == SignatureDrawMode.Write;

        for (var py = minY; py <= maxY; py++)
        {
            for (var px = minX; px <= maxX; px++)
            {
                if (write)
                    Data.SetPixel(px, py);
                else
                    Data.ErasePixel(px, py);
            }
        }

        _dirty = true;
    }

    private void DrawLine(int x0, int y0, int x1, int y1)
    {
        var dx = Math.Abs(x1 - x0);
        var sx = x0 < x1 ? 1 : -1;
        var dy = -Math.Abs(y1 - y0);
        var sy = y0 < y1 ? 1 : -1;
        var err = dx + dy;

        while (true)
        {
            DrawPixel(x0, y0);

            if (x0 == x1 && y0 == y1)
                break;

            var e2 = err * 2;

            if (e2 >= dy)
            {
                err += dy;
                x0 += sx;
            }

            if (e2 <= dx)
            {
                err += dx;
                y0 += sy;
            }
        }
    }

    public void ClearSignature()
    {
        if (Data == null)
            return;

        Data.Clear();
        _dirty = true;
        SignatureChanged?.Invoke(Data);
        InvalidateMeasure();
    }

    private void UpdateCanvas(DrawingHandleScreen handle)
    {
        if (CanvasSize.X <= 0 || CanvasSize.Y <= 0)
            return;

        if (Data == null)
            return;

        if (_canvas == null)
        {
            _canvas = _clyde.CreateRenderTarget(
                new Vector2i((int)CanvasSize.X, (int)CanvasSize.Y),
                new RenderTargetFormatParameters(RenderTargetColorFormat.Rgba8Srgb),
                name: "signature");
            _dirty = true;
        }

        if (!_dirty)
            return;

        handle.RenderInRenderTarget(_canvas,
            () =>
            {
                // Run-length encoding method
                for (var y = 0; y < Data.Height; y++)
                {
                    var startX = -1;

                    for (var x = 0; x < Data.Width; x++)
                    {
                        var px = Data.GetPixel(x, y);

                        if (px)
                        {
                            if (startX == -1)
                                startX = x;
                        }
                        else if (startX != -1)
                        {
                            var rect = new UIBox2i(startX, y, x, y + 1);
                            handle.DrawRect(rect, Color.Black);
                            startX = -1;
                        }
                    }

                    if (startX != -1)
                    {
                        var rect = new UIBox2i(startX, y, Data.Width, y + 1);
                        handle.DrawRect(rect, Color.Black);
                    }
                }
            },
            Color.Transparent);

        _dirty = false;
    }

    protected override void Draw(DrawingHandleScreen handle)
    {
        base.Draw(handle);

        var size = PixelSize;
        var rect = new UIBox2(0, 0, size.X, size.Y);

        if (Editable)
        {
            handle.DrawRect(rect, BackgroundColor);
            handle.DrawRect(rect, BorderColor, false);
        }

        UpdateCanvas(handle);

        if (_canvas != null)
            handle.DrawTextureRect(_canvas.Texture, rect);
    }
}

public enum SignatureDrawMode
{
    Write,
    Erase,
}
