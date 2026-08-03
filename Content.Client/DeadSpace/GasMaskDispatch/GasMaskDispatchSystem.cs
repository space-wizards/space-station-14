// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Client.UserInterface.Controls;
using Content.Shared.DeadSpace.GasMaskDispatch.Components;
using Robust.Shared.Utility;

namespace Content.Client.DeadSpace.GasMaskDispatch;

public sealed class GasMaskDispatchSystem : EntitySystem
{
    private const string MenuIconsRsi = "/Textures/_DeadSpace/Interface/Radial/GasMaskDispatch.rsi";

    private static readonly Dictionary<GasMaskDispatchCode, SpriteSpecifier> MenuIcons = new()
    {
        [GasMaskDispatchCode.Code0] = new SpriteSpecifier.Rsi(new ResPath(MenuIconsRsi), "code-0"),
        [GasMaskDispatchCode.Code1] = new SpriteSpecifier.Rsi(new ResPath(MenuIconsRsi), "code-1"),
        [GasMaskDispatchCode.Code2] = new SpriteSpecifier.Rsi(new ResPath(MenuIconsRsi), "code-2"),
        [GasMaskDispatchCode.Code3] = new SpriteSpecifier.Rsi(new ResPath(MenuIconsRsi), "code-3"),
    };

    private SimpleRadialMenu? _menu;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GasMaskDispatchComponent, OpenGasMaskDispatchMenuEvent>(OnOpenMenu);
    }

    private void OnOpenMenu(Entity<GasMaskDispatchComponent> ent, ref OpenGasMaskDispatchMenuEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;

        _menu?.Close();
        _menu = new SimpleRadialMenu();

        var mask = ent.Owner;
        var buttons = new List<RadialMenuOptionBase>
        {
            CreateOption(mask, GasMaskDispatchCode.Code0, "gas-mask-dispatch-menu-code-0"),
            CreateOption(mask, GasMaskDispatchCode.Code1, "gas-mask-dispatch-menu-code-1"),
            CreateOption(mask, GasMaskDispatchCode.Code2, "gas-mask-dispatch-menu-code-2"),
            CreateOption(mask, GasMaskDispatchCode.Code3, "gas-mask-dispatch-menu-code-3"),
        };

        _menu.SetButtons(buttons);
        _menu.OpenCentered();
    }

    private RadialMenuOptionBase CreateOption(EntityUid mask, GasMaskDispatchCode code, string tooltipLocId)
    {
        return new RadialMenuActionOption<GasMaskDispatchCode>(selected => SelectCode(mask, selected), code)
        {
            IconSpecifier = RadialMenuIconSpecifier.With(MenuIcons[code]),
            ToolTip = Loc.GetString(tooltipLocId),
        };
    }

    private void SelectCode(EntityUid mask, GasMaskDispatchCode code)
    {
        RaiseNetworkEvent(new GasMaskDispatchSelectMessage(GetNetEntity(mask), code));
        _menu?.Close();
    }
}
