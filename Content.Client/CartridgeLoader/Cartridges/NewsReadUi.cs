using Content.Client.UserInterface.Fragments;
using Content.Shared.CartridgeLoader.Cartridges;
using Content.Shared.CartridgeLoader;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface;

namespace Content.Client.CartridgeLoader.Cartridges;

public sealed partial class NewsReadUi : UIFragment
{
    private NewsReadUiFragment? _fragment;

    public override Control GetUIFragmentRoot()
    {
        return _fragment!;
    }

    public override void Setup(BoundUserInterface userInterface, EntityUid? fragmentOwner)
    {
        _fragment = new NewsReadUiFragment();

        _fragment.OnNextButtonPressed += () =>
        {
            SendNewsReadMessage(NewsReadUiAction.Next, userInterface);
        };
        _fragment.OnPrevButtonPressed += () =>
        {
            SendNewsReadMessage(NewsReadUiAction.Prev, userInterface);
        };
        _fragment.OnNotificationSwithPressed += () =>
        {
            SendNewsReadMessage(NewsReadUiAction.NotificationSwith, userInterface);
        };
    }

    public override void UpdateState(BoundUserInterfaceState state)
    {
        if (state is NewsReadBoundUserInterfaceState cast)
            _fragment?.UpdateState(cast.Article, cast.TargetNum, cast.TotalNum, cast.NotificationOn);
        else if (state is NewsReadEmptyBoundUserInterfaceState empty)
            _fragment?.UpdateEmptyState(empty.NotificationOn);
    }

    private void SendNewsReadMessage(NewsReadUiAction action, BoundUserInterface userInterface)
    {
        var newsMessage = new NewsReadUiMessageEvent(action);
        var message = new CartridgeUiMessage(newsMessage);
        userInterface.SendMessage(message);
    }
}
