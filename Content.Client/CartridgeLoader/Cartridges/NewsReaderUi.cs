using Content.Client.UserInterface.Fragments;
using Content.Shared.CartridgeLoader.Cartridges;
using Content.Shared.CartridgeLoader;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface;

namespace Content.Client.CartridgeLoader.Cartridges;

public sealed class NewsReaderUi : UIFragment
{
    private NewsReaderUiFragment? _fragment;

    public override Control GetUIFragmentRoot()
    {
        return _fragment!;
    }

    public override void Setup(BoundUserInterface userInterface, EntityUid? fragmentOwner)
    {
        _fragment = new NewsReaderUiFragment();

        _fragment.OnNextButtonPressed += () =>
        {
            SendNewsReaderMessage(NewsReaderUiAction.Next, userInterface);
        };
        _fragment.OnPrevButtonPressed += () =>
        {
            SendNewsReaderMessage(NewsReaderUiAction.Prev, userInterface);
        };
        _fragment.OnNotificationSwithPressed += () =>
        {
            SendNewsReaderMessage(NewsReaderUiAction.NotificationSwith, userInterface);
        };
    }

    public override void UpdateState(BoundUserInterfaceState state)
    {
        if (state is NewsReaderBoundUserInterfaceState cast)
            _fragment?.UpdateState(cast.Article, cast.TargetNum, cast.TotalNum, cast.NotificationOn);
        else if (state is NewsReaderEmptyBoundUserInterfaceState empty)
            _fragment?.UpdateEmptyState(empty.NotificationOn);
    }

    private void SendNewsReaderMessage(NewsReaderUiAction action, BoundUserInterface userInterface)
    {
        var newsMessage = new NewsReaderUiMessageEvent(action);
        var message = new CartridgeUiMessage(newsMessage);
        userInterface.SendMessage(message);
    }
}
