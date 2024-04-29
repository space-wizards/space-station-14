using Content.Shared.Tips;
using Robust.Client.UserInterface;

namespace Content.Client.Tips;

public sealed class TipsSystem : EntitySystem
{
    [Dependency] private readonly IUserInterfaceManager _uiMan = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<TippyEvent>(OnClippyEv);
    }

    private void OnClippyEv(TippyEvent ev)
    {
        _uiMan.GetUIController<TippyUIController>().AddMessage(ev);
    }
}
