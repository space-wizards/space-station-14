using Content.Client.Clothing.Systems;
using Content.Shared.Inventory;
using JetBrains.Annotations;
using Robust.Client.GameObjects;

namespace Content.Client.Clothing.UI;

[UsedImplicitly]
public sealed class ChameleonBoundUserInterface : BoundUserInterface
{
    private ChameleonMenu? _menu;

    public ChameleonBoundUserInterface(ClientUserInterfaceComponent owner, object uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        var targets = EntitySystem.Get<ChameleonClothingSystem>().GetValidItems(SlotFlags.INNERCLOTHING);

        _menu = new ChameleonMenu(targets);
        _menu.OnClose += Close;
        _menu.OnIdSelected += OnIdSelected;
        _menu.OpenCentered();
    }

    private void OnIdSelected(string selectedId)
    {
        Logger.Debug(selectedId);
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
