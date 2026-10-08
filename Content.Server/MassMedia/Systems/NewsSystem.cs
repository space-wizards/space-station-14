using Content.Server.Administration.Logs;
using Content.Server.CartridgeLoader.Cartridges;
using Content.Server.CartridgeLoader;
using Content.Server.Chat.Managers;
using Content.Server.Discord;
using Content.Server.GameTicking;
using Content.Server.MassMedia.Components;
using Content.Server.Popups;
using Content.Server.Station.Systems;
using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared.CCVar;
using Content.Shared.CartridgeLoader.Cartridges;
using Content.Shared.CartridgeLoader;
using Content.Shared.Database;
using Content.Shared.GameTicking;
using Content.Shared.IdentityManagement;
using Content.Shared.MassMedia.Components;
using Content.Shared.MassMedia.Systems;
using Content.Shared.Popups;
using Robust.Server.GameObjects;
using Robust.Server;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Collections;
using Robust.Shared.Configuration;
using Robust.Shared.Maths;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace Content.Server.MassMedia.Systems;

public sealed partial class NewsSystem : SharedNewsSystem
{
    [Dependency] private AccessReaderSystem _accessReaderSystem = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IAdminLogManager _adminLogger = default!;
    [Dependency] private UserInterfaceSystem _ui = default!;
    [Dependency] private CartridgeLoaderSystem _cartridgeLoaderSystem = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private PopupSystem _popup = default!;
    [Dependency] private ServerStationSystem _station = default!;
    [Dependency] private ServerGameTicker _ticker = default!;
    [Dependency] private IChatManager _chatManager = default!;
    [Dependency] private DiscordWebhook _discord = default!;
    [Dependency] private IConfigurationManager _cfg = default!;
    [Dependency] private IBaseServer _baseServer = default!;
    [Dependency] private IdentitySystem _identity = default!;
    [Dependency] private SharedIdCardSystem _idCard = default!;

    private WebhookIdentifier? _webhookId = null;
    private Color _webhookEmbedColor;
    private bool _webhookSendDuringRound;

