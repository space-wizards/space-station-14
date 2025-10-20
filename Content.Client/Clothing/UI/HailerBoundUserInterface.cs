
using Content.Client.Administration.UI.Tabs.PlayerTab;
using Content.Client.Clothing.Systems;
using Content.Client.UserInterface.Controls;
using Content.Shared.Changeling.Components;
using Content.Shared.Clothing.Components;
using Content.Shared.Clothing.EntitySystems;
using Content.Shared.IdentityManagement;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Elfie.Serialization;
using Robust.Client.GameObjects;
using Robust.Client.Player;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using System.Numerics;
using static Robust.Client.UserInterface.Control;

namespace Content.Client.Clothing.UI;

public sealed class HailerBoundUserInterface : BoundUserInterface
{
    [Dependency] private readonly IPlayerManager _player = default!;
    private readonly HailerSystem _hailer = default!;
    private readonly SpriteSystem _sprite = default!;

    private HailerRadialMenu? _menu;
    private SimpleRadialMenu? _hailerRadioMenu;

    public HailerBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
        _hailer = EntMan.System<HailerSystem>();
        _sprite = EntMan.System<SpriteSystem>();
    }

    protected override void Open()
    {
        base.Open();

        _hailerRadioMenu = this.CreateWindow<SimpleRadialMenu>();

        Update();

        _hailerRadioMenu.OpenCentered();

        //if (_menu == null)
        //{
        //    _menu = new(Owner, EntMan, _player, _hailer, _sprite);

        //    _menu.OnOrderPicked += index =>
        //    {
        //        SendPredictedMessage(new HailerOrderMessage(index));
        //        Close();
        //    };

        //    _menu.OnClose += () => Close();
        //}

        //_menu.OpenCentered();
    }

    public override void Update()
    {
        if (_hailerRadioMenu == null)
            return;

        if (!EntMan.TryGetComponent<HailerComponent>(Owner, out var hailerComp))
            return;

        if (_player.LocalSession?.AttachedEntity is not { } user)
            return;

        var list = ConvertToButtons(hailerComp.Orders);
        _hailerRadioMenu.SetButtons(list);
    }

    private IEnumerable<RadialMenuOptionBase> ConvertToButtons(List<HailOrder> orders)
    {
        List<RadialMenuOptionBase> list = new();

        for (var i = 0; i < orders.Count; i++)
        {
            var line = orders[i];
            var tooltip = line.Description;
            //var button = new RadialMenuButton()
            //{
            //    StyleClasses = { "RadialMenuButton" },
            //    SetSize = new Vector2(64f, 64f),
            //    ToolTip = tooltip
            //};
            //if (line.Icon != null)
            //{
            //    var tex = new TextureRect()
            //    {
            //        VerticalAlignment = VAlignment.Center,
            //        HorizontalAlignment = HAlignment.Center,
            //        Texture = sprite.Frame0(line.Icon),
            //        TextureScale = new Vector2(2f, 2f),
            //    };

            //    button.AddChild(tex);
            //}
            //button.OnButtonUp += _ => OnOrderPicked?.Invoke(orderIndex);

            var orderIndex = i;
            var button = new RadialMenuActionOption<int>(DoSomething, orderIndex)
            {
                IconSpecifier = RadialMenuIconSpecifier.With(line.Icon),
                ToolTip = tooltip,
                BackgroundColor = Color.Red,
                HoverBackgroundColor = Color.Blue
            };

            list.Add(button);
        }


        return list;
    }

    private void DoSomething(int obj)
    {
    }
}
