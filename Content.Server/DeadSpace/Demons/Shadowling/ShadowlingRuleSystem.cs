// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Server.Antag;
using Content.Server.GameTicking.Rules;
using Content.Shared.DeadSpace.Demons.Shadowling;
using Content.Shared.GameTicking.Components;
using Content.Shared.Mobs.Systems;
using Robust.Shared.Prototypes;
using Content.Server.Mind;
using Content.Shared.Mobs.Components;
using Content.Server.GameTicking;
using Content.Shared.Mind.Components;
using Content.Shared.Mind;

namespace Content.Server.DeadSpace.Demons.Shadowling;

public sealed class ShadowlingRuleSystem : GameRuleSystem<ShadowlingRuleComponent>
{
    [Dependency] private readonly MindSystem _mind = default!;
    [Dependency] private readonly AntagSelectionSystem _antag = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;

    public readonly EntProtoId ObjectiveId = "ShadowlingRecruitObjective";

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ShadowlingRevealComponent, MapInitEvent>(OnShadowlingInit);
    }

    private void OnShadowlingInit(EntityUid uid, ShadowlingRevealComponent component, MapInitEvent args)
    {
        if (!_mind.TryGetMind(uid, out var mindId, out var mind))
            return;
        _mind.TryAddObjective(mindId, mind, ObjectiveId);
    }

    protected override void AppendRoundEndText(EntityUid uid,
        ShadowlingRuleComponent component,
        GameRuleComponent gameRule,
        ref RoundEndTextAppendEvent args)
    {
        base.AppendRoundEndText(uid, component, gameRule, ref args);

        if (component.ManifestWritten)
            return;
        component.ManifestWritten = true;

        var sessionData = _antag.GetAntagIdentifiers(uid);

        if (sessionData.Count == 0)
            return;

        bool anyAlive = false;
        bool anyAscended = false;

        var query = EntityQueryEnumerator<MindContainerComponent, MobStateComponent>();
        while (query.MoveNext(out var entity, out var mindContainer, out var mobState))
        {
            if (!_mind.TryGetMind(entity, out var mindId, out _, mindContainer))
                continue;

            bool isAntag = false;
            foreach (var (antagMind, _, _) in sessionData)
            {
                if (antagMind == mindId)
                {
                    isAntag = true;
                    break;
                }
            }
            if (!isAntag)
                continue;

            if (!_mobState.IsAlive(entity, mobState))
                continue;

            anyAlive = true;
            if (HasComp<ShadowlingAnnihilationComponent>(entity))
                anyAscended = true;
        }

        if (anyAscended)
            args.AddLine(Loc.GetString("shadowling-win"));
        else if (sessionData.Count > 0 && !anyAlive)
            args.AddLine(Loc.GetString("shadowling-lose"));
        else if (sessionData.Count > 0)
            args.AddLine(Loc.GetString("shadowling-stalemate"));
    }

    protected override void AppendAdminStatus(EntityUid uid,
        ShadowlingRuleComponent component,
        GameRuleComponent gameRule,
        CollectGameRuleAdminStatusEvent args)
    {
        var antags = _antag.GetAntagIdentifiers(uid);
        var living = 0;
        var ascended = 0;
        var lines = new List<string>();

        foreach (var (mindId, _, initialName) in antags)
        {
            if (!TryComp<MindComponent>(mindId, out var mind) ||
                mind.OwnedEntity is not { } body ||
                !Exists(body))
            {
                continue;
            }

            if (_mobState.IsAlive(body))
                living++;

            if (HasComp<ShadowlingAnnihilationComponent>(body))
                ascended++;

            var slaves = 0;
            var slaveQuery = EntityQueryEnumerator<ShadowlingSlaveComponent, MobStateComponent>();
            while (slaveQuery.MoveNext(out var slave, out var slaveComponent, out var mobState))
            {
                if (slaveComponent.Master == body && _mobState.IsAlive(slave, mobState))
                    slaves++;
            }

            var progress = component.TargetSlaves <= 0
                ? 1f
                : Math.Clamp(slaves / (float) component.TargetSlaves, 0f, 1f);
            lines.Add(Loc.GetString("game-rule-admin-status-shadowling-master",
                ("name", ToPrettyString(body).Name ?? initialName),
                ("slaves", slaves),
                ("target", component.TargetSlaves),
                ("progress", progress.ToString("P0"))));
        }

        var state = component switch
        {
            { IsAscended: true } => "ascended",
            { AllDead: true } => "defeated",
            { HadShadowlings: true } => "active",
            _ => "waiting",
        };

        lines.Insert(0, Loc.GetString("game-rule-admin-status-shadowling-summary",
            ("state", Loc.GetString($"game-rule-admin-status-shadowling-state-{state}")),
            ("living", living),
            ("ascended", ascended)));

        if (lines.Count == 1)
            lines.Add(Loc.GetString("game-rule-admin-status-shadowling-no-masters"));

        args.AddSection(Loc.GetString("game-rule-admin-status-shadowling-title"), lines);
    }

    protected override void ActiveTick(EntityUid uid, ShadowlingRuleComponent component, GameRuleComponent gameRule, float frameTime)
    {
        base.ActiveTick(uid, component, gameRule, frameTime);

        if (component.IsAscended || component.AllDead)
            return;

        var sessionData = _antag.GetAntagIdentifiers(uid);
        if (sessionData.Count == 0)
            return;

        component.HadShadowlings = true;

        bool anyAlive = false;
        bool anyAscended = false;

        var query = EntityQueryEnumerator<MindContainerComponent, MobStateComponent>();
        while (query.MoveNext(out var entity, out var mindContainer, out var mobState))
        {
            if (!_mind.TryGetMind(entity, out var mindId, out _, mindContainer))
                continue;

            bool isAntag = false;
            foreach (var (antagMind, _, _) in sessionData)
            {
                if (antagMind == mindId)
                {
                    isAntag = true;
                    break;
                }
            }
            if (!isAntag)
                continue;

            if (!_mobState.IsAlive(entity, mobState))
                continue;

            anyAlive = true;
            if (HasComp<ShadowlingAnnihilationComponent>(entity))
                anyAscended = true;
        }

        if (anyAscended)
            component.IsAscended = true;
        else if (!anyAlive)
            component.AllDead = true;
    }
}
