using System.Linq;
using Content.Server.Popups;
using Content.Shared.Spider;
using Content.Shared.Maps;
using Content.Shared.Mobs.Systems;
using Robust.Server.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Timing;
using Content.Shared._Starlight.Spider.Events;
using Content.Shared.Weapons.Melee.Events;

namespace Content.Server.Spider;

public sealed class SpiderSystem : SharedSpiderSystem
{
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly TurfSystem _turf = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;

    /// <summary>
    ///     A recycled hashset used to check turfs for spiderwebs.
    /// </summary>
    private HashSet<EntityUid> _webs = []; // Starlight-edit

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<SpiderComponent, SpiderWebActionEvent>(OnSpawnNet);
        SubscribeLocalEvent<SpiderComponent, MeleeHitEvent>(OnMeleeHit);
    }

    // Starlight-start
    public void OnMeleeHit(EntityUid uid, SpiderComponent component, ref MeleeHitEvent args)
    {
        if (component.CantBreakWeb && args.HitEntities.Any(EntityManager.HasComponent<SpiderWebObjectComponent>))
            args.BonusDamage = -args.BaseDamage;
    }
    // Starlight-end

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<SpiderComponent>();
        while (query.MoveNext(out var uid, out var spider))
        {
            spider.NextWebSpawn ??= _timing.CurTime + spider.WebSpawnCooldown;

            if (_timing.CurTime < spider.NextWebSpawn)
                continue;

            spider.NextWebSpawn += spider.WebSpawnCooldown;

            if (HasComp<ActorComponent>(uid)
                || _mobState.IsDead(uid)
                || !spider.SpawnsWebsAsNonPlayer)
                continue;

            var transform = Transform(uid);
            SpawnWeb((uid, spider), transform.Coordinates);
        }
    }

    private void OnSpawnNet(EntityUid uid, SpiderComponent component, SpiderWebActionEvent args)
    {
        if (args.Handled)
            return;

        var transform = Transform(uid);

        if (transform.GridUid == null)
        {
            _popup.PopupEntity(Loc.GetString("spider-web-action-nogrid"), args.Performer, args.Performer);
            return;
        }

        var result = SpawnWeb((uid, component), transform.Coordinates);

        if (result)
        {
            _popup.PopupEntity(Loc.GetString("spider-web-action-success"), args.Performer, args.Performer);
            args.Handled = true;
        }
        else
            _popup.PopupEntity(Loc.GetString("spider-web-action-fail"), args.Performer, args.Performer);
    }

    private bool SpawnWeb(Entity<SpiderComponent> ent, EntityCoordinates coords)
    {
        var result = false;

        // Spawn web in center
        if (!IsTileBlockedByWeb(coords) && ent.Comp.OnlyOneWebPerTile) // Starlight-edit
        {
            Spawn(ent.Comp.WebPrototype, coords);
            result = true;
        }

        if (ent.Comp.OneWebSpawn) // Starlight-edit: we spawn only one web in center
            return result;

        // Spawn web in other directions
        for (var i = 0; i < 4; i++)
        {
            var direction = (DirectionFlag)(1 << i);
            var outerSpawnCoordinates = coords.Offset(direction.AsDir().ToVec());

            if (IsTileBlockedByWeb(outerSpawnCoordinates) && ent.Comp.OnlyOneWebPerTile) // Starlight-edit
                continue;

            Spawn(ent.Comp.WebPrototype, outerSpawnCoordinates);
            result = true;
        }

        // Starlight-start
        if (result)
        {
            var ev = new SpiderWebSpawnedEvent();
            RaiseLocalEvent(ent.Owner, ev);
        }
        // Starlight-end

        return result;
    }

    private bool IsTileBlockedByWeb(EntityCoordinates coords)
    {
        _webs = _lookup.GetEntitiesIntersecting(coords); // Starlight-edit
        foreach (var entity in _webs)
        {
            if (HasComp<SpiderWebObjectComponent>(entity))
                return true;
        }
        return false;
    }
}
