// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Server.Antag;
using Content.Server.GameTicking.Rules.Components;
using Robust.Shared.Timing;
using Content.Server.RoundEnd;
using Content.Shared.DeadSpace.Necromorphs.Necroobelisk;
using Content.Server.Audio;
using Content.Shared.Audio;
using Robust.Shared.Map;
using Content.Shared.GameTicking.Components;
using Robust.Shared.Prototypes;
using Content.Shared.Roles;
using Content.Server.Antag.Components;
using System.Linq;
using Content.Server.Roles;
using Content.Server.Mind;
using Content.Server.Explosion.EntitySystems;
using Content.Server.Chat.Systems;
using Content.Shared.DeadSpace.Necromorphs.Unitology.Components;
using Content.Shared.Objectives.Systems;
using Content.Shared.Damage;
using Content.Shared.Speech.Components;
using Robust.Shared.Audio.Systems;
using Content.Shared.DeadSpace.Necromorphs.InfectionDead.Components;
using Content.Shared.Zombies;
using Content.Server.DeadSpace.Necromorphs.InfectionDead;
using Content.Shared.Stunnable;
using Robust.Shared.Random;

namespace Content.Server.GameTicking.Rules;

public sealed class UnitologyRuleSystem : GameRuleSystem<UnitologyRuleComponent>
{
    [Dependency] private readonly RoleSystem _role = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly RoundEndSystem _roundEnd = default!;
    [Dependency] private readonly ServerGlobalSoundSystem _sound = default!;
    [Dependency] private readonly AntagSelectionSystem _antag = default!;
    [Dependency] private readonly MindSystem _mindSystem = default!;
    [Dependency] private readonly ChatSystem _chatSystem = default!;
    [Dependency] private readonly ExplosionSystem _explosion = default!;
    [Dependency] private readonly SharedObjectivesSystem _objectives = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly InfectionDeadSystem _infectionDead = default!;
    [Dependency] private readonly NecromorfSystem _necromorfSystem = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    private static readonly EntProtoId UnitologyRule = "Unitology";
    public static readonly ProtoId<AntagPrototype> UnitologyAntagRole = "UniHead";

