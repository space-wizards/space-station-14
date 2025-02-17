// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Shared.Actions;
using Content.Shared.DeadSpace.Demons.DemonShadow.Components;
using Content.Shared.DeadSpace.Demons.DemonShadow;
using Content.Shared.Physics;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Timing;
using Robust.Shared.Audio;
using Content.Shared.Mobs.Components;
using Content.Server.Beam;
using Robust.Server.GameObjects;
using Content.Server.GameTicking;
using Robust.Shared.Physics.Systems;
using Content.Shared.Movement.Systems;
using Robust.Shared.Physics;
using System.Linq;
using Content.Shared.Eye;
using Content.Shared.Stunnable;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.Examine;
using Robust.Shared.Map;
using Content.Shared.Damage;
using Content.Shared.Mobs.Systems;
using Content.Shared.Maps;
using Content.Shared.Popups;
using Content.Shared.NPC.Systems;
using Content.Shared.NPC.Components;
using Content.Shared.NPC.Prototypes;
using Robust.Shared.Prototypes;
using Content.Shared.DeadSpace.Abilities.Cocoon;
using Content.Shared.Interaction;
using Content.Server.DeadSpace.Abilities.Cocoon;

namespace Content.Server.DeadSpace.Demons.DemonShadow;

public sealed class DemonShadowSystem : SharedDemonShadowSystem
{
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;
    [Dependency] private readonly BeamSystem _beam = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly VisibilitySystem _visibility = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _movement = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly ExamineSystemShared _examine = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly NpcFactionSystem _faction = default!;
    [Dependency] private readonly SharedInteractionSystem _interaction = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DemonShadowComponent, ShadowCrawlActionEvent>(DoShadowCrawl);
        SubscribeLocalEvent<DemonShadowComponent, ShadowGrappleEvent>(DoShadowGrapple);
        SubscribeLocalEvent<DemonShadowComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<DemonShadowComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<DemonShadowComponent, ComponentShutdown>(OnComponentShutdown);
        SubscribeLocalEvent<DemonShadowComponent, MeleeHitEvent>(OnMeleeHit);
        SubscribeLocalEvent<DemonShadowComponent, LockCocoonEvent>(OnLockCocoon, before: new[] { typeof(LockCocoonSystem) });

