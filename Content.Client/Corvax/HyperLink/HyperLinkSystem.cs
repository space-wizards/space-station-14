using Content.Shared.HyperLink;
using Robust.Client.UserInterface;

namespace Content.Client.HyperLink;

public sealed class HyperLinkSystem : EntitySystem
{
    public override void Initialize() 
    {
        base.Initialize();
        SubscribeNetworkEvent<OpenURLEvent>(OnOpenURL);
    }

    private void OnOpenURL(OpenURLEvent args)
    {
        var uriOpener = IoCManager.Resolve<IUriOpener>();
        uriOpener.OpenUri(args.URL);
    }
}
