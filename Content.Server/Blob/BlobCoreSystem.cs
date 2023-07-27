using System.Linq;
using Content.Server.Chat.Managers;
using Content.Server.GameTicking;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.Mind;
using Content.Server.Objectives;
using Content.Server.Roles;
using Content.Shared.Alert;
using Content.Shared.Blob;
using Content.Shared.Damage;
using Content.Shared.Destructible;
using Content.Shared.FixedPoint;
using Content.Shared.Popups;
using Content.Shared.Roles;
using Robust.Server.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;

namespace Content.Server.Blob;

public sealed class BlobCoreSystem : EntitySystem
{
    [Dependency] private readonly AlertsSystem _alerts = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly MindSystem _mindSystem = default!;
    [Dependency] private readonly IChatManager _chatManager = default!;
    [Dependency] private readonly AudioSystem _audioSystem = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly DamageableSystem _damageableSystem = default!;
    [Dependency] private readonly GameTicker _gameTicker = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BlobCoreComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<BlobCoreComponent, DestructionEventArgs>(OnDestruction);
        SubscribeLocalEvent<BlobCoreComponent, DamageChangedEvent>(OnDamaged);
        SubscribeLocalEvent<BlobCoreComponent, PlayerAttachedEvent>(OnPlayerAttached);
    }

    private void OnPlayerAttached(EntityUid uid, BlobCoreComponent component, PlayerAttachedEvent args)
    {
        var xform = Transform(uid);
        if (!_mapManager.TryGetGrid(xform.GridUid, out var map))
            return;

        CreateBlobObserver(uid, args.Player.UserId, component);
    }

    public bool CreateBlobObserver(EntityUid blobCoreUid, NetUserId userId, BlobCoreComponent? core = null)
    {
        var xform = Transform(blobCoreUid);

        if (!Resolve(blobCoreUid, ref core))
            return false;

        var blobRule = EntityQuery<BlobRuleComponent>().FirstOrDefault();

        if (blobRule == null)
        {
            //todo fuck me this shit is awful
            //no i wont fuck you, erp is against rules
            _gameTicker.StartGameRule("Blob", out var ruleEntity);
            blobRule = Comp<BlobRuleComponent>(ruleEntity);
        }

        var observer = Spawn(core.ObserverBlobPrototype, xform.Coordinates);

        core.Observer = observer;

        if (!TryComp<BlobObserverComponent>(observer, out var blobObserverComponent))
            return false;

        blobObserverComponent.Core = blobCoreUid;

        _mindSystem.TryGetMind(userId, out var mind);
        if (mind == null)
            return false;

        _mindSystem.TransferTo(mind, observer, ghostCheckOverride: false);

        _alerts.ShowAlert(observer, AlertType.BlobHealth, (short) Math.Clamp(Math.Round(core.CoreBlobTotalHealth.Float() / 10f), 0, 20));

        var antagPrototype = _prototypeManager.Index<AntagPrototype>(core.AntagBlobPrototypeId);
        var blobRole = new BlobRole(mind, antagPrototype);

        _mindSystem.AddRole(mind, blobRole);
        SendBlobBriefing(mind);

        blobRule.Blobs.Add(blobRole);

        if (_prototypeManager.TryIndex<ObjectivePrototype>("BlobCaptureObjective", out var objective)
            && objective.CanBeAssigned(mind))
        {
            _mindSystem.TryAddObjective(blobRole.Mind, objective);
        }

        if (_mindSystem.TryGetSession(mind, out var session))
        {
            _audioSystem.PlayGlobal(core.GreetSoundNotification, session);
        }

        return true;
    }

    private void SendBlobBriefing(Mind.Mind mind)
    {
        if (_mindSystem.TryGetSession(mind, out var session))
        {
            _chatManager.DispatchServerMessage(session, Loc.GetString("blob-role-greeting"));
        }
    }

    private void OnDamaged(EntityUid uid, BlobCoreComponent component, DamageChangedEvent args)
    {
        var maxHealth = component.CoreBlobTotalHealth;
        var currentHealth = maxHealth - args.Damageable.TotalDamage;

        if (component.Observer != null)
            _alerts.ShowAlert(component.Observer.Value, AlertType.BlobHealth, (short) Math.Clamp(Math.Round(currentHealth.Float() / 10f), 0, 20));
    }

    private void OnStartup(EntityUid uid, BlobCoreComponent component, ComponentStartup args)
    {
        ChangeBlobPoint(uid, 0, component);

        if (TryComp<BlobTileComponent>(uid, out var blobTileComponent))
        {
            blobTileComponent.Core = uid;
        }
    }

    private void OnDestruction(EntityUid uid, BlobCoreComponent component, DestructionEventArgs args)
    {
        if (component.Observer != null)
        {
            QueueDel(component.Observer.Value);
        }

        foreach (var blobTile in component.BlobTiles)
        {
            if (!TryComp<BlobTileComponent>(blobTile, out var blobTileComponent))
                continue;
            blobTileComponent.Core = null;

            blobTileComponent.State = BlobTileState.Dead;
            Dirty(blobTileComponent);
        }
    }

    public bool TransformBlobTile(EntityUid? oldTileUid, EntityUid coreTileUid, string newBlobTileProto, EntityCoordinates coordinates, BlobCoreComponent? blobCore = null, bool ReturnCost = true)
    {
        if (!Resolve(coreTileUid, ref blobCore))
            return false;
        if (oldTileUid != null)
        {
            QueueDel(oldTileUid.Value);
            blobCore.BlobTiles.Remove(oldTileUid.Value);
        }
        var resourceBlob = EntityManager.SpawnEntity(newBlobTileProto, coordinates);
        if (TryComp<BlobTileComponent>(resourceBlob, out var blobTileComponent))
        {
            blobTileComponent.ReturnCost = ReturnCost;
            blobTileComponent.Core = coreTileUid;
        }
        blobCore.BlobTiles.Add(resourceBlob);
        return true;
    }

    public bool RemoveBlobTile(EntityUid tileUid, EntityUid coreTileUid, BlobCoreComponent? blobCore = null)
    {
        if (!Resolve(coreTileUid, ref blobCore))
            return false;

        _damageableSystem.TryChangeDamage(tileUid, blobCore.DamageOnRemove);
        blobCore.BlobTiles.Remove(tileUid);

        return true;
    }

    public bool ChangeBlobPoint(EntityUid uid, FixedPoint2 amount, BlobCoreComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return false;

        component.Points += amount;

        if (component.Observer != null)
            _alerts.ShowAlert(component.Observer.Value, AlertType.BlobResource, (short) Math.Clamp(Math.Round(component.Points.Float() / 10f), 0, 16));

        return true;
    }

    public bool TryUseAbility(EntityUid uid, EntityUid coreUid, BlobCoreComponent component, FixedPoint2 abilityCost)
    {
        if (component.Points < abilityCost)
        {
            _popup.PopupEntity(Loc.GetString("blob-not-enough-resources"), uid, uid, PopupType.Large);
            return false;
        }

        ChangeBlobPoint(coreUid, -abilityCost, component);

        return true;
    }
}
