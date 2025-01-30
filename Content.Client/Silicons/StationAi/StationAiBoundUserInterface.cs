using System.Linq;
using Content.Client.UserInterface.Controls;
using Content.Shared.Silicons.StationAi;
using Robust.Client.UserInterface;

namespace Content.Client.Silicons.StationAi;

public sealed class StationAiBoundUserInterface(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    private RadialMenu? _menu;

    protected override void Open()
    {
        base.Open();

        this.CreateWindow<SimpleRadialMenu>();

        var ev = new GetStationAiRadialEvent();
        EntMan.EventBus.RaiseLocalEvent(Owner, ref ev);
        var buttonModels = ConvertToButtons(ev.Actions);

        _menu = new SimpleRadialMenu(buttonModels, Owner);
        _menu.Open();
    }

    private IEnumerable<RadialMenuActionOption> ConvertToButtons(IEnumerable<StationAiRadial> actions)
    {
        return actions.Select(
            x => new RadialMenuActionOption<StationAiRadial>(HandleRadialMenuClick, x)
            {
                Sprite = x.Sprite,
                ToolTip = x.Tooltip
            }
        );
    }

    private void HandleRadialMenuClick(StationAiRadial p)
    {
        SendPredictedMessage(new StationAiRadialMessage { Event = p });
    }
}
