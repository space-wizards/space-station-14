using System.Linq;
using Content.Client.UserInterface.Controls;
using Content.Shared.Silicons.StationAi;

namespace Content.Client.Silicons.StationAi;

public sealed class StationAiBoundUserInterface : BoundUserInterface
{
    private RadialMenu? _menu;

    public StationAiBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        var ev = new GetStationAiRadialEvent();
        EntMan.EventBus.RaiseLocalEvent(Owner, ref ev);
        var buttonModels = ev.Actions.Select(x
            => new RadialMenuButtonModel(() => SendPredictedMessage(new StationAiRadialMessage { Event = x, }))
            {
                Sprite = x.Sprite,
                ToolTip = x.Tooltip
            });

        _menu = new SimpleRadialMenu( buttonModels, Owner);
        _menu.Open();
    }
}
