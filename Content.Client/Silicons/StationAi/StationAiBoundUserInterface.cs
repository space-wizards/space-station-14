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

    private IEnumerable<RadialMenuActionOption> ConvertToButtons(IReadOnlyList<StationAiRadial> actions)
    {
        var models = new RadialMenuActionOption[actions.Count];
        for (int i = 0; i < actions.Count; i++)
        {
            var action = actions[i];
            models[i] = new RadialMenuActionOption<BaseStationAiAction>(HandleRadialMenuClick, action.Event)
            {
                Sprite = action.Sprite,
                ToolTip = action.Tooltip
            };
        }

        return models;
    }

    private void HandleRadialMenuClick(BaseStationAiAction p)
    {
        SendPredictedMessage(new StationAiRadialMessage { Event = p });
    }
}
