using Content.Server.MassMedia.Components;
using Robust.Server.GameObjects;
using Content.Shared.MassMedia.Components;
using Content.Shared.MassMedia.Systems;
using Content.Server.PDA.Ringer;
using Content.Shared.GameTicking;
using System.Linq;
using Content.Server.CartridgeLoader.Cartridges;
using Content.Shared.CartridgeLoader;
using Content.Shared.CartridgeLoader.Cartridges;
using Content.Server.CartridgeLoader;
using Robust.Shared.Timing;


namespace Content.Server.MassMedia.Systems;

public sealed class NewsSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;
    [Dependency] private readonly RingerSystem _ringer = default!;
    [Dependency] private readonly CartridgeLoaderSystem? _cartridgeLoaderSystem = default!;

    public List<NewsArticle> Articles = new List<NewsArticle>();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<NewsWriteComponent, NewsWriteShareMessage>(OnWriteUiMessage);
        SubscribeLocalEvent<NewsWriteComponent, NewsWriteDeleteMessage>(OnWriteUiMessage);
        SubscribeLocalEvent<NewsWriteComponent, NewsWriteArticlesRequestMessage>(OnWriteUiMessage);

        SubscribeLocalEvent<NewsReadCartridgeComponent, CartridgeUiReadyEvent>(OnReadUiReady);
        SubscribeLocalEvent<NewsReadCartridgeComponent, CartridgeMessageEvent>(OnReadUiMessage);

        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestart);
    }

    private void OnRoundRestart(RoundRestartCleanupEvent ev)
    {
        if (Articles != null) Articles.Clear();
    }

    public void ToggleUi(EntityUid user, EntityUid deviceEnt, NewsWriteComponent? component)
    {
        if (!Resolve(deviceEnt, ref component))
            return;

        if (!TryComp<ActorComponent>(user, out var actor))
            return;

        _ui.TryToggleUi(deviceEnt, NewsWriteUiKey.Key, actor.PlayerSession);
    }

    public void OnReadUiReady(EntityUid uid, NewsReadCartridgeComponent component, CartridgeUiReadyEvent args)
    {
        UpdateReadUi(uid, args.Loader, component);
    }

    public void UpdateWriteUi(EntityUid uid, NewsWriteComponent component)
    {
        if (!_ui.TryGetUi(uid, NewsWriteUiKey.Key, out _))
            return;

        var state = new NewsWriteBoundUserInterfaceState(Articles.ToArray(), component.ShareAvalible);
        _ui.TrySetUiState(uid, NewsWriteUiKey.Key, state);
    }

    public void UpdateReadUi(EntityUid uid, EntityUid loaderUid, NewsReadCartridgeComponent? component)
    {
        if (!Resolve(uid, ref component))
            return;

        NewsReadLeafArticle(component, 0);

        if (Articles.Any())
            _cartridgeLoaderSystem?.UpdateCartridgeUiState(loaderUid, new NewsReadBoundUserInterfaceState(Articles[component.ArticleNum], component.ArticleNum + 1, Articles.Count, component.NotificationOn));
        else
            _cartridgeLoaderSystem?.UpdateCartridgeUiState(loaderUid, new NewsReadEmptyBoundUserInterfaceState(component.NotificationOn));
    }

    private void OnReadUiMessage(EntityUid uid, NewsReadCartridgeComponent component, CartridgeMessageEvent args)
    {
        if (args is not NewsReadUiMessageEvent message)
            return;

        if (message.Action == NewsReadUiAction.Next)
            NewsReadLeafArticle(component, 1);
        if (message.Action == NewsReadUiAction.Prev)
            NewsReadLeafArticle(component, -1);
        if (message.Action == NewsReadUiAction.NotificationSwith)
            component.NotificationOn = !component.NotificationOn;

        UpdateReadUi(uid, args.LoaderUid, component);
    }

    public void OnWriteUiMessage(EntityUid uid, NewsWriteComponent component, NewsWriteShareMessage msg)
    {
        Articles.Add(msg.Article);

        component.ShareAvalible = false;

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

    private void NewsReadLeafArticle(NewsReadCartridgeComponent component, int leafDir)
    {
        component.ArticleNum += leafDir;

        if (component.ArticleNum >= Articles.Count) component.ArticleNum = 0;
        if (component.ArticleNum < 0) component.ArticleNum = Articles.Count - 1;
    }

    private void TryNotify()
    {
        var query = EntityQueryEnumerator<CartridgeLoaderComponent, RingerComponent>();

        while (query.MoveNext(out var comp, out var ringer))
        {
            foreach (var app in comp.InstalledPrograms)
            {
                if (EntityManager.TryGetComponent<NewsReadCartridgeComponent>(app, out var cartridge) && cartridge.NotificationOn)
                {
                    _ringer.RingerPlayRingtone(comp.Owner, ringer);
                    break;
                }
            }
        }
    }

    private void UpdateReadDevices()
    {
        var query = EntityQueryEnumerator<CartridgeLoaderComponent>();

        while (query.MoveNext(out var comp))
        {
            if (EntityManager.TryGetComponent<NewsReadCartridgeComponent>(comp.ActiveProgram, out var cartridge))
                UpdateReadUi(cartridge.Owner, comp.Owner, cartridge);
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

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<NewsWriteComponent>();
        while (query.MoveNext(out var comp))
        {
            if (comp.ShareAvalible || _timing.CurTime < comp.NextShare)
                continue;

            comp.NextShare = _timing.CurTime + TimeSpan.FromSeconds(comp.ShareCooldown);
            comp.ShareAvalible = true;

            UpdateWriteUi(comp.Owner, comp);
        }
    }
}

