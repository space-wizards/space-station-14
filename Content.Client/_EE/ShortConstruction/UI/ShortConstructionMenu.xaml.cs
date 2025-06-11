using Content.Client.Construction;
using Content.Client.UserInterface.Controls;
using Content.Shared._EE.ShortConstruction;
using Content.Shared.Construction;
using Content.Shared.Construction.Prototypes;
using Content.Shared.NodeContainer;
using Content.Shared.Popups;
using Robust.Client.GameObjects;
using Robust.Client.Placement;
using Robust.Client.Player;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.XAML;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;
using System.Numerics;

namespace Content.Client._EE.ShortConstruction.UI;

//This was originally a PR for Einstein's Engines, submitted by Github user VMSolidus.
//https://github.com/Simple-Station/Einstein-Engines/pull/861
//It has been modified to work within the Imp Station 14 server fork by Honeyed_Lemons. (and mqole (hi!))

public sealed class ShortConstructionMenu : RadialMenu
{
    [Dependency] private readonly EntityManager _entManager = default!;
    [Dependency] private readonly IPlacementManager _placementManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IPrototypeManager _protoManager = default!;
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

            // imp: basically just checking that the target proto Exists as a valid entity
            // do not add button if it isnt real
            if (!_protoManager.TryIndex(protoId, out var proto) ||
                !_construction.TryGetRecipePrototype(proto.ID, out var targetProtoId) ||
                !_protoManager.TryIndex(targetProtoId, out var targetProto))
                continue;

            var button = new RadialMenuTextureButtonWithSector
            {
                ToolTip = Loc.GetString(targetProto.Name), // imp
                SetSize = new Vector2(48f, 48f)
            };

            var texture = new TextureRect
            {
                VerticalAlignment = VAlignment.Center,
                HorizontalAlignment = HAlignment.Center,
                Texture = _spriteSystem.Frame0(targetProto), // imp
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