    public override void Initialize()
    {
        base.Initialize();

        // Discord hook
        _cfg.OnValueChanged(CCVars.DiscordNewsWebhook,
            value =>
            {
                if (!string.IsNullOrWhiteSpace(value))
                    _discord.GetWebhook(value, data => _webhookId = data.ToIdentifier());
            }, true);

        _cfg.OnValueChanged(CCVars.DiscordNewsWebhookEmbedColor, value =>
            {
                _webhookEmbedColor = Color.LawnGreen;
                if (Color.TryParse(value, out var color))
                    _webhookEmbedColor = color;
            }, true);

        _cfg.OnValueChanged(CCVars.DiscordNewsWebhookSendDuringRound, value => _webhookSendDuringRound = value, true);
        SubscribeLocalEvent<RoundEndMessageEvent>(OnRoundEndMessageEvent);

        // News writer
        SubscribeLocalEvent<NewsWriterComponent, MapInitEvent>(OnMapInit);

        // New writer bui messages
        Subs.BuiEvents<NewsWriterComponent>(NewsWriterUiKey.Key, subs =>
        {
            subs.Event<NewsWriterDeleteMessage>(OnWriteUiDeleteMessage);
            subs.Event<NewsWriterToggleLockCommentsMessage>(OnWriteUiToggleLockCommentsMessage);
            subs.Event<NewsWriterDeleteCommentMessage>(OnWriteUiDeleteCommentMessage);
            subs.Event<NewsWriterArticlesRequestMessage>(OnRequestArticlesUiMessage);
            subs.Event<NewsWriterPublishMessage>(OnWriteUiPublishMessage);
            subs.Event<NewsWriterSaveDraftMessage>(OnNewsWriterDraftUpdatedMessage);
            subs.Event<NewsWriterRequestDraftMessage>(OnRequestArticleDraftMessage);
        });

        // News reader
        SubscribeLocalEvent<NewsReaderCartridgeComponent, NewsArticlePublishedEvent>(OnArticlePublished);
        SubscribeLocalEvent<NewsReaderCartridgeComponent, NewsArticleDeletedEvent>(OnArticleDeleted);
        SubscribeLocalEvent<NewsReaderCartridgeComponent, NewsArticleUpdatedEvent>(OnArticleUpdatedEvent);
        SubscribeLocalEvent<NewsReaderCartridgeComponent, CartridgeMessageEvent>(OnReaderUiMessage);
        SubscribeLocalEvent<NewsReaderCartridgeComponent, CartridgeUiReadyEvent>(OnReaderUiReady);
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
            UpdateWriterUi((uid, comp));
        }
    }

    #region Writer Event Handlers

    private void OnMapInit(Entity<NewsWriterComponent> ent, ref MapInitEvent args)
    {
        var station = _station.GetOwningStation(ent);
        if (!station.HasValue)
            return;

        EnsureComp<StationNewsComponent>(station.Value);
    }

    private void OnWriteUiDeleteMessage(Entity<NewsWriterComponent> ent, ref NewsWriterDeleteMessage msg)
    {
        if (!TryGetStationNews(ent, out var stationNews))
            return;

        var articles = stationNews.Articles;
        if (msg.ArticleNum < 0 || msg.ArticleNum >= articles.Count)
            return;

        var article = articles[msg.ArticleNum];
        if (TryCheckConsoleAccess(ent, msg.Actor))
        {
            _adminLogger.Add(
                LogType.Chat, LogImpact.Medium,
                $"{ToPrettyString(msg.Actor):actor} deleted news article {article.Title} by {article.Author}: {article.Content}"
            );

            articles.RemoveAt(msg.ArticleNum);

            // Clean up any per-PDA reactions associated with the deleted article without closure variable capture.
            var keysToRemove = new ValueList<(int ArticleId, NetEntity LoaderUid)>();
            foreach (var key in stationNews.PdaReactions.Keys)
            {
                if (key.ArticleId == article.ArticleId)
                    keysToRemove.Add(key);
            }

            foreach (var key in keysToRemove)
            {
                stationNews.PdaReactions.Remove(key);
            }

            _audio.PlayPvs(ent.Comp.ConfirmSound, ent);
        }

        var args = new NewsArticleDeletedEvent();
        var query = EntityQueryEnumerator<NewsReaderCartridgeComponent>();
        while (query.MoveNext(out var readerUid, out _))
        {
            RaiseLocalEvent(readerUid, ref args);
        }

        UpdateWriterDevices();
    }

    private void OnWriteUiToggleLockCommentsMessage(
        Entity<NewsWriterComponent> ent,
        ref NewsWriterToggleLockCommentsMessage msg)
    {
        if (!TryGetStationNews(ent, out var stationNews))
            return;

        var articles = stationNews.Articles;
        if (msg.ArticleNum < 0 || msg.ArticleNum >= articles.Count)
            return;

        if (!TryCheckConsoleAccess(ent, msg.Actor))
            return;

        var article = articles[msg.ArticleNum];
        article.CommentsLocked = !article.CommentsLocked;
        articles[msg.ArticleNum] = article;

        _adminLogger.Add(
            LogType.Chat,
            LogImpact.Low,
            $"{ToPrettyString(msg.Actor):actor} {(article.CommentsLocked ? "locked" : "unlocked")} comments on news article {article.Title}");

        _audio.PlayPvs(ent.Comp.ConfirmSound, ent);
        BroadcastArticleUpdated(article.ArticleId);
        UpdateWriterDevices();
    }

    private void OnWriteUiDeleteCommentMessage(
        Entity<NewsWriterComponent> ent,
        ref NewsWriterDeleteCommentMessage msg)
    {
        if (!TryGetStationNews(ent, out var stationNews))
            return;

        var articles = stationNews.Articles;
        if (msg.ArticleNum < 0 || msg.ArticleNum >= articles.Count)
            return;

        if (!TryCheckConsoleAccess(ent, msg.Actor))
            return;

        var article = articles[msg.ArticleNum];
        if (article.Comments == null)
            return;

        var commentIdx = -1;
        for (var i = 0; i < article.Comments.Count; i++)
        {
            if (article.Comments[i].CommentId != msg.CommentId)
                continue;

            commentIdx = i;
            break;
        }

        if (commentIdx < 0)
            return;

        var removed = article.Comments[commentIdx];
        article.Comments.RemoveAt(commentIdx);
        articles[msg.ArticleNum] = article;

        _adminLogger.Add(
            LogType.Chat,
            LogImpact.Low,
            $"{ToPrettyString(msg.Actor):actor} deleted comment by {removed.Author ?? "Anonymous"} on news article {article.Title}: {removed.Content}");

        _audio.PlayPvs(ent.Comp.ConfirmSound, ent);
        BroadcastArticleUpdated(article.ArticleId);
        UpdateWriterDevices();
    }

    private void OnRequestArticlesUiMessage(Entity<NewsWriterComponent> ent, ref NewsWriterArticlesRequestMessage msg)
    {
        UpdateWriterUi(ent);
    }

    private void OnWriteUiPublishMessage(Entity<NewsWriterComponent> ent, ref NewsWriterPublishMessage msg)
    {
        if (!ent.Comp.PublishEnabled)
            return;

        if (!CanUse(msg.Actor, ent.Owner))
            return;

        ent.Comp.PublishEnabled = false;
        ent.Comp.NextPublish = _timing.CurTime + TimeSpan.FromSeconds(ent.Comp.PublishCooldown);

        var authorName = _identity.GetIdentityShortInfo(msg.Actor, ent);

        var title = msg.Title.Trim();
        var content = msg.Content.Trim();

        if (TryAddNews(ent, title, content, out var article, authorName, msg.Actor, msg.CommentsLocked))
        {
            _audio.PlayPvs(ent.Comp.ConfirmSound, ent);

            _chatManager.SendAdminAnnouncement(Loc.GetString("news-publish-admin-announcement",
                                                             ("actor", msg.Actor),
                                                             ("title", article.Value.Title),
                                                             ("author", article.Value.Author ?? Loc.GetString("news-read-ui-no-author"))
            ));
        }
    }

    /// <summary>
    /// Adds a news article to the station's news feed.
    /// </summary>
    /// <param name="uid">Entity on the station to which news will be added.</param>
    /// <param name="title">Title of the news article.</param>
    /// <param name="content">Content of the news article.</param>
    /// <param name="article">The created news article.</param>
    /// <param name="author">Author of the news article.</param>
    /// <param name="actor">Entity which caused the news article to publish. Used for admin logs.</param>
    /// <param name="commentsLocked">Whether comments are initially locked on this article.</param>
    public bool TryAddNews(
        EntityUid uid,
        string title,
        string content,
        [NotNullWhen(true)] out NewsArticle? article,
        string? author = null,
        EntityUid? actor = null,
        bool commentsLocked = false)
    {
        if (!TryGetStationNews(uid, out var stationNews))
        {
            article = null;
            return false;
        }

        var articles = stationNews.Articles;

        article = new NewsArticle
        {
            ArticleId = stationNews.NextArticleId++,
            Title = title.Length <= MaxTitleLength ? title : $"{title[..MaxTitleLength]}...",
            Content = content.Length <= MaxContentLength ? content : $"{content[..MaxContentLength]}...",
            Author = author,
            ShareTime = _ticker.RoundDuration(),
            CommentsLocked = commentsLocked,
            Comments = new List<NewsComment>(),
            ReactionCounts = new Dictionary<NewsReactionType, int>()
        };

        articles.Add(article.Value);

        if (actor != null)
        {
            _adminLogger.Add(
                LogType.Chat,
                LogImpact.Medium,
                $"{ToPrettyString(actor):actor} created news article {article.Value.Title} by {article.Value.Author}: {article.Value.Content}");
        }
        else
        {
            _adminLogger.Add(
                LogType.Chat,
                LogImpact.Medium,
                $"Created news article {article.Value.Title} by {article.Value.Author}: {article.Value.Content}");
        }

        var args = new NewsArticlePublishedEvent(article.Value);
        var query = EntityQueryEnumerator<NewsReaderCartridgeComponent>();

        while (query.MoveNext(out var readerUid, out _))
        {
            RaiseLocalEvent(readerUid, ref args);
        }

        if (_webhookSendDuringRound)
            AddNewsSendWebhook(article.Value);

        UpdateWriterDevices();

        return true;
    }

    private async void AddNewsSendWebhook(NewsArticle article)
    {
        await Task.Run(async () => await SendArticleToDiscordWebhook(article));
    }

    #endregion

    #region Reader Event Handlers

    private void OnArticlePublished(Entity<NewsReaderCartridgeComponent> ent, ref NewsArticlePublishedEvent args)
    {
        if (Comp<CartridgeComponent>(ent).LoaderUid is not { } loaderUid)
            return;

        UpdateReaderUi(ent, loaderUid);

        if (!ent.Comp.NotificationOn)
            return;

        _cartridgeLoaderSystem.SendNotification(
            loaderUid,
            Loc.GetString("news-pda-notification-header"),
            args.Article.Title);
    }

    private void OnArticleDeleted(Entity<NewsReaderCartridgeComponent> ent, ref NewsArticleDeletedEvent args)
    {
        if (Comp<CartridgeComponent>(ent).LoaderUid is not { } loaderUid)
            return;

        UpdateReaderUi(ent, loaderUid);
    }

    private void OnArticleUpdatedEvent(Entity<NewsReaderCartridgeComponent> ent, ref NewsArticleUpdatedEvent args)
    {
        if (Comp<CartridgeComponent>(ent).LoaderUid is not { } loaderUid)
            return;

        UpdateReaderUi(ent, loaderUid);
    }

    private void OnReaderUiMessage(Entity<NewsReaderCartridgeComponent> ent, ref CartridgeMessageEvent args)
    {
        var loaderUid = GetEntity(args.LoaderUid);

        switch (args)
        {
            case NewsReaderUiMessageEvent message:
                switch (message.Action)
                {
                    case NewsReaderUiAction.Next:
                        NewsReaderLeafArticle(ent, 1);
                        break;
                    case NewsReaderUiAction.Prev:
                        NewsReaderLeafArticle(ent, -1);
                        break;
                    case NewsReaderUiAction.NotificationSwitch:
                        ent.Comp.NotificationOn = !ent.Comp.NotificationOn;
                        break;
                }

                UpdateReaderUi(ent, loaderUid);
                break;

            case NewsReaderReactionMessageEvent reactionMsg:
                HandleReaderReaction(ent, args.LoaderUid, reactionMsg.Reaction);
                break;

            case NewsReaderCommentMessageEvent commentMsg:
                HandleReaderComment(ent, loaderUid, args.Actor, commentMsg.Comment);
                break;
        }
    }

    private void HandleReaderReaction(
        Entity<NewsReaderCartridgeComponent> ent,
        NetEntity loaderNetEntity,
        NewsReactionType reaction)
    {
        if (!TryGetStationNews(ent, out var stationNews))
            return;

        var articles = stationNews.Articles;
        NewsReaderLeafArticle(ent, 0);
        if (articles.Count == 0 || ent.Comp.ArticleNumber < 0 || ent.Comp.ArticleNumber >= articles.Count)
            return;

        var article = articles[ent.Comp.ArticleNumber];
        article.ReactionCounts ??= new Dictionary<NewsReactionType, int>();

        var key = (article.ArticleId, loaderNetEntity);
        if (!stationNews.PdaReactions.TryGetValue(key, out var pdaReactions))
        {
            pdaReactions = new HashSet<NewsReactionType>();
            stationNews.PdaReactions[key] = pdaReactions;
        }

        var currentCount = article.ReactionCounts.GetValueOrDefault(reaction, 0);
        if (pdaReactions.Remove(reaction))
        {
            var nextCount = Math.Max(0, currentCount - 1);
            if (nextCount == 0)
                article.ReactionCounts.Remove(reaction);
            else
                article.ReactionCounts[reaction] = nextCount;
        }
        else
        {
            pdaReactions.Add(reaction);
            article.ReactionCounts[reaction] = currentCount + 1;
        }

        articles[ent.Comp.ArticleNumber] = article;
        BroadcastArticleUpdated(article.ArticleId);
        UpdateWriterDevices();
    }

    private void HandleReaderComment(
        Entity<NewsReaderCartridgeComponent> ent,
        EntityUid loaderUid,
        EntityUid actor,
        string rawComment)
    {
        if (!TryGetStationNews(ent, out var stationNews))
            return;

        var articles = stationNews.Articles;
        NewsReaderLeafArticle(ent, 0);
        if (articles.Count == 0 || ent.Comp.ArticleNumber < 0 || ent.Comp.ArticleNumber >= articles.Count)
            return;

        var article = articles[ent.Comp.ArticleNumber];
        if (article.CommentsLocked)
            return;

        var trimmed = rawComment.Trim();
        if (trimmed.Length == 0)
            return;

        article.Comments ??= new List<NewsComment>();
        if (article.Comments.Count >= MaxCommentsPerArticle)
            return;

        var content = trimmed.Length <= MaxCommentLength
            ? trimmed
            : $"{trimmed[..MaxCommentLength]}...";

        var author = GetPdaAuthorName(loaderUid);

        var comment = new NewsComment
        {
            CommentId = stationNews.NextCommentId++,
            Author = author,
            Content = content,
            PostedTime = _ticker.RoundDuration()
        };

        article.Comments.Add(comment);
        articles[ent.Comp.ArticleNumber] = article;

        _adminLogger.Add(
            LogType.Chat,
            LogImpact.Low,
            $"{ToPrettyString(actor):actor} commented on news article {article.Title} as {author ?? "Anonymous"}: {content}");

        BroadcastArticleUpdated(article.ArticleId);
        UpdateWriterDevices();
    }

    /// <summary>
    /// Gets the author string from the ID card inserted into the given PDA (<paramref name="loaderUid"/>),
    /// or null if no ID card with a name is inserted.
    /// </summary>
    private string? GetPdaAuthorName(EntityUid loaderUid)
    {
        if (!_idCard.TryGetIdCard(loaderUid, out var idCard) ||
            string.IsNullOrWhiteSpace(idCard.Comp.FullName))
        {
            return null;
        }

        // FullName only permits read/write access, so copy it before invoking methods on it.
        string fullName = idCard.Comp.FullName;
        fullName = fullName.Trim();
        if (!string.IsNullOrWhiteSpace(idCard.Comp.LocalizedJobTitle))
        {
            var jobTitle = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(idCard.Comp.LocalizedJobTitle.Trim());
            return Loc.GetString("news-read-ui-comment-author-job", ("author", fullName), ("job", jobTitle));
        }

        return fullName;
    }

    private void BroadcastArticleUpdated(int articleId)
    {
        var args = new NewsArticleUpdatedEvent(articleId);
        var query = EntityQueryEnumerator<NewsReaderCartridgeComponent>();
        while (query.MoveNext(out var readerUid, out _))
        {
            RaiseLocalEvent(readerUid, ref args);
        }
    }

    private void OnReaderUiReady(Entity<NewsReaderCartridgeComponent> ent, ref CartridgeUiReadyEvent args)
    {
        UpdateReaderUi(ent, args.Loader);
    }
    #endregion

    private bool TryGetStationNews(EntityUid uid, [NotNullWhen(true)] out StationNewsComponent? stationNews)
    {
        if (_station.GetOwningStation(uid) is not { } station ||
            !TryComp(station, out stationNews))
        {
            stationNews = null;
            return false;
        }

        return true;
    }

    private bool TryGetArticles(EntityUid uid, [NotNullWhen(true)] out List<NewsArticle>? articles)
    {
        if (!TryGetStationNews(uid, out var stationNews))
        {
            articles = null;
            return false;
        }

        articles = stationNews.Articles;
        return true;
    }

    private void UpdateWriterUi(Entity<NewsWriterComponent> ent)
    {
        if (!_ui.HasUi(ent, NewsWriterUiKey.Key))
            return;

        if (!TryGetArticles(ent, out var articles))
            return;

        var state = new NewsWriterBoundUserInterfaceState(
            articles.ToArray(),
            ent.Comp.PublishEnabled,
            ent.Comp.NextPublish,
            ent.Comp.DraftTitle,
            ent.Comp.DraftContent);
        _ui.SetUiState(ent.Owner, NewsWriterUiKey.Key, state);
    }

    private void UpdateReaderUi(Entity<NewsReaderCartridgeComponent> ent, EntityUid loaderUid)
    {
        if (!TryGetStationNews(ent, out var stationNews))
            return;

        var articles = stationNews.Articles;
        NewsReaderLeafArticle(ent, 0);

        if (articles.Count == 0)
        {
            _cartridgeLoaderSystem.UpdateCartridgeUiState(loaderUid, new NewsReaderEmptyBoundUserInterfaceState(ent.Comp.NotificationOn));
            return;
        }

        var article = articles[ent.Comp.ArticleNumber];
        HashSet<NewsReactionType>? activeReactions = null;
        if (stationNews.PdaReactions.TryGetValue((article.ArticleId, GetNetEntity(loaderUid)), out var reactions))
            activeReactions = new HashSet<NewsReactionType>(reactions);

        var state = new NewsReaderBoundUserInterfaceState(
            article,
            ent.Comp.ArticleNumber + 1,
            articles.Count,
            ent.Comp.NotificationOn,
            activeReactions);

        _cartridgeLoaderSystem.UpdateCartridgeUiState(loaderUid, state);
    }

    private void NewsReaderLeafArticle(Entity<NewsReaderCartridgeComponent> ent, int leafDir)
    {
        if (!TryGetArticles(ent, out var articles))
            return;

        ent.Comp.ArticleNumber += leafDir;

        if (ent.Comp.ArticleNumber >= articles.Count)
            ent.Comp.ArticleNumber = 0;

        if (ent.Comp.ArticleNumber < 0)
            ent.Comp.ArticleNumber = articles.Count - 1;
    }

    private void UpdateWriterDevices()
    {
        var query = EntityQueryEnumerator<NewsWriterComponent>();
        while (query.MoveNext(out var owner, out var comp))
        {
            UpdateWriterUi((owner, comp));
        }
    }

    /// <summary>
    /// Verifies that <paramref name="actor"/> has access to use the news writer console,
    /// playing a denial sound and popup if access is denied.
    /// </summary>
    private bool TryCheckConsoleAccess(Entity<NewsWriterComponent> ent, EntityUid actor)
    {
        if (CanUse(actor, ent.Owner))
            return true;

        _popup.PopupEntity(Loc.GetString("news-write-no-access-popup"), ent, PopupType.SmallCaution);
        _audio.PlayPvs(ent.Comp.NoAccessSound, ent);
        return false;
    }

    private bool CanUse(EntityUid user, EntityUid console)
    {
        if (TryComp<AccessReaderComponent>(console, out var accessReaderComponent))
        {
            return _accessReaderSystem.IsAllowed(user, console, accessReaderComponent);
        }
        return true;
    }

    private void OnNewsWriterDraftUpdatedMessage(Entity<NewsWriterComponent> ent, ref NewsWriterSaveDraftMessage args)
    {
        ent.Comp.DraftTitle = args.DraftTitle;
        ent.Comp.DraftContent = args.DraftContent;
    }

    private void OnRequestArticleDraftMessage(Entity<NewsWriterComponent> ent, ref NewsWriterRequestDraftMessage msg)
    {
        UpdateWriterUi(ent);
    }

    #region Discord Hook

    private void OnRoundEndMessageEvent(RoundEndMessageEvent ev)
    {
        if (_webhookSendDuringRound)
            return;

        var query = EntityQueryEnumerator<StationNewsComponent>();

        while (query.MoveNext(out _, out var comp))
        {
            SendArticlesListToDiscordWebhook(comp.Articles.OrderBy(article => article.ShareTime));
        }
    }

    private async void SendArticlesListToDiscordWebhook(IOrderedEnumerable<NewsArticle> articles)
    {
        foreach (var article in articles)
        {
            await Task.Delay(TimeSpan.FromSeconds(1)); // TODO: proper discord rate limit handling
            await SendArticleToDiscordWebhook(article);
        }
    }

    private async Task SendArticleToDiscordWebhook(NewsArticle article)
    {
        if (_webhookId is null)
            return;

        try
        {
            var embed = new WebhookEmbed
            {
                Title = article.Title,
                // There is no need to cut article content. It's MaxContentLength smaller then discord's limit (4096):
                Description = FormattedMessage.RemoveMarkupPermissive(article.Content),
                Color = _webhookEmbedColor.ToArgb() & 0xFFFFFF, // HACK: way to get hex without A (transparency)
                Footer = new WebhookEmbedFooter
                {
                    Text = Loc.GetString("news-discord-footer",
                        ("server", _baseServer.ServerName),
                        ("round", _ticker.RoundId),
                        ("author", article.Author ?? Loc.GetString("news-discord-unknown-author")),
                        ("time", article.ShareTime.ToString(@"hh\:mm\:ss")))
                }
            };
            var payload = new WebhookPayload { Embeds = [embed] };
            await _discord.CreateMessage(_webhookId.Value, payload);
            Log.Info("Sent news article to Discord webhook");
        }
        catch (Exception e)
        {
            Log.Error($"Error while sending discord news article:\n{e}");
        }
    }

    #endregion
}
