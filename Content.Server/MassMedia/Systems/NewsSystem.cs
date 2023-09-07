using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Server.PDA.Ringer;
using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared.MassMedia.Components;
using Content.Shared.MassMedia.Systems;
using Content.Shared.PDA;
using Robust.Server.GameObjects;
using Content.Server.Administration.Logs;
using Content.Server.CartridgeLoader.Cartridges;
using Content.Shared.CartridgeLoader;
using Content.Shared.CartridgeLoader.Cartridges;
using Content.Server.CartridgeLoader;
using Content.Server.MassMedia.Components;
using Robust.Shared.Timing;
using Content.Server.Popups;
using Content.Server.Station.Systems;
using Content.Shared.Database;

namespace Content.Server.MassMedia.Systems;

public sealed class NewsSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;
    [Dependency] private readonly CartridgeLoaderSystem _cartridgeLoaderSystem = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly StationSystem _station = default!;
    [Dependency] private readonly IAdminLogManager _adminLogger = default!;

    [Dependency] private readonly AccessReaderSystem _accessReader = default!;

    public override void Initialize()
    {
        base.Initialize();

        // News writer
        SubscribeLocalEvent<NewsWriterComponent, NewsWriterDeleteMessage>(OnWriteUiDeleteMessage);
        SubscribeLocalEvent<NewsWriterComponent, NewsWriterArticlesRequestMessage>(OnRequestArticlesUiMessage);
        SubscribeLocalEvent<NewsWriterComponent, NewsWriterPublishMessage>(OnWriteUiPublishMessage);

        // News reader
        SubscribeLocalEvent<NewsReaderCartridgeComponent, CartridgeAddedEvent>(OnAdded);
        SubscribeLocalEvent<NewsReaderCartridgeComponent, CartridgeRemovedEvent>(OnRemoved);
        SubscribeLocalEvent<NewsReaderCartridgeComponent, NewsArticlePublishedEvent>(OnArticlePublished);
        SubscribeLocalEvent<NewsReaderCartridgeComponent, NewsArticleDeletedEvent>(OnArticleDeleted);
        SubscribeLocalEvent<NewsReaderCartridgeComponent, CartridgeMessageEvent>(OnReaderUiMessage);
        SubscribeLocalEvent<NewsReaderCartridgeComponent, CartridgeUiReadyEvent>(OnReaderUiReady);
    }

    private void OnAdded(EntityUid uid, NewsReaderCartridgeComponent component, CartridgeAddedEvent args)
    {
        component.CartridgeLoader = args.Loader;
    }

    private void OnRemoved(EntityUid uid, NewsReaderCartridgeComponent component, CartridgeRemovedEvent args)
    {
        component.CartridgeLoader = default;
    }

    private void OnArticlePublished(EntityUid uid, NewsReaderCartridgeComponent component, ref NewsArticlePublishedEvent args)
    {
        if (!component.CartridgeLoader.HasValue)
            return;

        UpdateReaderUi(uid, component.CartridgeLoader.Value, component);

        if (!component.NotificationOn )
            return;

        _cartridgeLoaderSystem.SendNotification(
            component.CartridgeLoader.Value,
            Loc.GetString("news-pda-notification-header"),
            args.Article.Name);
    }

    private void OnArticleDeleted(EntityUid uid, NewsReaderCartridgeComponent component, ref NewsArticleDeletedEvent args)
    {
        if (!component.CartridgeLoader.HasValue)
            return;

        UpdateReaderUi(uid, component.CartridgeLoader.Value, component);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<NewsWriterComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (comp.PublishEnabled || _timing.CurTime < comp.NextPublish)
                continue;

            comp.PublishEnabled = true;
            UpdateWriterUi(uid, comp);
        }
    }

    private bool TryGetArticles(EntityUid uid, [NotNullWhen(true)] out List<NewsArticle>? articles)
    {
        if (_station.GetOwningStation(uid) is not { } station ||
            !TryComp<StationNewsComponent>(station, out var stationNews))
        {
            articles = null;
            return false;
        }

        articles = stationNews.Articles;
        return true;
    }

    private void OnReaderUiReady(EntityUid uid, NewsReaderCartridgeComponent component, CartridgeUiReadyEvent args)
    {
        UpdateReaderUi(uid, args.Loader, component);
    }

    private void UpdateWriterUi(EntityUid uid, NewsWriterComponent component)
    {
        if (!_ui.TryGetUi(uid, NewsWriterUiKey.Key, out var ui))
            return;

        if (!TryGetArticles(uid, out var articles))
            return;

        var state = new NewsWriterBoundUserInterfaceState(articles.ToArray(), component.PublishEnabled, component.NextPublish);
        UserInterfaceSystem.SetUiState(ui, state);
    }

    private void UpdateReaderUi(EntityUid uid, EntityUid loaderUid, NewsReaderCartridgeComponent? component)
    {
        if (!Resolve(uid, ref component))
            return;

        if (!TryGetArticles(uid, out var articles))
            return;

        NewsReaderLeafArticle(uid, component, 0);

        if (!articles.Any())
        {
            _cartridgeLoaderSystem.UpdateCartridgeUiState(loaderUid, new NewsReaderEmptyBoundUserInterfaceState(component.NotificationOn));
            return;
        }

        var state = new NewsReaderBoundUserInterfaceState(
            articles[component.ArticleNumber],
            component.ArticleNumber + 1,
            articles.Count,
            component.NotificationOn);

        _cartridgeLoaderSystem.UpdateCartridgeUiState(loaderUid, state);
    }

    private void OnReaderUiMessage(EntityUid uid, NewsReaderCartridgeComponent component, CartridgeMessageEvent args)
    {
        if (args is not NewsReaderUiMessageEvent message)
            return;

        switch (message.Action)
        {
            case NewsReaderUiAction.Next:
                NewsReaderLeafArticle(uid, component, 1);
                break;
            case NewsReaderUiAction.Prev:
                NewsReaderLeafArticle(uid, component, -1);
                break;
            case NewsReaderUiAction.NotificationSwitch:
                component.NotificationOn = !component.NotificationOn;
                break;
        }

        UpdateReaderUi(uid, args.LoaderUid, component);
    }

    private void OnWriteUiPublishMessage(EntityUid uid, NewsWriterComponent component, NewsWriterPublishMessage msg)
    {
        if (!component.PublishEnabled)
            return;

        component.PublishEnabled = false;
        component.NextPublish = _timing.CurTime + TimeSpan.FromSeconds(component.PublishCooldown);

        if (!TryGetArticles(uid, out var articles))
            return;

        var article = msg.Article;
        var author = msg.Session.AttachedEntity;

        article.Name = article.Name.Length <= 108 ? article.Name : article.Name[..108];

        if (author.HasValue
            && _accessReader.FindAccessItemsInventory(author.Value, out var items)
            && _accessReader.FindStationRecordKeys(author.Value, out var stationRecordKeys, items))
        {
            article.AuthorStationRecordKeyIds = stationRecordKeys;

            foreach (var item in items)
            {
                // ID
                if (TryComp(item, out IdCardComponent? id))
                {
                    article.Author = id.FullName;
                    break;
                }

                // PDA
                if (TryComp(item, out PdaComponent? pda)
                    && pda.ContainedId != null
                    && TryComp(pda.ContainedId, out id))
                {
                    article.Author = id.FullName;
                    break;
                }
            }
        }

        _audio.PlayPvs(component.ConfirmSound, uid);

        if (author != null)
        {
            _adminLogger.Add(
                LogType.Chat,
                LogImpact.Medium,
                $"{ToPrettyString(author.Value):actor} created news article {article.Name} by {article.Author}: {article.Content}"
                );
        }
        else
        {
            _adminLogger.Add(
                LogType.Chat,
                LogImpact.Medium,
                $"{msg.Session.Name:actor} created news article {article.Name}: {article.Content}"
                );
        }

        articles.Add(article);

        // Eventually replace with device networking to only notify pdas on the same station
        var args = new NewsArticlePublishedEvent(article);

        var query = EntityQueryEnumerator<NewsReaderCartridgeComponent>();
        while (query.MoveNext(out var readerUid, out _))
        {
            RaiseLocalEvent(readerUid, ref args);
        }

        UpdateWriterDevices();
    }

    private void OnWriteUiDeleteMessage(EntityUid uid, NewsWriterComponent component, NewsWriterDeleteMessage msg)
    {
        if (!TryGetArticles(uid, out var articles))
            return;

        if (msg.ArticleNum > articles.Count)
            return;

        var article = articles[msg.ArticleNum];
        var actor = msg.Session.AttachedEntity;

        if (CheckDeleteAccess(article, uid, actor))
        {
            if (actor != null)
            {
                _adminLogger.Add(
                    LogType.Chat,
                    LogImpact.Medium,
                    $"{ToPrettyString(actor.Value):actor} deleted news article {article.Name} by {article.Author}: {article.Content}"
                    );
            }
            else
            {
                _adminLogger.Add(
                    LogType.Chat, LogImpact.Medium,
                    $"{msg.Session.Name:actor} deleted news article {article.Name}: {article.Content}");
            }

            articles.RemoveAt(msg.ArticleNum);
            _audio.PlayPvs(component.ConfirmSound, uid);
        }
        else
        {
            _popup.PopupEntity(Loc.GetString("news-write-no-access-popup"), uid);
            _audio.PlayPvs(component.NoAccessSound, uid);
        }

        var args = new NewsArticleDeletedEvent();

        var query = EntityQueryEnumerator<NewsReaderCartridgeComponent>();
        while (query.MoveNext(out var readerUid, out _))
        {
            RaiseLocalEvent(readerUid, ref args);
        }

        UpdateWriterDevices();
    }

    private void OnRequestArticlesUiMessage(EntityUid uid, NewsWriterComponent component, NewsWriterArticlesRequestMessage msg)
    {
        UpdateWriterUi(uid, component);
    }

    private void NewsReaderLeafArticle(EntityUid uid, NewsReaderCartridgeComponent component, int leafDir)
    {
        if (!TryGetArticles(uid, out var articles))
            return;

        component.ArticleNumber += leafDir;

        if (component.ArticleNumber >= articles.Count)
            component.ArticleNumber = 0;

        if (component.ArticleNumber < 0)
            component.ArticleNumber = articles.Count - 1;
    }

    private void UpdateWriterDevices()
    {
        var query = EntityQueryEnumerator<NewsWriterComponent>();

        while (query.MoveNext(out var owner, out var comp))
        {
            UpdateWriterUi(owner, comp);
        }
    }

    private bool CheckDeleteAccess(NewsArticle articleToDelete, EntityUid device, EntityUid? user)
    {
        if (EntityManager.TryGetComponent<AccessReaderComponent>(device, out var accessReader) &&
            user.HasValue &&
            _accessReader.IsAllowed(user.Value, device, accessReader))
        {
            return true;
        }

        if (articleToDelete.AuthorStationRecordKeyIds == null || !articleToDelete.AuthorStationRecordKeyIds.Any())
        {
            return true;
        }

        return user.HasValue
               && _accessReader.FindStationRecordKeys(user.Value, out var recordKeys)
               && recordKeys.Intersect(articleToDelete.AuthorStationRecordKeyIds).Any();
    }
}

