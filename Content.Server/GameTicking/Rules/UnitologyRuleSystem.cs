// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Server.Antag;
using Content.Server.GameTicking.Rules.Components;
using Robust.Shared.Timing;
using Content.Server.RoundEnd;
using Content.Shared.DeadSpace.Necromorphs.Necroobelisk;
using Content.Server.Audio;
using Content.Shared.Audio;
using Content.Shared.Clothing;
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
using Content.Shared.Damage.Systems;
using Content.Shared.Speech.Components;
using Robust.Shared.Audio.Systems;
using Content.Shared.DeadSpace.Necromorphs.InfectionDead.Components;
using Content.Shared.Zombies;
using Content.Server.DeadSpace.Necromorphs.InfectionDead;
using Content.Shared.Stunnable;
using Content.Shared.Fax.Components;
using Content.Shared.Station.Components;
using Content.Server.Station.Systems;
using Content.Shared.Paper;
using Content.Server.Fax;
using Robust.Shared.Random;
using Content.Shared.Cargo.Prototypes;
using Content.Server.Cargo.Systems;
using Content.Shared.Cargo.Components;
using Content.Server.DeadSpace.ERT;
using Content.Server.AlertLevel;
using Content.Shared.DeadSpace.ERT.Prototypes;
using Content.Server.Database;
using Content.Server.DeadSpace.Necromorphs.Unitology;
using Content.Server.DeadSpace.Necromorphs.Unitology.Components;
using Content.Shared.Damage.Components;
using Robust.Server.Player;
using Robust.Shared.Player;
using Content.Shared.Humanoid;
using Content.Shared.Mind;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;

namespace Content.Server.GameTicking.Rules;

