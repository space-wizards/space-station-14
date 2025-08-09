using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface;

namespace Content.Client.BlueArtilery.UI;

[UsedImplicitly]
public sealed class BluespaceArtileryConsoleBoundUserInterfaces : BoundUserInterface
{
    [ViewVariables]
    private BluespaceArtileryConsoleWindow? _window;

    public BluespaceArtileryConsoleBoundUserInterfaces(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<BluespaceArtileryConsoleWindow>();
    }
    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            _window?.Close();
            _window = null;
        }
    }
}
