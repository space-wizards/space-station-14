using System.Linq;
using Content.Client.Popups;
using Content.Client.UserInterface.Controls;
using Content.Shared.RCD;
using Content.Shared.RCD.Components;
using JetBrains.Annotations;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Client.RCD;

[UsedImplicitly]
public sealed class RCDMenuBoundUserInterface : BoundUserInterface
{
    [Dependency] private readonly IClyde _displayManager = default!;
    [Dependency] private readonly IInputManager _inputManager = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly ISharedPlayerManager _playerManager = default!;

    private RadialMenu? _menu;

    private static readonly Dictionary<string, (string Tooltip, SpriteSpecifier Sprite)> PrototypesGroupingInfo
        = new Dictionary<string, (string Tooltip, SpriteSpecifier Sprite)>
    {
        ["WallsAndFlooring"] = ("rcd-component-walls-and-flooring", new SpriteSpecifier.Texture(new ResPath("/Textures/Interface/Radial/RCD/walls_and_flooring.png"))),
        ["WindowsAndGrilles"] = ("rcd-component-windows-and-grilles", new SpriteSpecifier.Texture(new ResPath("/Textures/Interface/Radial/RCD/windows_and_grilles.png"))),
        ["Airlocks"] = ("rcd-component-airlocks", new SpriteSpecifier.Texture(new ResPath("/Textures/Interface/Radial/RCD/airlocks.png"))),
        ["Electrical"] = ("rcd-component-electrical", new SpriteSpecifier.Texture(new ResPath("/Textures/Interface/Radial/RCD/multicoil.png"))),
        ["Lighting"] = ("rcd-component-lighting", new SpriteSpecifier.Texture(new ResPath("/Textures/Interface/Radial/RCD/lighting.png"))),
    };

    public RCDMenuBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
        IoCManager.InjectDependencies(this);
    }

    protected override void Open()
    {
        base.Open();

        if (!EntMan.TryGetComponent<RCDComponent>(Owner, out var rcd))
            return;

        var models = ConvertToButtons(rcd.AvailablePrototypes);

        _menu = new SimpleRadialMenu(models, Owner);

        // Open the menu, centered on the mouse
        var vpSize = _displayManager.ScreenSize;
        _menu.OpenCenteredAt(_inputManager.MouseScreenPosition.Position / vpSize);
    }

    private IEnumerable<RadialMenuNestedLayerOption> ConvertToButtons(HashSet<ProtoId<RCDPrototype>> prototypes)
    {
        var models = prototypes.Select(x => _prototypeManager.Index(x))
                        .GroupBy(x => x.Category)
                        .Where(x => PrototypesGroupingInfo.ContainsKey(x.Key))
                        .Select(x =>
                        {
                            var nested = x.Select(proto => new RadialMenuActionOption(() =>
                            {
                                // A predicted message cannot be used here as the RCD UI is closed immediately
                                // after this message is sent, which will stop the server from receiving it
                                SendMessage(new RCDSystemMessage(proto.ID));

                                var popup = EntMan.System<PopupSystem>();

                                if (_playerManager.LocalSession?.AttachedEntity != null)
                                {
                                    var msg = Loc.GetString("rcd-component-change-mode", ("mode", Loc.GetString(proto.SetName)));

                                    if (proto.Mode == RcdMode.ConstructTile || proto.Mode == RcdMode.ConstructObject)
                                    {
                                        var name = Loc.GetString(proto.SetName);

                                        if (proto.Prototype != null &&
                                            _prototypeManager.TryIndex(proto.Prototype, out var entProto, logError: false))
                                            name = entProto.Name;

                                        msg = Loc.GetString("rcd-component-change-build-mode", ("name", name));
                                    }

                                    // Popup message
                                    popup.PopupClient(msg, Owner, _playerManager.LocalSession.AttachedEntity);
                                }
                            })
                            {
                                Sprite = proto.Sprite,
                                ToolTip = GetTooltip(proto)
                            }).ToArray();

                            var tuple = PrototypesGroupingInfo[x.Key];
                            return new RadialMenuNestedLayerOption(nested)
                            {
                                Sprite = tuple.Sprite,
                                ToolTip = Loc.GetString(tuple.Tooltip)
                            };
                        });
        return models;
    }

    private string GetTooltip(RCDPrototype proto)
    {
        var tooltip = Loc.GetString(proto.SetName);

        if ((proto.Mode == RcdMode.ConstructTile || proto.Mode == RcdMode.ConstructObject) &&
            proto.Prototype != null && _prototypeManager.TryIndex(proto.Prototype, out var entProto, logError: false))
        {
            tooltip = Loc.GetString(entProto.Name);
        }

        return tooltip;
    }
}
