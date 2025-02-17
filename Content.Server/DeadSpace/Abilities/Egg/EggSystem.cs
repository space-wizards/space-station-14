using Content.Shared.DeadSpace.Abilities.Egg.Components;
using Robust.Shared.Timing;
using Robust.Shared.Audio.Systems;
using Content.Shared.Examine;
using Robust.Shared.Random;
using Content.Shared.Storage;
using Content.Shared.DeadSpace.Abilities.Egg;
using Content.Server.Ghost.Roles.Components;
using Content.Server.Ghost.Roles;
using Content.Shared.Mobs.Components;
using Content.Shared.Mind;
using Content.Shared.Damage;

namespace Content.Server.DeadSpace.Abilities.Egg;

public sealed partial class EggSystem : SharedEggSystem
{

    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly IRobustRandom _robustRandom = default!;
    [Dependency] private readonly SharedMindSystem _mindSystem = default!;
    [Dependency] private readonly DamageableSystem _damage = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EggComponent, EggSpawnEvent>(BeginSpawn);
        SubscribeLocalEvent<EggComponent, PlayEggSoundEvent>(PlayEggSound);
        SubscribeLocalEvent<EggComponent, ExaminedEvent>(OnExamine);
    }
    private void OnExamine(EntityUid uid, EggComponent component, ExaminedEvent args)
    {
        var time = _timing.CurTime - component.TimeUntilSpawn;
        double seconds = Math.Abs(time.TotalSeconds);
        int roundedSeconds = (int)Math.Round(seconds);

        if (HasComp<MobStateComponent>(uid))
        {
            if (args.Examiner == args.Examined)
            {
                args.PushMarkup(Loc.GetString($"Вы вылупитесь через [color=red]{roundedSeconds} секунд[/color]."));
            }
        }
        else
        {
            args.PushMarkup(Loc.GetString($"Вылупится через [color=red]{roundedSeconds} секунд[/color]."));
        }
    }

    private void BeginSpawn(EntityUid uid, EggComponent component, EggSpawnEvent args)
    {
        var entitys = component.SpawnedEntities;

        var spawnEntities = EntitySpawnCollection.GetSpawns(entitys, _robustRandom);
        var coords = Transform(uid).MapPosition;

        EntityUid popupEnt = default!;
        if (spawnEntities.Count <= 0)
        {
            RemComp<EggComponent>(uid);
            return;
        }

        foreach (var proto in spawnEntities)
        {
            popupEnt = Spawn(proto, coords.Offset(_robustRandom.NextVector2(0.25f)));
        }

        if (HasComp<MobStateComponent>(uid))
        {
            DamageSpecifier dspec = new();
            dspec.DamageDict.Add("Piercing", 200f);
            _damage.TryChangeDamage(uid, dspec, true, false);

            if (!_mindSystem.TryGetMind(uid, out var mindId, out var mind))
            {
                RemComp<EggComponent>(uid);
                return;
            }

            if (!EntityManager.TryGetComponent<GhostRoleComponent>(popupEnt, out var ghostRoleComponent))
            {
                _mindSystem.TransferTo(mindId, popupEnt);
                RemComp<EggComponent>(uid);
                return;
            }

            var id = ghostRoleComponent.Identifier;
            var session = mind.Session;

            if (session != null)
            {
                EntityManager.EntitySysManager.GetEntitySystem<GhostRoleSystem>().Takeover(session, id);
            }
            else
            {
                RemComp<EggComponent>(uid);
                return;
            }
        }
        else
        {
            QueueDel(uid);
        }

        RemComp<EggComponent>(uid);
        return;
    }

    private void PlayEggSound(EntityUid uid, EggComponent component, PlayEggSoundEvent args)
    {
        component.TimeUntilPlaySound = TimeSpan.FromSeconds(component.DurationPlayEggSound) + _timing.CurTime;

        if (component.EggSound == null)
            return;

        _audio.PlayPvs(component.EggSound, uid);
    }

}
