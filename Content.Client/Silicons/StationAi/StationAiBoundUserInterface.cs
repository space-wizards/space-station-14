using System.Linq;
using Content.Client.UserInterface.Controls;
using Content.Shared.Silicons.StationAi;
using Robust.Client.UserInterface;

namespace Content.Client.Silicons.StationAi;

public sealed class StationAiBoundUserInterface(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    private SimpleRadialMenu? _menu;

    protected override void Open()
    {
        base.Open();

        var ev = new GetStationAiRadialEvent();
        EntMan.EventBus.RaiseLocalEvent(Owner, ref ev);

        _menu = this.CreateWindow<SimpleRadialMenu>();
        _menu.Track(Owner);
        var buttonModels = ConvertToButtons(ev.Actions);
        _menu.SetButtons(buttonModels);
        
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
