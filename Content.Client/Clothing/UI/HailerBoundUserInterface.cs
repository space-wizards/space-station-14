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
    private static readonly Color SelectedOptionBackground = StyleNano.ButtonColorDefaultRed.WithAlpha(128);
    private static readonly Color SelectedOptionHoverBackground = StyleNano.ButtonColorHoveredRed.WithAlpha(128);

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

            var orderIndex = i;
            var button = new RadialMenuActionOption<int>(DoSomething, orderIndex)
            {
                IconSpecifier = RadialMenuIconSpecifier.With(line.Icon),
                ToolTip = tooltip,
                BackgroundColor = SelectedOptionBackground,
                HoverBackgroundColor = SelectedOptionHoverBackground
            };

            list.Add(button);
        }


        return list;
    }

    private void DoSomething(int index)
    {
        SendPredictedMessage(new HailerOrderMessage(index));
        Close();
    }
}
