using Content.Shared.DoAfter;
using Content.Shared.Explosion;
using Content.Shared.Input;
using Robust.Shared.Input.Binding;
using Content.Shared.Standing;
using Robust.Shared.Serialization;
using Content.Shared.Stunnable;
using Robust.Shared.Player;
using Content.Shared.Movement.Systems;

namespace Content.Shared.Crawling;
public sealed partial class CrawlingSystem : EntitySystem
{
    [Dependency] private readonly StandingStateSystem _standing = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _movementSpeedModifier = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CrawlerComponent, CrawlStandupDoAfterEvent>(OnDoAfter);
        SubscribeLocalEvent<CrawlerComponent, StandAttemptEvent>(OnStandUp);
        SubscribeLocalEvent<CrawlerComponent, DownAttemptEvent>(OnFall);
        SubscribeLocalEvent<CrawlerComponent, StunnedEvent>(OnStunned);
        SubscribeLocalEvent<CrawlerComponent, GetExplosionResistanceEvent>(OnGetExplosionResistance);

        SubscribeLocalEvent<CrawlingComponent, ComponentInit>(OnCrawlSlowdownInit);
        SubscribeLocalEvent<CrawlingComponent, ComponentShutdown>(OnCrawlSlowRemove);
        SubscribeLocalEvent<CrawlingComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshMovespeed);

        CommandBinds.Builder
            .Bind(ContentKeyFunctions.ToggleCrawling, InputCmdHandler.FromDelegate(ToggleCrawling, handle: false))
            .Register<CrawlingSystem>();
    }

    private void ToggleCrawling(ICommonSession? session)
    {
        if (session?.AttachedEntity == null || !TryComp<CrawlerComponent>(session.AttachedEntity, out var crawler))
            return;
        ///checks players standing state, downing player if they are standding and starts doafter with standing up if they are downed
        switch (_standing.IsDown(session.AttachedEntity.Value))
        {
            case false:
                _standing.Down(session.AttachedEntity.Value, dropHeldItems: false);
                break;
            case true:
                _doAfter.TryStartDoAfter(new DoAfterArgs(EntityManager, session.AttachedEntity.Value, crawler.StandUpTime, new CrawlStandupDoAfterEvent(), session.AttachedEntity.Value, used: session.AttachedEntity.Value)
                {
                    BreakOnDamage = true
                });
                break;
        }
    }
    private void OnDoAfter(EntityUid uid, CrawlerComponent component, CrawlStandupDoAfterEvent args)
    {
        if (args.Cancelled)
            return;
        _standing.Stand(uid);
    }
    private void OnStandUp(EntityUid uid, CrawlerComponent component, StandAttemptEvent args)
    {
        if (args.Cancelled)
            return;
        RemCompDeferred<CrawlingComponent>(uid);
    }
    private void OnFall(EntityUid uid, CrawlerComponent component, DownAttemptEvent args)
    {
        if (args.Cancelled)
            return;
        if (!HasComp<CrawlingComponent>(uid))
            AddComp<CrawlingComponent>(uid);
        //TODO: add hiding under table
    }
    private void OnStunned(EntityUid uid, CrawlerComponent component, StunnedEvent args)
    {
        if (!HasComp<CrawlingComponent>(uid))
            AddComp<CrawlingComponent>(uid);
    }
    private void OnGetExplosionResistance(EntityUid uid, CrawlerComponent component, ref GetExplosionResistanceEvent args)
    {
        // fall on explosion damage and lower explosion damage of crawling
        if (_standing.IsDown(uid))
            args.DamageCoefficient *= component.DownedDamageCoefficient;
        else
            _standing.Down(uid, dropHeldItems: false);
    }
    private void OnCrawlSlowdownInit(EntityUid uid, CrawlingComponent component, ComponentInit args)
    {
        _movementSpeedModifier.RefreshMovementSpeedModifiers(uid);
    }
    private void OnCrawlSlowRemove(EntityUid uid, CrawlingComponent component, ComponentShutdown args)
    {
        component.SprintSpeedModifier = 1f;
        component.WalkSpeedModifier = 1f;
        _movementSpeedModifier.RefreshMovementSpeedModifiers(uid);
    }
    private void OnRefreshMovespeed(EntityUid uid, CrawlingComponent component, RefreshMovementSpeedModifiersEvent args)
    {
        args.ModifySpeed(component.WalkSpeedModifier, component.SprintSpeedModifier);
    }
}

[Serializable, NetSerializable]
public sealed partial class CrawlStandupDoAfterEvent : SimpleDoAfterEvent
{
}

