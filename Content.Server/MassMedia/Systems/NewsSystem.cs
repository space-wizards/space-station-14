using Content.Server.MassMedia.Components;
using Content.Server.UserInterface;
using Content.Server.CartridgeLoader;
using Robust.Server.GameObjects;
using Content.Shared.MassMedia.Components;
using Content.Shared.MassMedia.Systems;
using Content.Shared.PDA;
using Content.Server.DeviceNetwork;
using Robust.Shared.Timing;
using Content.Server.GameTicking;
using Content.Server.PDA.Ringer;

namespace Content.Server.MassMedia.Systems;

public sealed class NewsSystem : EntitySystem
{
    [Dependency] private readonly UserInterfaceSystem _ui = default!;
    [Dependency] private readonly RingerSystem _ringer = default!;

    public List<NewsArticle> Articles = new List<NewsArticle>();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<NewsWriteComponent, NewsWriteShareMessage>(OnWriteUiMessage);
        SubscribeLocalEvent<NewsWriteComponent, NewsWriteDeleteMessage>(OnWriteUiMessage);
        SubscribeLocalEvent<NewsWriteComponent, NewsWriteArticlesRequestMessage>(OnWriteUiMessage);
        SubscribeLocalEvent<NewsReadComponent, NewsReadLeafMessage>(OnReadUiMessage);
    }

    public void ToggleUi(EntityUid user, EntityUid deviceEnt, NewsReadComponent? component)
    {
        if (!Resolve(deviceEnt, ref component))
            return;

        if (!TryComp<ActorComponent>(user, out var actor))
            return;

        if (_ui.TryToggleUi(deviceEnt, NewsReadUiKey.Key, actor.PlayerSession))
        {

        }

        UpdateReadUi(deviceEnt, component);
    }

    public void ToggleUi(EntityUid user, EntityUid deviceEnt, NewsWriteComponent? component)
    {
        if (!Resolve(deviceEnt, ref component))
            return;

        if (!TryComp<ActorComponent>(user, out var actor))
            return;

        if (_ui.TryToggleUi(deviceEnt, NewsWriteUiKey.Key, actor.PlayerSession))
        {

        }

        UpdateWriteUi(deviceEnt, component);
    }

    public void UpdateWriteUi(EntityUid uid, NewsWriteComponent component)
    {
        if (!_ui.TryGetUi(uid, NewsWriteUiKey.Key, out _))
            return;

        var state = new NewsWriteBoundUserInterfaceState(Articles.ToArray());
        _ui.TrySetUiState(uid, NewsWriteUiKey.Key, state);
    }

    public void UpdateReadUi(EntityUid uid, NewsReadComponent component)
    {
        if (!_ui.TryGetUi(uid, NewsReadUiKey.Key, out _))
            return;

        if (component.ArticleNum < 0) NewsReadLeafArticle(component, false);

        NewsReadBoundUserInterfaceState state;
        if (Articles.Count != 0)
            state = new NewsReadBoundUserInterfaceState(Articles[component.ArticleNum], component.ArticleNum + 1, Articles.Count);
        else
            state = new NewsReadBoundUserInterfaceState(new NewsArticle(), -1, -1);

        _ui.TrySetUiState(uid, NewsReadUiKey.Key, state);
    }

    public void OnWriteUiMessage(EntityUid uid, NewsWriteComponent component, NewsWriteShareMessage msg)
    {
        Articles.Add(msg.Article);

        UpdateWriteUi(uid, component);
        TryNotify();
    }

    public void OnWriteUiMessage(EntityUid uid, NewsWriteComponent component, NewsWriteDeleteMessage msg)
    {
        component.Test = Articles.Count;

        if (msg.ArticleNum < Articles.Count)
        {
            Articles.RemoveAt(msg.ArticleNum);
        }

        UpdateWriteUi(uid, component);
    }

    public void OnWriteUiMessage(EntityUid uid, NewsWriteComponent component, NewsWriteArticlesRequestMessage msg)
    {
        UpdateWriteUi(uid, component);
    }

    public void OnReadUiMessage(EntityUid uid, NewsReadComponent component, NewsReadLeafMessage msg)
    {
        component.Test++;

        NewsReadLeafArticle(component, msg.IsNext);

        UpdateReadUi(uid, component);
    }

    private void NewsReadLeafArticle(NewsReadComponent component, bool isNext)
    {
        if (isNext)
            component.ArticleNum++;
        else
            component.ArticleNum--;

        if (component.ArticleNum >= Articles.Count) component.ArticleNum = 0;
        if (component.ArticleNum < 0) component.ArticleNum = Articles.Count - 1;
    }

    private void TryNotify()
    {
        var query = EntityQueryEnumerator<NewsReadComponent>();

        while (query.MoveNext(out var comp))
        {
            NewsReadTryRing(comp);
        }
    }

    private void NewsReadTryRing(NewsReadComponent component)
    {
        if (!EntityManager.TryGetComponent(component.Owner, out RingerComponent? ringer)) return;

        _ringer.RingerPlayRingtone(component.Owner, ringer);
    }
}