public sealed class UnitologyRuleSystem : GameRuleSystem<UnitologyRuleComponent>
{
    [Dependency] private readonly RoleSystem _role = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly RoundEndSystem _roundEnd = default!;
    [Dependency] private readonly ServerGlobalSoundSystem _sound = default!;
    [Dependency] private readonly AntagSelectionSystem _antag = default!;
    [Dependency] private readonly IServerDbManager _db = default!;
    [Dependency] private readonly MindSystem _mindSystem = default!;
    [Dependency] private readonly ChatSystem _chatSystem = default!;
    [Dependency] private readonly SharedObjectivesSystem _objectives = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly InfectionDeadSystem _infectionDead = default!;
    [Dependency] private readonly NecromorfSystem _necromorfSystem = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly StationSystem _station = default!;
    [Dependency] private readonly FaxSystem _faxSystem = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly LoadoutSystem _loadout = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly UnitologySubmissionConditionSystem _submissionCondition = default!;
    [Dependency] private readonly UnitologyAssemblyConditionSystem _assemblyCondition = default!;
    private static readonly EntProtoId UnitologyRule = "Unitology";
    public static readonly ProtoId<AntagPrototype> UnitologyAntagRole = "UniHead";
    public static readonly ProtoId<AntagPrototype> RegularUnitologyAntagRole = "Uni";
    public static readonly ProtoId<AntagPrototype> EnslavedUnitologyAntagRole = "UniEnslaved";
    private const float ConvergenceSongLength = 60f + 37.6f;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<UnitologyRuleComponent, StageObeliskEvent>(OnStageObelisk);
        SubscribeLocalEvent<UnitologyRuleComponent, SpawnNecroMoonEvent>(EndStageConvergence);
        SubscribeLocalEvent<UnitologyRuleComponent, StageConvergenceEvent>(OnStageConvergence);
    }

    public bool TryGrantUnitologyRole(EntityUid target, ProtoId<AntagPrototype> role, ICommonSession? session = null, bool forceCreateRule = true)
    {
        if (!_mindSystem.TryGetMind(target, out var mindId, out var mind))
            return false;

        session ??= _player.TryGetSessionById(mind.UserId, out var foundSession)
            ? foundSession
            : null;

        var ruleQuery = EntityQueryEnumerator<UnitologyRuleComponent, AntagSelectionComponent>();
        while (ruleQuery.MoveNext(out var ruleUid, out _, out var antagSelection))
        {
            if (HasComp<EndedGameRuleComponent>(ruleUid))
                continue;

            if (MetaData(ruleUid).EntityPrototype?.ID != UnitologyRule.Id)
                continue;

            if (!TryFindUnitologyDefinition(antagSelection, role, out var activeDefinition))
                return false;

            if (session != null)
            {
                _antag.MakeAntag((ruleUid, antagSelection), session, activeDefinition);
                return true;
            }

            break;
        }

        if (session != null && forceCreateRule)
        {
            var rule = _antag.ForceGetGameRuleEnt<UnitologyRuleComponent>(UnitologyRule);
            var antagSelection = Comp<AntagSelectionComponent>(rule.Owner);
            if (!TryFindUnitologyDefinition(antagSelection, role, out var activeDefinition))
                return false;

            _antag.MakeAntag((rule.Owner, antagSelection), session, activeDefinition);
            return true;
        }

        if (!_proto.TryIndex<EntityPrototype>(UnitologyRule, out var prototype)
            || !prototype.TryGetComponent<AntagSelectionComponent>(out var prototypeSelection, Factory)
            || !TryFindUnitologyDefinition(prototypeSelection, role, out var definition))
        {
            return false;
        }

        EntityManager.AddComponents(target, definition.Components);
        EntityManager.AddComponents(mindId, definition.MindComponents);

        var gear = new List<ProtoId<StartingGearPrototype>>();
        if (definition.StartingGear is not null)
            gear.Add(definition.StartingGear.Value);

        _loadout.Equip(target, gear, definition.RoleLoadout);
        _role.MindAddRoles(mindId, definition.MindRoles, mind, true);
        _antag.SendBriefing(session, definition.Briefing);
        return true;
    }

    private bool TryFindUnitologyDefinition(
        AntagSelectionComponent antagSelection,
        ProtoId<AntagPrototype> role,
        out AntagSelectionDefinition definition)
    {
        foreach (var antagDefinition in antagSelection.Definitions)
        {
            if (!antagDefinition.PrefRoles.Contains(role))
                continue;

            definition = antagDefinition;
            return true;
        }

        definition = default;
        return false;
    }

    protected override void ActiveTick(EntityUid uid, UnitologyRuleComponent component, GameRuleComponent gameRule, float frameTime)
    {
        base.ActiveTick(uid, component, gameRule, frameTime);

        if (component.IsStageObelisk && component.TimeUtilStopTransformations > _timing.CurTime)
        {
            VictimTransformations(uid, component);
        }

        if (!component.IsTransformationEnd && component.IsStageObelisk && component.TimeUtilStopTransformations < _timing.CurTime)
        {
            EndTransformations(uid, component);
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

    protected override void AppendAdminStatus(EntityUid uid,
        UnitologyRuleComponent component,
        GameRuleComponent gameRule,
        CollectGameRuleAdminStatusEvent args)
    {
        var heads = 0;
        var members = 0;
        var enslaved = 0;
        var lines = new List<string>();

        if (!TryComp<AntagSelectionComponent>(uid, out var selection))
            return;

        foreach (var mindId in _antag.GetAntagMindEntityUids((uid, selection)).ToHashSet())
        {
            if (!TryComp<MindComponent>(mindId, out var mind) ||
                mind.OwnedEntity is not { } body ||
                !_mobState.IsAlive(body))
            {
                continue;
            }

            if (HasComp<UnitologyEnslavedComponent>(body))
            {
                enslaved++;
                continue;
            }

            if (!HasComp<UnitologyHeadComponent>(body))
            {
                if (HasComp<UnitologyComponent>(body))
                    members++;

                continue;
            }

            heads++;
            foreach (var objective in mind.Objectives)
            {
                var progress = GetAdminObjectiveProgress(objective);
                lines.Add(Loc.GetString("game-rule-admin-status-unitology-objective",
                    ("head", ToPrettyString(body).Name ?? body.ToString()),
                    ("objective", MetaData(objective).EntityName),
                    ("progress", progress?.ToString("P0")
                        ?? Loc.GetString("game-rule-admin-status-unknown"))));
            }
        }

        var stage = component switch
        {
            { IsEndConvergence: true } => "end",
            { IsStageConvergence: true } => "convergence",
            { IsStageObelisk: true } => "obelisk",
            { IsObeliskArrival: true } => "arrival",
            _ => "objectives",
        };

        lines.Insert(0, Loc.GetString("game-rule-admin-status-unitology-summary",
            ("stage", Loc.GetString($"game-rule-admin-status-unitology-stage-{stage}")),
            ("heads", heads),
            ("members", members),
            ("enslaved", enslaved)));

        if (component.IsStageObelisk || component.IsEndConvergence)
        {
            var remaining = component.NextStageTime - _timing.CurTime;
            if (remaining < TimeSpan.Zero)
                remaining = TimeSpan.Zero;

            lines.Insert(1, Loc.GetString("game-rule-admin-status-unitology-countdown",
                ("time", remaining.ToString(@"mm\:ss"))));
        }

        if (lines.Count == 1)
            lines.Add(Loc.GetString("game-rule-admin-status-unitology-no-objectives"));

        args.AddSection(Loc.GetString("game-rule-admin-status-unitology-title"), lines);
    }

    private float? GetAdminObjectiveProgress(EntityUid objective)
    {
        if (TryComp<UnitologySubmissionConditionComponent>(objective, out var submission))
        {
            return _submissionCondition.TryGetAssignedTarget(objective, out var target)
                ? _submissionCondition.CalculateProgress(target)
                : null;
        }

        if (TryComp<UnitologyAssemblyConditionComponent>(objective, out var assembly))
        {
            return _assemblyCondition.TryGetProgress(objective, out var progress, assembly)
                ? progress
                : null;
        }

        return null;
    }

    public bool AreSummoningConditionsComplete(EntityUid head)
    {
        if (!TryGetRequiredSlaves(head, out var required))
            return false;

        var total = 0;
        var nearby = 0;
        var slaves = AllEntityQuery<UnitologyEnslavedComponent>();
        while (slaves.MoveNext(out var slave, out _))
        {
            total++;
            if (_transform.InRange(Transform(head).Coordinates, Transform(slave).Coordinates, 3f))
                nearby++;
        }

        return total >= required && nearby >= required && GetNearbyHumanCorpses(head).Count > 0;
    }

    private bool TryGetRequiredSlaves(EntityUid head, out int required)
    {
        required = 0;
        if (!_mindSystem.TryGetMind(head, out _, out var mind))
            return false;

        foreach (var objective in mind.Objectives)
        {
            if (_submissionCondition.TryGetAssignedTarget(objective, out required))
                return true;
        }

        return false;
    }

    public bool TrySummonObelisk(EntityUid head, bool black)
    {
        var rules = AllEntityQuery<UnitologyRuleComponent>();
        while (rules.MoveNext(out var ruleUid, out var rule))
        {
            if (rule.IsObeliskArrival || !AreSummoningConditionsComplete(head))
                return false;

            var prototype = black ? rule.BlackObeliskPrototype : rule.ObeliskPrototype;
            var obelisk = Spawn(prototype, Transform(head).Coordinates);
            foreach (var corpse in GetNearbyHumanCorpses(head))
            {
                var necromorf = _infectionDead.GetRandomNecromorfPrototypeId();
                _necromorfSystem.Necrofication(corpse, necromorf, new InfectionDeadStrainData());
            }

            var stageEvent = new StageObeliskEvent(obelisk);
            RaiseLocalEvent(ruleUid, ref stageEvent);
            rule.IsObeliskArrival = true;
            return true;
        }

        return false;
    }

    private HashSet<EntityUid> GetNearbyHumanCorpses(EntityUid head)
    {
        var corpses = new HashSet<EntityUid>();
        foreach (var entity in _lookup.GetEntitiesInRange(Transform(head).Coordinates, 3f))
        {
            if (HasComp<HumanoidAppearanceComponent>(entity) &&
                !HasComp<NecromorfComponent>(entity) &&
                !HasComp<ZombieComponent>(entity) &&
                _mobState.IsDead(entity))
            {
                corpses.Add(entity);
            }
        }

        return corpses;
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

        _damageable.TryChangeDamage(target, component.Damage, false, false);
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

    private void EndStageConvergence(EntityUid uid, UnitologyRuleComponent component, SpawnNecroMoonEvent ev)
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

        // Статистика для дашборда
        var winner = index == 2 ? BiStatWinner.Antagonist : BiStatWinner.Crew;
        _ = System.Threading.Tasks.Task.Run(async () =>
        {
            try
            {
                await _db.AddBiStatAsync("Юнитологи", winner, DateTime.UtcNow);
            }
            catch
            {

            }
        });

    }

    protected override void AppendRoundEndDiscordText(EntityUid uid,
        UnitologyRuleComponent component,
        GameRuleComponent gameRule,
        ref RoundEndDiscordTextAppendEvent args)
    {
        var sessionData = _antag.GetAntagIdentifiers(uid);
        args.AddLine(Loc.GetString("uni-initial-count", ("initialCount", sessionData.Count)));

        foreach (var (_, data, name) in sessionData)
        {
            args.AddLine(Loc.GetString("uni-initial-name-user",
                ("name", name),
                ("username", data.UserName)));
        }

        args.AddLine("");
    }

    private void SendOrder()
    {
        var faxes = EntityQueryEnumerator<FaxMachineComponent>();
        var wasSent = false;

        var query = EntityQueryEnumerator<UnitologyHeadComponent>();

        while (query.MoveNext(out var ent, out _))
        {
            if (wasSent)
                return;

            var xform = Transform(ent);
            var station = _station.GetStationInMap(xform.MapID);

            if (!HasComp<StationDataComponent>(station))
                continue;

            while (faxes.MoveNext(out var faxEnt, out var fax))
            {
                if (!fax.ReceiveNukeCodes)
                    continue;

                var content = Loc.GetString("paper-order-necromorph");

                var printout = new FaxPrintout(
                    content,
                    Loc.GetString("paper-order-necromorph"),
                    null,
                    null,
                    "paper_stamp-centcom",
                    new List<StampDisplayInfo>
                    {
                        new StampDisplayInfo { StampedName = Loc.GetString("stamp-component-stamped-name-centcom"), StampedColor = Color.FromHex("#006600") },
                    }
                );

                _faxSystem.Receive(faxEnt, printout, null, fax);

                wasSent = true;
            }
        }
    }

    private static readonly string[] Outcomes =
    {
        "uni-lost",
        "uni-obelisk",
        "uni-convergence",
    };
}
