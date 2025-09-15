// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Shared.Actions;
using Content.Shared.DeadSpace.Necromorphs.Unitology;
using Content.Shared.DeadSpace.Necromorphs.Unitology.Components;
using Content.Server.Popups;
using Content.Shared.DeadSpace.Necromorphs.InfectionDead.Components;
using Content.Server.DeadSpace.Necromorphs.Unitology.Components;
using Content.Shared.Zombies;
using Content.Server.DeadSpace.Necromorphs.InfectionDead;
using Content.Shared.Humanoid;
using Content.Shared.DoAfter;
using Content.Shared.Tag;
using System.Linq;
using Content.Server.Mind;
using Content.Server.Antag;
using Robust.Shared.Prototypes;
using Content.Server.Antag.Components;
using Content.Shared.Roles;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.Chat.Systems;
using Content.Server.Decals;
using Robust.Shared.Map;
using Content.Shared.Maps;
using Content.Shared.Decals;
using Content.Shared.Mobs.Systems;
using Content.Shared.Mindshield.Components;
using Robust.Server.Player;

namespace Content.Server.DeadSpace.Necromorphs.Unitology;

public sealed class UnitologyHeadSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly InfectionDeadSystem _infectionDead = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly TagSystem _tags = default!;
    [Dependency] private readonly MindSystem _mindSystem = default!;
    [Dependency] private readonly AntagSelectionSystem _antag = default!;
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly DecalSystem _decals = default!;
    [Dependency] private readonly IMapManager _mapMan = default!;
    [Dependency] private readonly TurfSystem _turf = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly IPlayerManager _player = default!;


    private static readonly EntProtoId UnitologyRule = "Unitology";
    public static readonly ProtoId<AntagPrototype> UnitologyAntagRole = "Uni";

    public const float DistanceRecruitmentDetermination = 2f;
    public const string CandleTag = "Candle";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<UnitologyHeadComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<UnitologyHeadComponent, ComponentShutdown>(OnShutDown);
        SubscribeLocalEvent<UnitologyHeadComponent, UnitologyHeadActionEvent>(OnHeadUnitology);
        SubscribeLocalEvent<UnitologyHeadComponent, OrderToSlaveActionEvent>(OnOrder);
        SubscribeLocalEvent<UnitologyHeadComponent, SelectTargetRecruitmentEvent>(OnSelectTargetRecruitment);
        SubscribeLocalEvent<UnitologyHeadComponent, UnitologistRecruitmentDoAfterEvent>(OnRecruitmentDoAfter);
    }

    private void OnComponentInit(EntityUid uid, UnitologyHeadComponent component, ComponentInit args)
    {
        _actionsSystem.AddAction(uid, ref component.ActionUnitologyHeadEntity, component.ActionUnitologyHead, uid);
        _actionsSystem.AddAction(uid, ref component.ActionOrderToSlaveEntity, component.ActionOrderToSlave, uid);
        _actionsSystem.AddAction(uid, ref component.ActionSelectTargetRecruitmentEntity, component.ActionSelectTargetRecruitment, uid);
    }

    private void OnShutDown(EntityUid uid, UnitologyHeadComponent component, ComponentShutdown args)
    {
        _actionsSystem.RemoveAction(uid, component.ActionUnitologyHeadEntity);
        _actionsSystem.RemoveAction(uid, component.ActionOrderToSlaveEntity);
        _actionsSystem.RemoveAction(uid, component.ActionSelectTargetRecruitmentEntity);
    }

    private void OnSelectTargetRecruitment(EntityUid uid, UnitologyHeadComponent component, SelectTargetRecruitmentEvent args)
    {
        if (args.Handled)
            return;

        if (args.Target == uid)
            return;

        var target = args.Target;

        if (!_infectionDead.IsInfectionPossible(target)
        || !HasComp<HumanoidAppearanceComponent>(target)
        || HasComp<MindShieldComponent>(target)
        || HasComp<UnitologyHeadComponent>(target)
        || HasComp<UnitologyComponent>(target)
        || HasComp<UnitologyEnslavedComponent>(target)
        || !_mobState.IsAlive(target)
        || !_mindSystem.TryGetMind(target, out _, out _))
        {
            _popup.PopupEntity(Loc.GetString("Цель не подходит для вербовки."), uid, uid);
            return;
        }

        var entities = _lookup.GetEntitiesInRange(_transform.GetMapCoordinates(uid, Transform(uid)), DistanceRecruitmentDetermination).ToList();
        var candlesEntities = entities
            .Where(ent => ent != uid && _tags.HasTag(ent, CandleTag))
            .ToList();

        var xform = Transform(target);
        var tileref = _turf.GetTileRef(xform.Coordinates);

        if (tileref == null)
            return;

        var decals = _decals.GetDecalsInRange(tileref.Value.GridUid, _turf.GetTileCenter(tileref.Value).Position, 1f);

        var penctagramDecals = _prototypeManager.EnumeratePrototypes<DecalPrototype>()
        .Where(x => x.Tags.Contains("uni-penctagram"))
        .Select(x => x.ID)
        .ToArray();

        bool condition = false;

        foreach (var (id, decal) in decals)
        {
            if (penctagramDecals.Contains(decal.Id))
            {
                condition = true;
                break;
            }
        }

        if (candlesEntities.Count() < component.NumberOfCandles || !condition)
        {
            _popup.PopupEntity(Loc.GetString("Условия не выполнены!"), uid, uid);
            return;
        }

        var doAfter = new DoAfterArgs(EntityManager, uid, component.VerbDuration, new UnitologistRecruitmentDoAfterEvent(), uid, target: target)
        {
            Hidden = true,
            Broadcast = false,
            BreakOnDamage = true,
            BreakOnMove = true,
            BlockDuplicate = true,
            CancelDuplicate = true,
            DistanceThreshold = 1
        };

        if (!_doAfter.TryStartDoAfter(doAfter))
            return;

        Random random = new Random();
        int index = random.Next(component.WordsArray.Length);
        string message = component.WordsArray[index];

        _chat.TrySendInGameICMessage(uid, message, InGameICChatType.Speak, true);

        args.Handled = true;
    }

    private void OnRecruitmentDoAfter(EntityUid uid, UnitologyHeadComponent component, UnitologistRecruitmentDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || args.Args.Target == null)
            return;

        var target = args.Args.Target;

        if (!_mindSystem.TryGetMind(target.Value, out var mindId, out var mind))
            return;

        var rule = _antag.ForceGetGameRuleEnt<UnitologyRuleComponent>(UnitologyRule);

        AntagSelectionDefinition? definition = rule.Comp.Definitions.FirstOrDefault(def =>
        def.PrefRoles.Contains(new ProtoId<AntagPrototype>(UnitologyAntagRole))
        );

        definition ??= rule.Comp.Definitions.Last();

        if (!_player.TryGetSessionById(mind.UserId, out var session))
            return;

        _antag.MakeAntag(rule, session, definition.Value);
    }

    private void OnOrder(EntityUid uid, UnitologyHeadComponent component, OrderToSlaveActionEvent args)
    {
        if (args.Handled)
            return;

        if (args.Target == uid)
            return;

        var target = args.Target;

        if (!HasComp<UnitologyEnslavedComponent>(target))
        {
            _popup.PopupEntity(Loc.GetString("Цель должна быть подчинена!"), uid, uid);
            return;
        }

        if (!HasComp<StunSlaveComponent>(target))
        {
            AddComp<StunSlaveComponent>(target);
            _popup.PopupEntity(Loc.GetString("Цель парализованна."), uid, uid);
        }
        else
        {
            RemComp<StunSlaveComponent>(target);
            _popup.PopupEntity(Loc.GetString("Цель может двигаться."), uid, uid);
        }

        args.Handled = true;

    }
    private void OnHeadUnitology(EntityUid uid, UnitologyHeadComponent component, UnitologyHeadActionEvent args)
    {
        if (args.Handled)
            return;

        if (args.Target == uid)
            return;

        var target = args.Target;
        if (!IsCanTransfer(uid, target))
            return;

        args.Handled = true;

        RemComp<UnitologyHeadComponent>(uid);

        AddComp<UnitologyHeadComponent>(target);
    }

    private bool IsCanTransfer(EntityUid uid, EntityUid target)
    {
        if (!HasComp<UnitologyComponent>(target))
        {
            _popup.PopupEntity(Loc.GetString("Цель должна быть юнитологом!"), uid, uid);
            return false;
        }

        if (HasComp<UnitologyHeadComponent>(target))
        {
            _popup.PopupEntity(Loc.GetString("Цель уже обладает вашими знаниями и положением!"), uid, uid);
            return false;
        }

        if (HasComp<UnitologyEnslavedComponent>(target))
        {
            _popup.PopupEntity(Loc.GetString("Цель не может быть порабощенным!"), uid, uid);
            return false;
        }

        if (HasComp<NecromorfComponent>(target) || HasComp<ZombieComponent>(target))
        {
            _popup.PopupEntity(Loc.GetString("Цель не может быть выбрана!"), uid, uid);
            return false;
        }

        return true;
    }
}
