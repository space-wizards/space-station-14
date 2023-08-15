using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Server.MassMedia.Components;
using Content.Server.PDA.Ringer;
using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared.MassMedia.Components;
using Content.Shared.MassMedia.Systems;
using Content.Shared.PDA;
using Robust.Server.GameObjects;
using Content.Server.Administration.Logs;
using Content.Server.Cargo.Components;
using Content.Server.CartridgeLoader.Cartridges;
using Content.Shared.CartridgeLoader;
using Content.Shared.CartridgeLoader.Cartridges;
using Content.Server.CartridgeLoader;
using Robust.Shared.Timing;
using Content.Server.Popups;
using Content.Server.Station.Systems;
using Content.Shared.Database;

namespace Content.Server.MassMedia.Systems;

public sealed class NewsSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;
    [Dependency] private readonly RingerSystem _ringer = default!;
    [Dependency] private readonly CartridgeLoaderSystem? _cartridgeLoaderSystem = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly StationSystem _station = default!;
    [Dependency] private readonly IAdminLogManager _adminLogger = default!;

    [Dependency] private readonly AccessReaderSystem _accessReader = default!;

    /*
     *  if (_station.GetOwningStation(uid) is not { } station ||
            !TryComp<StationCargoBountyDatabaseComponent>(station, out var bountyDb))
            return;
     */

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<NewsWriterComponent, NewsWriterShareMessage>(OnWriteUiShareMessage);
        SubscribeLocalEvent<NewsWriterComponent, NewsWriterDeleteMessage>(OnWriteUiDeleteMessage);
        SubscribeLocalEvent<NewsWriterComponent, NewsWriterArticlesRequestMessage>(OnRequestWriteUiMessage);

        SubscribeLocalEvent<NewsReaderCartridgeComponent, CartridgeUiReadyEvent>(OnReaderUiReady);
        SubscribeLocalEvent<NewsReaderCartridgeComponent, CartridgeMessageEvent>(OnReadUiMessage);
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

        var state = new NewsWriterBoundUserInterfaceState(articles.ToArray(), component.PublishEnabled);
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
            _cartridgeLoaderSystem?.UpdateCartridgeUiState(loaderUid, new NewsReaderEmptyBoundUserInterfaceState(component.NotificationOn));
            return;
        }

        var state = new NewsReaderBoundUserInterfaceState(
            articles[component.ArticleNumber],
            component.ArticleNumber + 1,
            articles.Count,
            component.NotificationOn);

        _cartridgeLoaderSystem?.UpdateCartridgeUiState(loaderUid, state);
    }

    private void OnReadUiMessage(EntityUid uid, NewsReaderCartridgeComponent component, CartridgeMessageEvent args)
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
            case NewsReaderUiAction.NotificationSwith:
                component.NotificationOn = !component.NotificationOn;
                break;
        }

        UpdateReaderUi(uid, args.LoaderUid, component);
    }

    private void OnWriteUiShareMessage(EntityUid uid, NewsWriterComponent component, NewsWriterShareMessage msg)
    {
        if (!TryGetArticles(uid, out var articles))
            return;

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
                if (TryComp(item, out PdaComponent? pda) && pda.ContainedId != null && TryComp(pda.ContainedId, out id))
                {
                    article.Author = id.FullName;
                    break;
                }
            }
        }

        _audio.PlayPvs(component.ConfirmSound, uid);

        if (author != null)
            _adminLogger.Add(LogType.Chat, LogImpact.Medium, $"{ToPrettyString(author.Value):actor} created news article {article.Name} by {article.Author}: {article.Content}");
        else
            _adminLogger.Add(LogType.Chat, LogImpact.Medium, $"{msg.Session.Name:actor} created news article {article.Name}: {article.Content}");
        articles.Add(article);

        component.PublishEnabled = false;
        component.NextPublish = _timing.CurTime + TimeSpan.FromSeconds(component.PublishCooldown);

        UpdateReaderDevices();
        UpdateWriterDevices();
        TryNotify();
    }

    public void OnWriteUiDeleteMessage(EntityUid uid, NewsWriterComponent component, NewsWriterDeleteMessage msg)
    {
        if (!TryGetArticles(uid, out var articles))
            return;

        if (msg.ArticleNum > articles.Count)
            return;

        var articleDeleter = msg.Session.AttachedEntity;
        if (CheckDeleteAccess(articles[msg.ArticleNum], uid, articleDeleter))
        {
            if (articleDeleter != null)
                _adminLogger.Add(LogType.Chat, LogImpact.Medium, $"{ToPrettyString(articleDeleter.Value):actor} deleted news article {articles[msg.ArticleNum].Name} by {articles[msg.ArticleNum].Author}: {articles[msg.ArticleNum].Content}");
            else
                _adminLogger.Add(LogType.Chat, LogImpact.Medium, $"{msg.Session.Name:actor} created news article {articles[msg.ArticleNum].Name}: {articles[msg.ArticleNum].Content}");
            articles.RemoveAt(msg.ArticleNum);
            _audio.PlayPvs(component.ConfirmSound, uid);
        }
        else
        {
            _popup.PopupEntity(Loc.GetString("news-write-no-access-popup"), uid);
            _audio.PlayPvs(component.NoAccessSound, uid);
        }

        UpdateReaderDevices();
        UpdateWriterDevices();
    }

    public void OnRequestWriteUiMessage(EntityUid uid, NewsWriterComponent component, NewsWriterArticlesRequestMessage msg)
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

    private void TryNotify()
    {
        var query = EntityQueryEnumerator<CartridgeLoaderComponent, RingerComponent>();

        while (query.MoveNext(out var owner, out var comp, out var ringer))
        {
            foreach (var app in comp.InstalledPrograms)
            {
                if (EntityManager.TryGetComponent<NewsReaderCartridgeComponent>(app, out var cartridge) && cartridge.NotificationOn)
                {
                    _ringer.RingerPlayRingtone(owner, ringer);
                    break;
                }
            }
        }
    }

    private void UpdateReaderDevices()
    {
        var query = EntityQueryEnumerator<CartridgeLoaderComponent>();

        while (query.MoveNext(out var owner, out var comp))
        {
            if (EntityManager.TryGetComponent<NewsReaderCartridgeComponent>(comp.ActiveProgram, out var cartridge))
                UpdateReaderUi(cartridge.Owner, comp.Owner, cartridge);
        }
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
            _accessReader.IsAllowed(user.Value, accessReader))
        {
            return true;
        }

        if (articleToDelete.AuthorStationRecordKeyIds == null || !articleToDelete.AuthorStationRecordKeyIds.Any())
        {
            return true;
        }

        if (user.HasValue
            && _accessReader.FindStationRecordKeys(user.Value, out var recordKeys)
            && recordKeys.Intersect(articleToDelete.AuthorStationRecordKeyIds).Any())
        {
            return true;
        }

        return false;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<NewsWriterComponent>();
        while (query.MoveNext(out var comp))
        {
            if (comp.PublishEnabled || _timing.CurTime < comp.NextPublish)
                continue;

            comp.PublishEnabled = true;

            UpdateWriterUi(comp.Owner, comp);
        }
    }
}

