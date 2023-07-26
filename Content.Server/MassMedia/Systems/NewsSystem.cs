using Content.Server.MassMedia.Components;
using Content.Server.PDA.Ringer;
using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared.GameTicking;
using Content.Shared.MassMedia.Components;
using Content.Shared.MassMedia.Systems;
using Content.Shared.PDA;
using Robust.Server.GameObjects;
using System.Linq;
using TerraFX.Interop.Windows;

namespace Content.Server.MassMedia.Systems;

public sealed class NewsSystem : EntitySystem
{
    [Dependency] private readonly UserInterfaceSystem _ui = default!;
    [Dependency] private readonly RingerSystem _ringer = default!;
    [Dependency] private readonly AccessReaderSystem _accessReader = default!;

    private readonly List<NewsArticle> _articles = new List<NewsArticle>();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<NewsWriteComponent, NewsWriteShareMessage>(OnWriteUiMessage);
        SubscribeLocalEvent<NewsWriteComponent, NewsWriteDeleteMessage>(OnDeleteUiMessage);
        SubscribeLocalEvent<NewsWriteComponent, NewsWriteArticlesRequestMessage>(OnRequestUiMessage);
        SubscribeLocalEvent<NewsReadComponent, NewsReadNextMessage>(OnNextArticleUiMessage);
        SubscribeLocalEvent<NewsReadComponent, NewsReadPrevMessage>(OnPrevArticleUiMessage);
        SubscribeLocalEvent<NewsReadComponent, NewsReadArticleRequestMessage>(OnReadUiMessage);

        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestart);
    }

    private void OnRoundRestart(RoundRestartCleanupEvent ev)
    {
        _articles?.Clear();
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

    public void UpdateWriteUi(EntityUid uid)
    {
        if (!_ui.TryGetUi(uid, NewsWriteUiKey.Key, out _))
            return;

        var state = new NewsWriteBoundUserInterfaceState(_articles.ToArray());
        _ui.TrySetUiState(uid, NewsWriteUiKey.Key, state);
    }

    public void UpdateReadUi(EntityUid uid, NewsReadComponent component)
    {
        if (!_ui.TryGetUi(uid, NewsReadUiKey.Key, out _))
            return;

        if (component.ArticleNum < 0)
        {
            NewsReadPreviousArticle(component);
        }

        if (_articles.Any())
        {
            if (component.ArticleNum >= _articles.Count)
            {
                component.ArticleNum = _articles.Count - 1;
            }

            _ui.TrySetUiState(uid, NewsReadUiKey.Key, new NewsReadBoundUserInterfaceState(_articles[component.ArticleNum], component.ArticleNum + 1, _articles.Count));
        }
        else
        {
            _ui.TrySetUiState(uid, NewsReadUiKey.Key, new NewsReadEmptyBoundUserInterfaceState());
        }
    }

    public void OnWriteUiMessage(EntityUid uid, NewsWriteComponent component, NewsWriteShareMessage msg)
    {
        var article = msg.Article;

        var author = msg.Session.AttachedEntity;
        if (author.HasValue
            && _accessReader.FindAccessItemsInventory(author.Value, out var items)
            && _accessReader.FindStationRecordKeys(author.Value, out var stationRecordKeys, items))
        {
            article.AuthorStationRecordKeyIds = stationRecordKeys;

            foreach (var item in items)
            {
                // ID Card
                if (TryComp(item, out IdCardComponent? id))
                {
                    article.Author = id.FullName;
                    break;
                }
                // PDA
                else if (TryComp(item, out PdaComponent? pda)
                    && pda.ContainedId != null
                    && TryComp(pda.ContainedId, out id))
                {
                    article.Author = id.FullName;
                    break;
                }
            }
        }

        _articles.Add(article);

        UpdateReadDevices();
        UpdateWriteDevices();
        TryNotify();
    }

    public void OnDeleteUiMessage(EntityUid uid, NewsWriteComponent component, NewsWriteDeleteMessage msg)
    {
        if (msg.ArticleNum > _articles.Count)
            return;

        var articleToDelete = _articles[msg.ArticleNum];
        if (articleToDelete.AuthorStationRecordKeyIds == null || !articleToDelete.AuthorStationRecordKeyIds.Any())
        {
            _articles.RemoveAt(msg.ArticleNum);
        }
        else
        {
            var author = msg.Session.AttachedEntity;
            if (author.HasValue
                && _accessReader.FindStationRecordKeys(author.Value, out var recordKeys)
                && recordKeys.Intersect(articleToDelete.AuthorStationRecordKeyIds).Any())
            {
                _articles.RemoveAt(msg.ArticleNum);
            }
        }

        UpdateReadDevices();
        UpdateWriteDevices();
    }

    public void OnRequestUiMessage(EntityUid uid, NewsWriteComponent component, NewsWriteArticlesRequestMessage msg)
    {
        UpdateWriteUi(uid);
    }

    public void OnNextArticleUiMessage(EntityUid uid, NewsReadComponent component, NewsReadNextMessage msg)
    {
        NewsReadNextArticle(component);

        UpdateReadUi(uid, component);
    }

    public void OnPrevArticleUiMessage(EntityUid uid, NewsReadComponent component, NewsReadPrevMessage msg)
    {
        NewsReadPreviousArticle(component);

        UpdateReadUi(uid, component);
    }

    public void OnReadUiMessage(EntityUid uid, NewsReadComponent component, NewsReadArticleRequestMessage msg)
    {
        UpdateReadUi(uid, component);
    }

    private void NewsReadNextArticle(NewsReadComponent component)
    {
        if (!_articles.Any())
        {
            return;
        }

        component.ArticleNum = (component.ArticleNum + 1) % _articles.Count;
    }

    private void NewsReadPreviousArticle(NewsReadComponent component)
    {
        if (!_articles.Any())
        {
            return;
        }

        component.ArticleNum = (component.ArticleNum - 1 + _articles.Count) % _articles.Count;
    }

    private void TryNotify()
    {
        var query = EntityQueryEnumerator<NewsReadComponent, RingerComponent>();

        while (query.MoveNext(out var owner, out var _, out var ringer))
        {
            _ringer.RingerPlayRingtone(owner, ringer);
        }
    }

    private void UpdateReadDevices()
    {
        var query = EntityQueryEnumerator<NewsReadComponent>();

        while (query.MoveNext(out var owner, out var comp))
        {
            UpdateReadUi(owner, comp);
        }
    }

    private void UpdateWriteDevices()
    {
        var query = EntityQueryEnumerator<NewsWriteComponent>();

        while (query.MoveNext(out var owner, out var _))
        {
            UpdateWriteUi(owner);
        }
    }
}

