using Content.Client.UserInterface.Fragments;
using Content.Shared.CartridgeLoader.Cartridges;
using Content.Shared.CartridgeLoader;
using Robust.Client.UserInterface;

namespace Content.Client.CartridgeLoader.Cartridges;

public sealed partial class NewsReaderUi : UIFragment
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
            SendNewsReaderMessage(NewsReaderUiAction.NotificationSwitch, userInterface);
        };
        _fragment.OnReactionPressed += reaction =>
        {
            SendCartridgeMessage(new NewsReaderReactionMessageEvent(reaction), userInterface);
        };
        _fragment.OnCommentSubmitted += comment =>
        {
            SendCartridgeMessage(new NewsReaderCommentMessageEvent(comment), userInterface);
        };
    }

    public override void UpdateState(BoundUserInterfaceState state)
    {
        switch (state)
        {
            case NewsReaderBoundUserInterfaceState cast:
                _fragment?.UpdateState(cast.Article, cast.TargetNum, cast.TotalNum, cast.NotificationOn, cast.ActiveReactions);
                break;
            case NewsReaderEmptyBoundUserInterfaceState empty:
                _fragment?.UpdateEmptyState(empty.NotificationOn);
                break;
        }
    }

    private static void SendNewsReaderMessage(NewsReaderUiAction action, BoundUserInterface userInterface)
    {
        SendCartridgeMessage(new NewsReaderUiMessageEvent(action), userInterface);
    }

    private static void SendCartridgeMessage(CartridgeMessageEvent messageEvent, BoundUserInterface userInterface)
    {
        var message = new CartridgeUiMessage(messageEvent);
        userInterface.SendMessage(message);
    }
}


