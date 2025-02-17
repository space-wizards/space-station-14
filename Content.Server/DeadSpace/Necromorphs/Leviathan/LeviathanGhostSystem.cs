// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Server.Popups;
using Content.Server.DeadSpace.Abilities.SpawnAbility;
using Content.Shared.DeadSpace.Abilities.SpawnAbility;
using Content.Shared.DeadSpace.Necromorphs.Leviathan.Components;
using Robust.Shared.Timing;
using Content.Shared.DeadSpace.Abilities.SpawnAbility.Components;
using Content.Shared.Mobs.Systems;

namespace Content.Server.DeadSpace.Necromorphs.Leviathan;

public sealed class LeviathanGhostSystem : EntitySystem
{
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    private const string Territory = "NecroKudzu";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<LeviathanGhostComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<LeviathanGhostComponent, SpawnPointActionEvent>(OnSpawnPointAction, before: new[] { typeof(CustomSpawnPointSystem) });
    }
    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var leviathanGhostQuery = EntityQueryEnumerator<LeviathanGhostComponent>();
        while (leviathanGhostQuery.MoveNext(out var ent, out var leviathanGhost))
        {
            if (_gameTiming.CurTime > leviathanGhost.NextTick)
            {
                CheckMaster(ent, leviathanGhost);
            }
        }
    }
    public void CheckMaster(EntityUid uid, LeviathanGhostComponent component)
    {
        if (component.MasterEntity == null)
            return;

        if (!EntityManager.EntityExists(component.MasterEntity))
            QueueDel(uid);

        if (_mobState.IsDead(component.MasterEntity.Value))
            QueueDel(uid);

        component.NextTick = _gameTiming.CurTime + TimeSpan.FromSeconds(1f);
    }
    private void OnComponentInit(EntityUid uid, LeviathanGhostComponent component, ComponentInit args)
    {
        component.NextTick = _gameTiming.CurTime + TimeSpan.FromSeconds(1f);
    }
    private void OnSpawnPointAction(EntityUid uid, LeviathanGhostComponent component, SpawnPointActionEvent args)
    {
        if (args.Handled)
            return;

        var ent = _lookup.GetEntitiesInRange<LeviathanTerritoryComponent>(args.Target, component.RangeTerritory);

        if (ent.Count <= 0)
        {
            TryComp<CustomSpawnPointComponent>(uid, out var spawnPoint);
            if (spawnPoint != null && spawnPoint.SelectEntity != Territory)
            {
                _popup.PopupEntity("Здесь нет подконтрольной вам территории!", uid, uid);
                args.Handled = true;
            }
        }
    }

}
