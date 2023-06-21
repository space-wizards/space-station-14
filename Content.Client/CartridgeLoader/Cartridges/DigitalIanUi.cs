using Content.Client.UserInterface.Fragments;
using Content.Shared.CartridgeLoader.Cartridges;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface;
using Content.Shared.CartridgeLoader;

using Content.Shared.Containers.ItemSlots;
using Robust.Shared.Prototypes;
namespace Content.Client.CartridgeLoader.Cartridges;

public sealed class DigitalIanUi : UIFragment
{
    private DigitalIanUiFragment? _fragment;

    public override Control GetUIFragmentRoot()
    {
        return _fragment!;

    }

    public override void Setup(BoundUserInterface userInterface, EntityUid? fragmentOwner)
    {
        _fragment = new DigitalIanUiFragment();
        _fragment.OnFeedButtonPressed += () => SendDigitalIanMessage(DigitalIanUiAction.Feed, userInterface);
        _fragment.OnPetButtonPressed += () => SendDigitalIanMessage(DigitalIanUiAction.Pet, userInterface);
    }

    public override void UpdateState(BoundUserInterfaceState state)
    {
        // No state to update

    }
    private void SendDigitalIanMessage(DigitalIanUiAction action, BoundUserInterface userInterface)
    {
        var digitalIanMessage = new DigitalIanUiMessageEvent(action);
        var message = new CartridgeUiMessage(digitalIanMessage);
        userInterface.SendMessage(message);
    }
}
