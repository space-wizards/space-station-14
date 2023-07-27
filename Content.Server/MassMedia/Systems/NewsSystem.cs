using Content.Server.MassMedia.Components;
using Robust.Server.GameObjects;
using Content.Shared.MassMedia.Components;
using Content.Shared.MassMedia.Systems;
using Content.Server.PDA.Ringer;
using Content.Shared.GameTicking;
using System.Linq;

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
        SubscribeLocalEvent<NewsReadComponent, NewsReadNextMessage>(OnReadUiMessage);
        SubscribeLocalEvent<NewsReadComponent, NewsReadPrevMessage>(OnReadUiMessage);
        SubscribeLocalEvent<NewsReadComponent, NewsReadArticleRequestMessage>(OnReadUiMessage);

        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestart);
    }

    private void OnRoundRestart(RoundRestartCleanupEvent ev)
    {
        if (Articles != null) Articles.Clear();
    }

    public void ToggleUi(EntityUid user, EntityUid deviceEnt, NewsReadComponent? component)
    {
        if (!Resolve(deviceEnt, ref component))
            return;

        if (!TryComp<ActorComponent>(user, out var actor))
            return;

        _ui.TryToggleUi(deviceEnt, NewsReadUiKey.Key, actor.PlayerSession);
    }

    public void ToggleUi(EntityUid user, EntityUid deviceEnt, NewsWriteComponent? component)
    {
        if (!Resolve(deviceEnt, ref component))
            return;

        if (!TryComp<ActorComponent>(user, out var actor))
            return;

        _ui.TryToggleUi(deviceEnt, NewsWriteUiKey.Key, actor.PlayerSession);
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

        if (component.ArticleNum < 0) NewsReadLeafArticle(component, -1);

        if (Articles.Any())
            _ui.TrySetUiState(uid, NewsReadUiKey.Key, new NewsReadBoundUserInterfaceState(Articles[component.ArticleNum], component.ArticleNum + 1, Articles.Count));
        else
            _ui.TrySetUiState(uid, NewsReadUiKey.Key, new NewsReadEmptyBoundUserInterfaceState());
    }

    public void OnWriteUiMessage(EntityUid uid, NewsWriteComponent component, NewsWriteShareMessage msg)
    {
        Articles.Add(msg.Article);

        UpdateReadDevices();
        UpdateWriteDevices();
        TryNotify();
    }

    public void OnWriteUiMessage(EntityUid uid, NewsWriteComponent component, NewsWriteDeleteMessage msg)
    {
        if (msg.ArticleNum < Articles.Count)
        {
            Articles.RemoveAt(msg.ArticleNum);
        }

        UpdateReadDevices();
        UpdateWriteDevices();
    }

    public void OnWriteUiMessage(EntityUid uid, NewsWriteComponent component, NewsWriteArticlesRequestMessage msg)
    {
        UpdateWriteUi(uid, component);
    }

    public void OnReadUiMessage(EntityUid uid, NewsReadComponent component, NewsReadNextMessage msg)
    {
        NewsReadLeafArticle(component, 1);

        UpdateReadUi(uid, component);
    }

    public void OnReadUiMessage(EntityUid uid, NewsReadComponent component, NewsReadPrevMessage msg)
    {
        NewsReadLeafArticle(component, -1);

        UpdateReadUi(uid, component);
    }

    public void OnReadUiMessage(EntityUid uid, NewsReadComponent component, NewsReadArticleRequestMessage msg)
    {
        UpdateReadUi(uid, component);
    }

    private void NewsReadLeafArticle(NewsReadComponent component, int leafDir)
    {
        component.ArticleNum += leafDir;

        if (component.ArticleNum >= Articles.Count) component.ArticleNum = 0;
        if (component.ArticleNum < 0) component.ArticleNum = Articles.Count - 1;
    }

    private void TryNotify()
    {
        var query = EntityQueryEnumerator<NewsReadComponent, RingerComponent>();

        while (query.MoveNext(out var comp, out var ringer))
        {
            _ringer.RingerPlayRingtone(comp.Owner, ringer);
        }
    }

    private void UpdateReadDevices()
    {
        var query = EntityQueryEnumerator<NewsReadComponent>();

        while (query.MoveNext(out var comp))
        {
            UpdateReadUi(comp.Owner, comp);
        }
    }

    private void UpdateWriteDevices()
    {
        var query = EntityQueryEnumerator<NewsWriteComponent>();

        while (query.MoveNext(out var comp))
        {
            UpdateWriteUi(comp.Owner, comp);
        }
    }
}

