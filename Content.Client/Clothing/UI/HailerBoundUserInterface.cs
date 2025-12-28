using Content.Client.Stylesheets;
using Content.Client.UserInterface.Controls;
using Content.Shared.Clothing.Components;
using Robust.Client.Player;
using Robust.Client.UserInterface;

namespace Content.Client.Clothing.UI;

public sealed class HailerBoundUserInterface : BoundUserInterface
{
    [Dependency] private readonly IPlayerManager _player = default!;

    private SimpleRadialMenu? _hailerRadioMenu;

    public HailerBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _hailerRadioMenu = this.CreateWindow<SimpleRadialMenu>();

        Update();

        _hailerRadioMenu.OpenCentered();
    }

    public override void Update()
    {
        if (_hailerRadioMenu == null)
            return;

        if (!EntMan.TryGetComponent<HailerComponent>(Owner, out var hailerComp))
            return;

        if (_player.LocalSession?.AttachedEntity is not { } user)
            return;

        //Convert hailer orders set in the yaml to buttons for the radialMenu
        var list = ConvertToButtons(hailerComp.Orders);
        _hailerRadioMenu.SetButtons(list);
    }

    private IEnumerable<RadialMenuOptionBase> ConvertToButtons(List<HailOrder> orders)
    {
        List<RadialMenuOptionBase> list = new();

        //For each order, add a button
        for (var i = 0; i < orders.Count; i++)
        {
            var line = orders[i];
            var tooltip = line.Description;

            //Index of the order in the hailer orders List
            var orderIndex = i;
            var button = new RadialMenuActionOption<int>(DoSomething, orderIndex)
            {
                IconSpecifier = RadialMenuIconSpecifier.With(line.Icon),
                ToolTip = tooltip
            };

            list.Add(button);
        }


        return list;
    }

    private void DoSomething(int index)
    {
        //Send message for HailerSystem to catch
        SendPredictedMessage(new HailerOrderMessage(index));

        //Close BUI
        Close();
    }
}