        SubscribeLocalEvent<RoundEndTextAppendEvent>(_ => MakeVisible(true));
    }

    private void OnLockCocoon(EntityUid uid, DemonShadowComponent component, LockCocoonEvent args)
    {
        if (component.IsShadowCrawl)
        {
            _popup.PopupEntity(Loc.GetString("Вы не можете применить эту способность в астрале."), uid, uid);
            args.Handled = true;
            return;
        }
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var demonShadow = EntityQueryEnumerator<DemonShadowComponent>();
        while (demonShadow.MoveNext(out var uid, out var component))
        {
            if (_gameTiming.CurTime > component.TimeToCheck)
                ShadowCheck(uid, component);

            if (component.NextTickForRegen + TimeSpan.FromSeconds(1) < _gameTiming.CurTime)
                Regeneration(uid, component);

            if (_gameTiming.CurTime >= component.TimeUtilTeleport && component.TeleportTarget != null)
                Teleport(uid, component.TeleportTarget.Value, component);

            if (_gameTiming.CurTime >= component.TimeUtilShadowCrawl && component.IsStartShadowCrawl)
                Crawl(uid, component);
        }
    }

    private void Regeneration(EntityUid uid, DemonShadowComponent component)
    {
        component.NextTickForRegen = _gameTiming.CurTime;

        if (!TryComp<MobStateComponent>(uid, out var mobStateComponent))
            return;

        if (!TryComp<DamageableComponent>(uid, out var damageableComponent))
            return;

        if (_mobState.IsDead(uid, mobStateComponent))
            return;

        var multiplier = component.IsShadowPosition
            ? component.PassiveHealingMultiplier
            : 1f;

        _damageable.TryChangeDamage(uid, component.PassiveHealing * multiplier, true, false, damageableComponent);
    }

    public void ShadowCheck(EntityUid uid, DemonShadowComponent component)
    {
        MapCoordinates lightPosition;
        MapCoordinates entityPosition = _transform.GetMapCoordinates(uid);

        var pointLightQuery = EntityQueryEnumerator<PointLightComponent, TransformComponent>();

        while (pointLightQuery.MoveNext(out var ent, out var lightComp, out var xform))
        {
            if (Transform(uid).MapID != xform.MapID)
                continue;

            lightPosition = _transform.GetMapCoordinates(ent);

            if (_examine.InRangeUnOccluded(entityPosition, lightPosition, lightComp.Radius, null) && lightComp.Enabled)
            {
                component.IsShadowPosition = false;
                _appearance.SetData(uid, DemonShadowVisuals.Hide, false);
                component.TimeToCheck = _gameTiming.CurTime + component.CheckDuration;
                component.MovementSpeedMultiply = 1f;
                _movement.RefreshMovementSpeedModifiers(uid);
                return;
            }
        }

        component.IsShadowPosition = true;
        _appearance.SetData(uid, DemonShadowVisuals.Hide, true);
        component.MovementSpeedMultiply = 3f;
        _movement.RefreshMovementSpeedModifiers(uid);
        component.TimeToCheck = _gameTiming.CurTime + component.CheckDuration;

        return;
    }

    private void OnStartup(EntityUid uid, DemonShadowComponent component, ComponentStartup args)
    {
        _appearance.SetData(uid, DemonShadowVisuals.Astral, false);
        _appearance.SetData(uid, DemonShadowVisuals.Hide, false);
        _appearance.SetData(uid, DemonShadowVisuals.DemonShadow, true);

        if (TryComp<NpcFactionMemberComponent>(uid, out var factionComp))
            component.OldFaction = GetFirstElement(factionComp.Factions);

        Astral(uid, true);
    }

    private void OnComponentInit(EntityUid uid, DemonShadowComponent component, ComponentInit args)
    {
        _actionsSystem.AddAction(uid, ref component.DemonShadowCrawlActionEntity, component.DemonShadowCrawl, uid);
        _actionsSystem.AddAction(uid, ref component.DemonShadowGrappleActionEntity, component.DemonShadowGrapple, uid);
    }

    private void OnMeleeHit(EntityUid uid, DemonShadowComponent component, MeleeHitEvent args)
    {
        if (component.IsShadowCrawl)
            args.Handled = true;
    }

    private void OnComponentShutdown(EntityUid uid, DemonShadowComponent component, ComponentShutdown args)
    {
        _actionsSystem.RemoveAction(uid, component.DemonShadowCrawlActionEntity);

        Astral(uid, false);

        component.MovementSpeedMultiply = 1;
        _movement.RefreshMovementSpeedModifiers(uid);
    }

    private void DoShadowCrawl(EntityUid uid, DemonShadowComponent component, ShadowCrawlActionEvent args)
    {
        if (args.Handled)
            return;

        if (!TryUseShadowCrawl(uid))
            return;

        args.Handled = true;

        _actionsSystem.SetEnabled(component.DemonShadowCrawlActionEntity, false);
        _appearance.SetData(uid, DemonShadowVisuals.Astral, true);
        _audio.PlayPvs("/Audio/_DeadSpace/Demons/shadow.ogg", uid, AudioParams.Default.WithVolume(1f));

        component.IsStartShadowCrawl = true;
        component.TimeUtilShadowCrawl = _gameTiming.CurTime + component.ShadowCrawlDuration;
    }

    private bool TryUseShadowCrawl(EntityUid uid)
    {
        var tileref = Transform(uid).Coordinates.GetTileRef();
        if (tileref != null)
        {
            if (_physics.GetEntitiesIntersectingBody(uid, (int) CollisionGroup.Impassable).Count > 0)
            {
                _popup.PopupEntity(Loc.GetString("revenant-in-solid"), uid, uid);
                return false;
            }
        }

        return true;
    }

    private void Crawl(EntityUid uid, DemonShadowComponent component)
    {
        component.IsStartShadowCrawl = false;
        _actionsSystem.SetEnabled(component.DemonShadowCrawlActionEntity, true);

        if (!TryUseShadowCrawl(uid))
        {
            _appearance.SetData(uid, DemonShadowVisuals.Astral, false);
            _actionsSystem.ClearCooldown(component.DemonShadowCrawlActionEntity);
            return;
        }

        _appearance.SetData(uid, DemonShadowVisuals.Astral, false);

        component.IsShadowCrawl = !component.IsShadowCrawl;

        if (component.IsShadowCrawl)
        {
            Astral(uid, true);
        }
        else
        {
            Astral(uid, false);
        }
    }

    private void ToggleFixtures(EntityUid uid, bool isHasFixture)
    {
        if (!isHasFixture)
        {
            if (TryComp<FixturesComponent>(uid, out var fixtures) && fixtures.FixtureCount >= 1)
            {
                var fixture = fixtures.Fixtures.First();

                _physics.SetCollisionMask(uid, fixture.Key, fixture.Value, (int) CollisionGroup.None, fixtures);
                _physics.SetCollisionLayer(uid, fixture.Key, fixture.Value, 0, fixtures);
            }
        }
        else
        {
            if (TryComp<FixturesComponent>(uid, out var fixtures) && fixtures.FixtureCount >= 1)
            {
                var fixture = fixtures.Fixtures.First();

                _physics.SetCollisionMask(uid, fixture.Key, fixture.Value, (int) (CollisionGroup.SmallMobMask | CollisionGroup.GhostImpassable), fixtures);
                _physics.SetCollisionLayer(uid, fixture.Key, fixture.Value, (int) CollisionGroup.SmallMobLayer, fixtures);
            }
        }
    }

    private void ToggleVisible(EntityUid uid, bool visible)
    {
        if (!TryComp<VisibilityComponent>(uid, out var visibleComponent))
            return;

        if (visible)
        {
            _visibility.AddLayer((uid, visibleComponent), (int) VisibilityFlags.Normal, false);
            _visibility.RemoveLayer((uid, visibleComponent), (int) VisibilityFlags.Ghost, false);
        }
        else
        {
            _visibility.AddLayer((uid, visibleComponent), (int) VisibilityFlags.Ghost, false);
            _visibility.RemoveLayer((uid, visibleComponent), (int) VisibilityFlags.Normal, false);
        }

        _visibility.RefreshVisibility(uid, visibleComponent);
    }

    private void Astral(EntityUid uid, bool isAstral)
    {
        if (!TryComp<DemonShadowComponent>(uid, out var component))
            return;

        if (isAstral)
        {
            _actionsSystem.SetEnabled(component.DemonShadowGrappleActionEntity, false);
            ToggleVisible(uid, false);
            ToggleFixtures(uid, false);

            _faction.ClearFactions(uid, dirty: false);
        }
        else
        {
            _actionsSystem.SetEnabled(component.DemonShadowGrappleActionEntity, true);
            ToggleVisible(uid, true);
            ToggleFixtures(uid, true);

            if (component.OldFaction != null)
            {
                _faction.AddFaction(uid, component.OldFaction);
            }
            else
            {
                Logger.Warning($"OldFaction для сущности {uid} равен null.");
            }
        }
    }

    static ProtoId<NpcFactionPrototype>? GetFirstElement(HashSet<ProtoId<NpcFactionPrototype>> set)
    {
        foreach (var element in set)
        {
            return element; // Возвращаем первый элемент, который найдем
        }

        return null;
    }

    private void DoShadowGrapple(EntityUid uid, DemonShadowComponent component, ShadowGrappleEvent args)
    {
        if (!_gameTiming.IsFirstTimePredicted)
            return;

        if (args.Handled)
            return;

        var user = args.Performer;
        var target = args.Target;

        if (!TryComp<MobStateComponent>(target, out var stateComponent))
            return;

        if (!_interaction.InRangeUnobstructed(user, target, 0f, CollisionGroup.BulletImpassable))
        {
            _popup.PopupEntity(Loc.GetString("Не могу пройти через препятствие!"), uid, uid);
            return;
        }

        args.Handled = true;

        _beam.TryCreateBeam(uid, target, "ShadowHand");
        _stun.TryParalyze(target, TimeSpan.FromSeconds(3), true);

        component.TeleportTarget = target;
        component.TimeUtilTeleport = _gameTiming.CurTime + component.TeleportDuration;
    }

    private void Teleport(EntityUid uid, EntityUid target, DemonShadowComponent component)
    {
        component.TeleportTarget = null;
        _transform.SetCoordinates(target, Transform(uid).Coordinates);
        _transform.AttachToGridOrMap(target);
    }

    public void MakeVisible(bool visible)
    {
        var query = EntityQueryEnumerator<DemonShadowComponent, VisibilityComponent>();
        while (query.MoveNext(out var uid, out _, out var vis))
        {
            if (visible)
            {
                _visibility.AddLayer((uid, vis), (int) VisibilityFlags.Normal, false);
                _visibility.RemoveLayer((uid, vis), (int) VisibilityFlags.Ghost, false);
            }
            else
            {
                _visibility.AddLayer((uid, vis), (int) VisibilityFlags.Ghost, false);
                _visibility.RemoveLayer((uid, vis), (int) VisibilityFlags.Normal, false);
            }

            _visibility.RefreshVisibility(uid, vis);
        }
    }

}
