using Content.Client.Construction;
using Content.Client.UserInterface.Controls;
using Content.Shared.Construction.Prototypes;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.Placement;
using Robust.Shared.Enums;

//This was originally a PR for Einstein's Engines, submitted by Github user VMSolidus.
//https://github.com/Simple-Station/Einstein-Engines/pull/861
//It has been modified to work within the Imp Station 14 server fork by Honeyed_Lemons.

// ReSharper disable InconsistentNaming

namespace Content.Client._EE.ShortConstruction.UI;

[UsedImplicitly]
public sealed class ShortConstructionMenuBUI : BoundUserInterface
{
    [Dependency] private readonly IClyde _displayManager = default!;
    [Dependency] private readonly IInputManager _inputManager = default!;
    [Dependency] private readonly EntityManager _entManager = default!;

    private readonly ConstructionSystem _construction;

    private ShortConstructionMenu? _menu;

    public ShortConstructionMenuBUI(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
        _construction = _entManager.System<ConstructionSystem>();
        _entManager.System<SpriteSystem>();
    }


    protected override void Open()
    {
        base.Open();

        _menu = new ShortConstructionMenu(Owner, this,_construction);
        _menu.OnClose += Close;

        // Open the menu, centered on the mouse
        var vpSize = _displayManager.ScreenSize;
        _menu.OpenCenteredAt(_inputManager.MouseScreenPosition.Position / vpSize);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (!disposing) return;

        _menu?.Dispose();
    }
}
