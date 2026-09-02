using Content.Shared.Silicons.StationAi;
using Robust.Client.UserInterface;
using Robust.Shared.Prototypes;

namespace Content.Client.Silicons.StationAi;

/// <summary>
/// A BUI for customizing the station AI appearance. Wraps a <see cref="StationAiCustomizationMenu"/>.
/// </summary>
/// <seealso cref="StationAiCustomizationComponent"/>
public sealed class StationAiCustomizationBoundUserInterface(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    private StationAiCustomizationMenu? _menu;

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
