using Content.Shared.Administration.Logs;
using Content.Shared.Database;
using Content.Shared.Hallucinations;
using Content.Shared.Humanoid;
using Content.Shared.StatusEffect;
using Robust.Server.GameObjects;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.Hallucinations;

public sealed partial class HallucinationsSystem : EntitySystem
{
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private readonly SharedEyeSystem _eye = default!;
    [Dependency] private readonly StatusEffectsSystem _status = default!;
    [Dependency] private readonly VisibilitySystem _visibilitySystem = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HallucinationsComponent, MapInitEvent>(OnHallucinationsInit);
        SubscribeLocalEvent<HallucinationsComponent, ComponentShutdown>(OnHallucinationsShutdown);
    }

    private void OnHallucinationsInit(EntityUid uid, HallucinationsComponent component, MapInitEvent args)
    {
        component.Layer = _random.Next(100, 150);
        if (!_entityManager.TryGetComponent<EyeComponent>(uid, out var eye))
            return;
        UpdatePreset(component);
        _eye.SetVisibilityMask(uid, eye.VisibilityMask | component.Layer, eye);
        _adminLogger.Add(LogType.Action, LogImpact.Medium,
        $"{ToPrettyString(uid):player} began to hallucinate.");
    }

    /// <summary>
    /// Updates hallucinations component settings to match prototype
    /// </summary>
    /// <param name="component">Active HallucinationsComponent</param>
    public void UpdatePreset(HallucinationsComponent component)
    {
        if (component.Proto == null)
            return;
        var preset = component.Proto;

        component.Spawns = preset.Entities;
        component.Range = preset.Range;
        component.SpawnRate = preset.SpawnRate;
        component.MinChance = preset.MinChance;
        component.MaxChance = preset.MaxChance;
        component.MaxSpawns = preset.MaxSpawns;
        component.IncreaseChance = preset.IncreaseChance;
    }
    private void OnHallucinationsShutdown(EntityUid uid, HallucinationsComponent component, ComponentShutdown args)
    {
        if (!_entityManager.TryGetComponent<EyeComponent>(uid, out var eye))
            return;
        _eye.SetVisibilityMask(uid, eye.VisibilityMask & ~component.Layer, eye);
        _adminLogger.Add(LogType.Action, LogImpact.Medium,
        $"{ToPrettyString(uid):player} stopped hallucinating.");
    }

    /// <summary>
    ///     Attempts to start hallucinations for target
    /// </summary>
    /// <param name="target">The target.</param>
    /// <param name="key">Status effect key.</param>
    /// <param name="time">Duration of hallucinations effect.</param>
    /// <param name="refresh">Refresh active effects.</param>
    /// <param name="proto">Hallucinations pack prototype.</param>
    public bool StartHallucinations(EntityUid target, string key, TimeSpan time, bool refresh, string proto)
    {
        if (proto == null)
            return false;
        if (!_proto.TryIndex<HallucinationsPrototype>(proto, out var prototype))
            return false;
        if (!_status.TryAddStatusEffect<HallucinationsComponent>(target, key, time, refresh))
            return false;

        var hallucinations = _entityManager.GetComponent<HallucinationsComponent>(target);
        hallucinations.Proto = prototype;
        UpdatePreset(hallucinations);
        hallucinations.CurChance = prototype.MinChance;

        return true;
    }
    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<HallucinationsComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var stat, out var xform))
        {
            if (_timing.CurTime < stat.NextSecond)
                continue;
            var rate = stat.SpawnRate;
            stat.NextSecond = _timing.CurTime + TimeSpan.FromSeconds(rate);

            if (stat.CurChance < stat.MaxChance && stat.CurChance + stat.IncreaseChance <= 1)
                stat.CurChance = stat.CurChance + stat.IncreaseChance;

            if (!_random.Prob(stat.CurChance))
                continue;

            stat.SpawnedCount = 0;

            var range = stat.Range * 4;

            // To be sure that entity will see right entities
            UpdatePreset(stat);

            // More people - worse hallucinations
            foreach (var (ent, comp) in _lookup.GetEntitiesInRange<HumanoidAppearanceComponent>(xform.MapPosition, range))
            {

                var newCoords = Transform(ent).MapPosition.Offset(_random.NextVector2(stat.Range));

                if (stat.SpawnedCount >= stat.MaxSpawns)
                    continue;
                stat.SpawnedCount = stat.SpawnedCount += 1;

                var hallucination = Spawn(_random.Pick(stat.Spawns), newCoords);
                EnsureComp<VisibilityComponent>(hallucination, out var visibility);
                _visibilitySystem.SetLayer(hallucination, visibility, (int) stat.Layer, false);
                _visibilitySystem.RefreshVisibility(hallucination, visibilityComponent: visibility);
            }

            // If there is no one... You are hallucinations source too
            var uidnewCoords = Transform(uid).MapPosition.Offset(_random.NextVector2(stat.Range));
            if (stat.SpawnedCount >= stat.MaxSpawns)
                continue;
            stat.SpawnedCount = stat.SpawnedCount += 1;

            var uidhallucination = Spawn(_random.Pick(stat.Spawns), uidnewCoords);
            EnsureComp<VisibilityComponent>(uidhallucination, out var uidvisibility);
            _visibilitySystem.SetLayer(uidhallucination, uidvisibility, (int) stat.Layer, false);
            _visibilitySystem.RefreshVisibility(uidhallucination, visibilityComponent: uidvisibility);

        }
    }

}