    private const float ConvergenceSongLength = 60f + 37.6f;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<UnitologyRuleComponent, AfterAntagEntitySelectedEvent>(AfterEntitySelected);
        SubscribeLocalEvent<UnitologyRuleComponent, StageObeliskEvent>(OnStageObelisk);
        SubscribeLocalEvent<UnitologyRuleComponent, EndStageConvergenceEvent>(EndStageConvergence);
        SubscribeLocalEvent<UnitologyRuleComponent, StageConvergenceEvent>(OnStageConvergence);
    }

    protected override void Started(EntityUid uid, UnitologyRuleComponent component, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        base.Started(uid, component, gameRule, args);

        component.TimeUntilArrivalObelisk = _timing.CurTime + TimeSpan.FromMinutes(component.DurationArrivalObelisk);
    }

    protected override void ActiveTick(EntityUid uid, UnitologyRuleComponent component, GameRuleComponent gameRule, float frameTime)
    {
        base.ActiveTick(uid, component, gameRule, frameTime);

        var time = component.TimeUntilArrivalObelisk;
        float minutes = (float)time.TotalMinutes;
        float seconds = (float)time.TotalSeconds;

        TimeSpan warningTime = TimeSpan.FromMinutes(minutes - component.TimeUntilWarning);
        TimeSpan spawnObeliskTime = TimeSpan.FromMinutes(seconds + component.TimeAfterTheExplosion);

        if (component.IsStageObelisk && component.TimeUtilStopTransformations > _timing.CurTime)
        {
            VictimTransformations(uid, component);
        }

        if (!component.IsTransformationEnd && component.IsStageObelisk && component.TimeUtilStopTransformations < _timing.CurTime)
        {
            EndTransformations(uid, component);
        }

        if (!component.IsWarningSend && warningTime < _timing.CurTime)
        {
            _chatSystem.DispatchGlobalAnnouncement(Loc.GetString("unitology-centcomm-announcement-obelisk-arrival"), playSound: true, colorOverride: Color.LightSeaGreen);
            component.IsWarningSend = true;
        }

        if (!component.IsObeliskArrival && component.TimeUntilArrivalObelisk < _timing.CurTime)
        {
            float multyExp = IsConditionsComplete() ? 2f : 1f;

            EntityCoordinates? landingSite = GetCoord(uid, component);

            if (landingSite == null)
                return;

            var expCoords = _transform.ToMapCoordinates(landingSite.Value);

            if (!component.ThisExplosionMade)
            {
                _explosion.QueueExplosion(expCoords, component.TypeId, component.TotalIntensity * multyExp, 1f, component.MaxTileIntensity * multyExp, null, 1f, int.MaxValue, true, true);
                component.ThisExplosionMade = true;
            }

            if (spawnObeliskTime < _timing.CurTime)
                return;

            EntityUid? obelisk = null;

            if (IsConditionsComplete())
                obelisk = Spawn(component.BlackObeliskPrototype, landingSite.Value);
            else
                obelisk = Spawn(component.ObeliskPrototype, landingSite.Value);

            if (obelisk == null)
                return;

            var stageObeliskEvent = new StageObeliskEvent(obelisk.Value);
            var ruleQuery = AllEntityQuery<UnitologyRuleComponent>();
            while (ruleQuery.MoveNext(out var ruleUid, out _))
            {
                RaiseLocalEvent(ruleUid, ref stageObeliskEvent);
            }

            component.IsObeliskArrival = true;
        }

        if (component.IsStageObelisk)
        {
            if (_timing.CurTime >= component.NextStageTime - TimeSpan.FromSeconds(ConvergenceSongLength) && !component.PlayedConvergenceSong)
            {
                var query = AllEntityQuery<NecroobeliskComponent>();
                while (query.MoveNext(out var uidObelisk, out _))
                {
                    _sound.DispatchStationEventMusic(uidObelisk, component.ConvergenceMusic, StationEventMusicType.Convergence);
                    component.PlayedConvergenceSong = true;
                }
            }

            if (_timing.CurTime >= component.NextStageTime)
            {
                var convergenceRuleEvent = new StageConvergenceEvent();
                RaiseLocalEvent(uid, ref convergenceRuleEvent);
            }
        }

        if (component.IsEndConvergence)
        {
            if (_timing.CurTime >= component.NextStageTime)
            {
                _roundEnd.EndRound();
            }
        }

        return;
    }

    public bool IsConditionsComplete()
    {
        bool isConditionsComplete = true;

        var query = EntityQueryEnumerator<UnitologyHeadComponent>();

        while (query.MoveNext(out var ent, out var component))
        {
            if (!_mindSystem.TryGetMind(ent, out var mindId, out var mind))
                continue;

            if (mind == null)
                continue;

            foreach (var objId in mind.Objectives)
            {
                if (!_objectives.IsCompleted(objId, (mindId, mind)))
                {
                    isConditionsComplete = false;
                    break;
                }

            }
        }

        return isConditionsComplete;
    }

    private EntityCoordinates? GetCoord(EntityUid uid, UnitologyRuleComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return null;

        if (component.ObeliskCoords != null)
            return component.ObeliskCoords.Value;

        var query = AllEntityQuery<UnitologyLighthouseComponent>();
        while (query.MoveNext(out var ent, out _))
        {
            component.ObeliskCoords = Transform(ent).Coordinates;
            break;
        }

        if (component.ObeliskCoords == null)
        {
            if (TryFindRandomTile(out _, out _, out _, out var coords))
            {
                component.ObeliskCoords = coords;
            }
        }

        return component.ObeliskCoords;
    }

    private void AfterEntitySelected(Entity<UnitologyRuleComponent> ent, ref AfterAntagEntitySelectedEvent args)
    {
        var antag = _antag.ForceGetGameRuleEnt<UnitologyRuleComponent>(UnitologyRule);

        AntagSelectionDefinition? definition = antag.Comp.Definitions.FirstOrDefault(def =>
        def.PrefRoles.Contains(new ProtoId<AntagPrototype>(UnitologyAntagRole))
        );

        if (!_mindSystem.TryGetMind(args.EntityUid, out var mindId, out var mind))
            return;

        var roles = _role.MindGetAllRoleInfo(mindId);
        var headRoles = roles.Where(x => x.Prototype == UnitologyAntagRole);

        if (headRoles.Count() < 0)
            _antag.MakeAntag(args.GameRule, args.Session, definition.Value);
    }

    private void OnStageObelisk(EntityUid uid, UnitologyRuleComponent component, StageObeliskEvent ev)
    {
        component.Obelisk = ev.Obelisk;
        component.NextStageTime = _timing.CurTime + component.StageObeliskDuration;
        component.TimeUtilStopTransformations = _timing.CurTime + TimeSpan.FromSeconds(component.DurationTransformations);
        component.IsStageObelisk = true;
    }

    private void EndTransformations(EntityUid uid, UnitologyRuleComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        var query = EntityQueryEnumerator<UnitologyHeadComponent>();
        var queryUni = EntityQueryEnumerator<UnitologyComponent>();
        var queryEnsl = EntityQueryEnumerator<UnitologyEnslavedComponent>();

        while (query.MoveNext(out var uniUid, out _))
        {
            if (HasComp<NecromorfComponent>(uniUid) || HasComp<ZombieComponent>(uniUid))
                continue;

            _necromorfSystem.Necrofication(uniUid, component.AfterGibNecroPrototype, new InfectionDeadStrainData());
        }

        while (queryUni.MoveNext(out var uniUid, out _))
        {
            if (HasComp<NecromorfComponent>(uniUid) || HasComp<ZombieComponent>(uniUid))
                continue;

            var necromorf = _infectionDead.GetRandomNecromorfPrototypeId();

            _necromorfSystem.Necrofication(uniUid, necromorf, new InfectionDeadStrainData());
        }

        while (queryEnsl.MoveNext(out var uniUid, out _))
        {
            if (HasComp<NecromorfComponent>(uniUid) || HasComp<ZombieComponent>(uniUid))
                continue;

            var necromorf = _infectionDead.GetRandomNecromorfPrototypeId();

            _necromorfSystem.Necrofication(uniUid, necromorf, new InfectionDeadStrainData());
        }

        component.IsTransformationEnd = true;
    }

    private void VictimTransformations(EntityUid uid, UnitologyRuleComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        if (component.DamageTick > _timing.CurTime)
            return;

        var query = EntityQueryEnumerator<UnitologyHeadComponent>();
        var queryUni = EntityQueryEnumerator<UnitologyComponent>();
        var queryEnsl = EntityQueryEnumerator<UnitologyEnslavedComponent>();

        while (query.MoveNext(out var uniUid, out _))
        {
            if (HasComp<NecromorfComponent>(uniUid) || HasComp<ZombieComponent>(uniUid))
                continue;

            VictimDamage(uid, uniUid, component);
        }

        while (queryUni.MoveNext(out var uniUid, out _))
        {
            if (HasComp<NecromorfComponent>(uniUid) || HasComp<ZombieComponent>(uniUid))
                continue;

            VictimDamage(uid, uniUid, component);
        }

        while (queryEnsl.MoveNext(out var uniUid, out _))
        {
            if (HasComp<NecromorfComponent>(uniUid) || HasComp<ZombieComponent>(uniUid))
                continue;

            VictimDamage(uid, uniUid, component);
        }

        component.DamageTick = _timing.CurTime + TimeSpan.FromSeconds(1f);
    }

    public void VictimDamage(EntityUid uid, EntityUid target, UnitologyRuleComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        if (!TryComp<DamageableComponent>(target, out var damageable))
            return;

        _damageable.TryChangeDamage(target, component.Damage, false, false, damageable);
        _stun.TryUpdateParalyzeDuration(target, TimeSpan.FromSeconds(2f));

        if (TryComp<VocalComponent>(target, out var vocal))
        {
            int chance = _random.Next(0, 5);

            if (vocal.EmoteSounds is not { } sounds)
                return;

            if (chance < 1)
            {
                _chatSystem.TryPlayEmoteSound(target, _proto.Index(sounds), "Crying");
            }
            else
            {
                _chatSystem.TryPlayEmoteSound(target, _proto.Index(sounds), "Scream");
            }
        }

        if (component.TransformationsSound != null)
            _audio.PlayPvs(component.TransformationsSound, uid);
    }

    private void OnStageConvergence(EntityUid uid, UnitologyRuleComponent component, StageConvergenceEvent ev)
    {
        var convergenceEvent = new NecroobeliskStartConvergenceEvent();

        component.IsStageObelisk = false;
        component.IsStageConvergence = true;

        RaiseLocalEvent(component.Obelisk, ref convergenceEvent);
    }

    private void EndStageConvergence(EntityUid uid, UnitologyRuleComponent component, EndStageConvergenceEvent ev)
    {
        component.IsEndConvergence = true;
        component.NextStageTime = _timing.CurTime + component.StageConvergenceDuration;
    }

    protected override void AppendRoundEndText(EntityUid uid, UnitologyRuleComponent component, GameRuleComponent gameRule,
        ref RoundEndTextAppendEvent args)
    {
        base.AppendRoundEndText(uid, component, gameRule, ref args);

        var index = 0;

        if (component.IsStageObelisk)
        {
            index = 1;
        }
        if (!component.IsStageObelisk && component.IsStageConvergence)
        {
            index = 1;
        }
        if (component.IsEndConvergence)
        {
            index = 2;
        }
        args.AddLine(Loc.GetString(Outcomes[index]));

        var sessionData = _antag.GetAntagIdentifiers(uid);
        args.AddLine(Loc.GetString("uni-initial-count", ("initialCount", sessionData.Count)));
        foreach (var (mind, data, name) in sessionData)
        {
            args.AddLine(Loc.GetString("uni-initial-name-user",
                ("name", name),
                ("username", data.UserName)));
        }

    }

    private static readonly string[] Outcomes =
    {
        "uni-lost",
        "uni-obelisk",
        "uni-convergence",
    };
}
