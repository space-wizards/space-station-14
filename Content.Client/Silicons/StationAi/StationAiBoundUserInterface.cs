using Content.Shared.Silicons.StationAi;
using Robust.Client.UserInterface;

namespace Content.Client.Silicons.StationAi;

public sealed class StationAiBoundUserInterface : BoundUserInterface
{
    private StationAiMenu? _menu;

    public StationAiBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();
        _menu = this.CreateWindow<StationAiMenu>();
        _menu.Track(Owner);

        _menu.OnAiRadial += args =>
        {
            SendPredictedMessage(new StationAiRadialMessage()
            {
                Event = args,
            });
        };
    }
}
