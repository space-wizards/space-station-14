// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Server.Antag;
using Content.Server.GameTicking.Rules.Components;
using Robust.Shared.Timing;
using Content.Server.RoundEnd;
using Content.Shared.DeadSpace.Necromorphs.Necroobelisk;
using Content.Server.Audio;
using Content.Shared.Audio;
using Content.Shared.NPC.Prototypes;
using Content.Shared.GameTicking.Components;
using Robust.Shared.Prototypes;
using Content.Shared.Roles;
using Content.Server.Antag.Components;
using System.Linq;
using Content.Server.Roles;
using Content.Server.Mind;

namespace Content.Server.GameTicking.Rules;

public sealed class UnitologyRuleSystem : GameRuleSystem<UnitologyRuleComponent>
{
    [Dependency] private readonly RoleSystem _role = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly RoundEndSystem _roundEnd = default!;
    [Dependency] private readonly ServerGlobalSoundSystem _sound = default!;
    [Dependency] private readonly AntagSelectionSystem _antag = default!;
    [Dependency] private readonly MindSystem _mindSystem = default!;

    [ValidatePrototypeId<EntityPrototype>]
    private const string UnitologyRule = "Unitology";

    [ValidatePrototypeId<AntagPrototype>]
    public const string UnitologyAntagRole = "UniHead";

    [ValidatePrototypeId<NpcFactionPrototype>]
    public const string UnitologyNpcFaction = "Necromorfs";

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
    }

    protected override void ActiveTick(EntityUid uid, UnitologyRuleComponent component, GameRuleComponent gameRule, float frameTime)
    {
        base.ActiveTick(uid, component, gameRule, frameTime);

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
        component.IsStageObelisk = true;
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
