using JetBrains.Annotations;
using Robust.Client.GameObjects;

namespace Content.Client.Clothing.UI;

public sealed class ChameleonBoundUserInterface : BoundUserInterface
{
    public ChameleonBoundUserInterface([NotNull] ClientUserInterfaceComponent owner, [NotNull] object uiKey) : base(owner, uiKey)
    {
    }

    private ChameleonMenu? _menu;

    protected override void Open()
    {
        base.Open();
        _menu = new ChameleonMenu(new []{"ClothingUniformJumpskirtResearchDirector"});
        _menu.OnClose += Close;
        _menu.OpenCentered();
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing)
        {
            _menu?.Close();
            _menu = null;
        }
    }
}
