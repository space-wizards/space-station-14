using Robust.Shared.Player;
using Content.Shared.Interaction;
using Content.Shared.HyperLink;

namespace Content.Server.HyperLink;

public sealed class HyperLinkSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<HyperLinkComponent, ActivateInWorldEvent>(OnActivate);
    }

    private void OnActivate(EntityUid uid, HyperLinkComponent component, ActivateInWorldEvent args)
    {
        if (!TryComp<ActorComponent>(args.User, out var actor))
            return;

        var url = String.Empty;

        switch (component.UrlType)
        {
            case "CorporateLaw":
                url = "https://wiki.deadspace14.net/%D0%9A%D0%BE%D1%80%D0%BF%D0%BE%D1%80%D0%B0%D1%82%D0%B8%D0%B2%D0%BD%D1%8B%D0%B9_%D0%97%D0%B0%D0%BA%D0%BE%D0%BD";
                break;
            case "SOPSec":
                url = "https://wiki.deadspace14.net/%D0%A1%D1%82%D0%B0%D0%BD%D0%B4%D0%B0%D1%80%D1%82%D0%BD%D1%8B%D0%B5_%D0%A0%D0%B0%D0%B1%D0%BE%D1%87%D0%B8%D0%B5_%D0%9F%D1%80%D0%BE%D1%86%D0%B5%D0%B4%D1%83%D1%80%D1%8B_(%D0%A1%D0%BB%D1%83%D0%B6%D0%B1%D0%B0_%D0%91%D0%B5%D0%B7%D0%BE%D0%BF%D0%B0%D1%81%D0%BD%D0%BE%D1%81%D1%82%D0%B8)";
                break;
            case "SOPLaw":
                url = "https://wiki.deadspace14.net/%D0%A1%D1%82%D0%B0%D0%BD%D0%B4%D0%B0%D1%80%D1%82%D0%BD%D1%8B%D0%B5_%D0%A0%D0%B0%D0%B1%D0%BE%D1%87%D0%B8%D0%B5_%D0%9F%D1%80%D0%BE%D1%86%D0%B5%D0%B4%D1%83%D1%80%D1%8B_(%D0%97%D0%B0%D0%BA%D0%BE%D0%BD)";
                break;
        }

        if (url == String.Empty)
            return;

        OpenURL(actor.PlayerSession, url);
    }

    public void OpenURL(ICommonSession session, string url)
    {
        var ev = new OpenURLEvent(url);
        RaiseNetworkEvent(ev, session.Channel);
    }
}
