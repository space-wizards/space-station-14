using Content.Shared.ColorShift;
using Content.Shared.Humanoid;
using JetBrains.Annotations;

namespace Content.Client.ColorShift;

[UsedImplicitly]
public sealed class ColorShiftBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private ColorShiftWindow? _window;

    [Dependency] private readonly IEntityManager _entManager = default!;

    public ColorShiftBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
        IoCManager.InjectDependencies(this);
    }

    protected override void Open()
    {
        base.Open();

        if (_window == null)
        {
            _window = new ColorShiftWindow();
            _window.OnClose += Close;
            _window.OnHueShift += WindowOnOnHueShift;
            _window.OpenCentered();
        }
        else
        {
            _window.Open();
        }
        Reload();
    }

    private void WindowOnOnHueShift(Color color)
    {
        var hsv = Color.ToHsv(color);
        var message = new PleaseHueShiftNetworkMessage(hsv.X, hsv.Y, hsv.Z);
        SendMessage(message);

        Close();
    }

    public void Reload()
    {
        if (_window == null || !_entManager.TryGetComponent(Owner, out HumanoidAppearanceComponent? component))
            return;

        _window.SetCurrentColor(component.SkinColor);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (!disposing)
            return;

        if (_window == null)
            return;
        _window.Orphan();
        _window.OnClose -= Close;
        _window = null;
    }
}
