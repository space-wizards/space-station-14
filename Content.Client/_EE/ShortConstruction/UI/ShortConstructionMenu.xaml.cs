using System.Numerics;
using Content.Client.Construction;
using Content.Client.UserInterface.Controls;
using Content.Shared._EE.ShortConstruction;
using Content.Shared.Construction.Prototypes;
using Content.Shared.Popups;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.Placement;
using Robust.Client.Player;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.XAML;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;

namespace Content.Client._EE.ShortConstruction.UI;

//This was originally a PR for Einstein's Engines, submitted by Github user VMSolidus.
//https://github.com/Simple-Station/Einstein-Engines/pull/861
//It has been modified to work within the Imp Station 14 server fork by Honeyed_Lemons.

public sealed class ShortConstructionMenu : RadialMenu
{
    [Dependency] private readonly IClyde _displayManager = default!;
    [Dependency] private readonly IInputManager _inputManager = default!;
    [Dependency] private readonly EntityManager _entManager = default!;
    [Dependency] private readonly IPrototypeManager _protoManager = default!;
    [Dependency] private readonly IPlacementManager _placementManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    private readonly ConstructionSystem _construction;

    private readonly SpriteSystem _spriteSystem;
    private readonly SharedPopupSystem _popup;

    private EntityUid _owner;

    public ShortConstructionMenu(EntityUid owner, ShortConstructionMenuBUI bui, ConstructionSystem construction)
    {
        IoCManager.InjectDependencies(this);
        RobustXamlLoader.Load(this);

        _spriteSystem = _entManager.System<SpriteSystem>();
        _popup = _entManager.System<SharedPopupSystem>();

        _owner = owner;
        _construction = construction;

        // Find the main radial container
        var main = FindControl<RadialContainer>("Main");

        if (!_entManager.TryGetComponent<ShortConstructionComponent>(owner, out var crafting))
            return;
        foreach (var protoId in crafting.Prototypes)
        {
            if (_playerManager.LocalSession == null)
                return;
            if (!_protoManager.TryIndex(protoId, out var proto))
                continue;

            var button = new RadialMenuTextureButtonWithSector
            {
                ToolTip = Loc.GetString(proto.Name),
                SetSize = new Vector2(48f, 48f)
            };

            var texture = new TextureRect
            {
                VerticalAlignment = Control.VAlignment.Center,
                HorizontalAlignment = Control.HAlignment.Center,
                Texture = _spriteSystem.Frame0(proto.Icon),
                TextureScale = new Vector2(1.5f, 1.5f)
            };

            button.AddChild(texture);

            main.AddChild(button);

            button.OnButtonUp += _ =>
            {
                Close();
                ConstructItem(proto);
            };
        }

    }
    /// <summary>
    /// Makes an item or places a schematic based on the type of construction recipe.
    /// </summary>
    private void ConstructItem(ConstructionPrototype prototype)
    {
        if (prototype.Type == ConstructionType.Item)
        {
            _construction.TryStartItemConstruction(prototype.ID);
            return;
        }

        _placementManager.BeginPlacing(new PlacementInformation
            {
                IsTile = false,
                PlacementOption = prototype.PlacementMode
            },
            new ConstructionPlacementHijack(_construction, prototype));

        // Should only close the menu if we're placing a construction hijack.
        Close();
    }
}
