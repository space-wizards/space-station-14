using Content.Shared.Silicons.StationAi;
using Robust.Client.UserInterface;
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

        _menu = this.CreateWindow<StationAiCustomizationMenu>();
        _menu.SetOwner(Owner);

        _menu.SendStationAiCustomizationMessageAction += SendStationAiCustomizationMessage;
    }

    public void SendStationAiCustomizationMessage(ProtoId<StationAiCustomizationGroupPrototype> groupProtoId, ProtoId<StationAiCustomizationPrototype> customizationProtoId)
    {
        SendPredictedMessage(new StationAiCustomizationMessage(groupProtoId, customizationProtoId));
    }
}
