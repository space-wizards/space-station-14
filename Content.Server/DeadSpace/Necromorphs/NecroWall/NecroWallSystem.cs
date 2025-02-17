// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using System.Linq;
using Content.Shared.DeadSpace.Necromorphs.NecroWall.Components;
using Robust.Shared.Timing;
using Content.Shared.Tag;
using Content.Shared.DeadSpace.Necromorphs.NecroWall;
using Content.Shared.Damage;
using Content.Server.DeadSpace.Necromorphs.NecroWall.Components;

namespace Content.Server.DeadSpace.Necromorphs.NecroWall;

public sealed class NecroWallSystem : EntitySystem
{
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly TagSystem _tags = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    private const float DurationActive = 5f;
    private const float Duration = 60f;
    private const float Range = 1f;
    private const float MaxLvlStage = 100f;
    private const string WallTag = "Wall";
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<NecroKudzuComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<NecroWallComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<NecroWallComponent, CaptureWallEvent>(OnCaptureWall);
        SubscribeLocalEvent<NecroWallComponent, DamageChangedEvent>(OnDamageChanged);
    }
    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var necroWallQuery = EntityQueryEnumerator<NecroWallComponent>();
        while (necroWallQuery.MoveNext(out var ent, out var necroWall))
        {
            if (_gameTiming.CurTime > necroWall.NextTickUtilRegen)
            {
                Regen(ent, necroWall);
            }

            if (_gameTiming.CurTime > necroWall.NextTick && necroWall.WallIsCaptured)
            {
                CaptureWall(ent, necroWall);
            }
        }

        var necroKudzuQuery = EntityQueryEnumerator<NecroKudzuComponent>();
        while (necroKudzuQuery.MoveNext(out var ent, out var necroKudzu))
        {
            if (_gameTiming.CurTime > necroKudzu.NextTick)
            {
                KudzuCaptureWall(ent, necroKudzu);
            }
        }
    }
    private void OnDamageChanged(EntityUid uid, NecroWallComponent component, DamageChangedEvent args)
    {
        component.LvlStage -= (float)args.Damageable.TotalDamage;
        UpdateNecroWall(uid, component);
    }
    private void OnMapInit(EntityUid uid, NecroKudzuComponent component, MapInitEvent args)
    {
        component.NextTick = _gameTiming.CurTime + TimeSpan.FromSeconds(component.Duration);
    }
    private void OnComponentInit(EntityUid uid, NecroWallComponent component, ComponentInit args)
    {
        component.NextTick = _gameTiming.CurTime + TimeSpan.FromSeconds(component.IsActive ? DurationActive : Duration);
        component.NextTickUtilRegen = _gameTiming.CurTime + TimeSpan.FromSeconds(1f);
        component.WallIsCaptured = true;
        CaptureWall(uid, component);
    }
    private void Regen(EntityUid uid, NecroWallComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        if (component.LvlStage < MaxLvlStage)
            component.LvlStage += component.Regen;

        UpdateNecroWall(uid, component);
        component.NextTickUtilRegen = _gameTiming.CurTime + TimeSpan.FromSeconds(1f);
    }
    private HashSet<EntityUid> GetValidWalls(EntityUid uid, float range)
    {
        var ents = _lookup.GetEntitiesInRange(_transform.GetMapCoordinates(uid, Transform(uid)), range).ToList();
        var validEntities = ents
            .Where(ent => _tags.HasTag(ent, WallTag) && !HasComp<InfestedDeadWallComponent>(ent))
            .ToList();

        return new HashSet<EntityUid>(validEntities);
    }

    private void SpawnNecroWall(EntityUid wall, string necroWallId)
    {
        var necroWallEntity = Spawn(necroWallId, Transform(wall).Coordinates);
        var infestedDeadWallComponent = AddComp<InfestedDeadWallComponent>(wall);
        infestedDeadWallComponent.NecroWallEntity = necroWallEntity;
    }

    private void OnCaptureWall(EntityUid uid, NecroWallComponent component, CaptureWallEvent args)
    {
        float normalizedLevel = component.LvlStage / MaxLvlStage;

        if (normalizedLevel < 0.75f)
            return;

        var walls = GetValidWalls(uid, Range);

        if (walls.Count > 0)
        {
            component.IsActive = true;
            var wall = walls.FirstOrDefault();
            SpawnNecroWall(wall, component.NecroWallId);
        }
        else
        {
            component.IsActive = false;
        }

        component.NextTick = _gameTiming.CurTime + TimeSpan.FromSeconds(component.IsActive ? DurationActive : Duration);
        component.WallIsCaptured = true;
        CaptureWall(uid, component);
    }

    private void CaptureWall(EntityUid uid, NecroWallComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        if (!component.WallIsCaptured)
            return;

        if (!EntityManager.EntityExists(uid))
            return;

        var ev = new CaptureWallEvent();
        RaiseLocalEvent(uid, ref ev);

        component.WallIsCaptured = false;
    }

    private void KudzuCaptureWall(EntityUid uid, NecroKudzuComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        var walls = GetValidWalls(uid, Range);

        if (walls.Count > 0)
        {
            var wall = walls.FirstOrDefault();
            SpawnNecroWall(wall, component.NecroWallId);
        }

        component.NextTick = _gameTiming.CurTime + TimeSpan.FromSeconds(Duration);
    }

    public void UpdateNecroWall(EntityUid uid, NecroWallComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        if (component.LvlStage <= 0f)
        {
            if (component.WallEntity != null && EntityManager.EntityExists(component.WallEntity))
            {
                if (HasComp<InfestedDeadWallComponent>(component.WallEntity))
                    RemComp<InfestedDeadWallComponent>(component.WallEntity.Value);
            }
            QueueDel(uid);
            return;
        }

        float normalizedLevel = component.LvlStage / MaxLvlStage;

        NecroWallVisuals currentStage;
        if (normalizedLevel >= 0.75f)
            currentStage = NecroWallVisuals.Stage4;
        else if (normalizedLevel >= 0.50f)
            currentStage = NecroWallVisuals.Stage3;
        else if (normalizedLevel >= 0.25f)
            currentStage = NecroWallVisuals.Stage2;
        else
            currentStage = NecroWallVisuals.Stage1;

        foreach (NecroWallVisuals stage in Enum.GetValues(typeof(NecroWallVisuals)))
        {
            _appearance.SetData(uid, stage, stage == currentStage);
        }
    }


}
