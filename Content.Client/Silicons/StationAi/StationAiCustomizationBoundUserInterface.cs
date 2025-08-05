using Content.Shared.Silicons.StationAi;
using Robust.Shared.Prototypes;

namespace Content.Client.Silicons.StationAi;

public sealed class StationAiCustomizationBoundUserInterface : BoundUserInterface
{
    private StationAiCustomizationMenu? _menu;

    public StationAiCustomizationBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {

    }

    protected override void Open()
    {
        base.Open();

        _menu = new StationAiCustomizationMenu(Owner);
        _menu.OpenCentered();
        _menu.OnClose += Close;

        _menu.SendStationAiCustomizationMessageAction += SendStationAiCustomizationMessage;
    }

    public void SendStationAiCustomizationMessage(ProtoId<StationAiCustomizationGroupPrototype> groupProtoId, ProtoId<StationAiCustomizationPrototype> customizationProtoId)
    {
        SendPredictedMessage(new StationAiCustomizationMessage(groupProtoId, customizationProtoId));
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (!disposing)
            return;

        _menu?.Dispose();
    }
}
