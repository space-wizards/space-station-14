using Content.Shared.Weapons.Melee.Events;
using Content.Shared.DeadSpace.Abilities.HipoBite.Components;
using Content.Shared.Body.Components;
using Content.Server.Popups;
using Content.Shared.Chemistry.EntitySystems;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Timing;
using Content.Shared.NPC.Systems;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using System.Linq;

namespace Content.Server.DeadSpace.Abilities.HipoBite;

public sealed partial class HipoBiteSystem : EntitySystem
{
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainer = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly NpcFactionSystem _npcFaction = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HipoBiteComponent, MeleeHitEvent>(OnMeleeHit);
        SubscribeLocalEvent<HipoBiteComponent, RegenReagentForHipoBiteEvent>(OnRegenReagent);
    }

    private void OnRegenReagent(EntityUid uid, HipoBiteComponent component, RegenReagentForHipoBiteEvent args)
    {

        if (component.CountReagent < component.MaxCountReagent)
        {
            SetReagentCount(uid, component, 5);
        }
        component.TimeUntilRegenReagent = _timing.CurTime + TimeSpan.FromSeconds(component.DurationRegenReagent);
    }

    private void OnMeleeHit(EntityUid uid, HipoBiteComponent component, MeleeHitEvent args)
    {

        if (!args.HitEntities.Any())
            return;

        foreach (var entity in args.HitEntities)
        {
            if (_npcFaction.IsEntityFriendly(uid, entity))
                continue;

            if (args.User == entity)
                continue;

            if (!TryComp<MobStateComponent>(entity, out var mobState))
                continue;

            if (mobState.CurrentState != MobState.Dead)
            {
                Inject(uid, component, entity);
            }
        }

    }

    private void Inject(EntityUid uid, HipoBiteComponent component, EntityUid target)
    {
        if (component.CountReagent <= 0)
            return;

        if (!HasComp<BodyComponent>(target))
            return;

        if (!_solutionContainer.TryGetInjectableSolution(target, out var injectable, out _))
            return;

        _audio.PlayPvs(component.InjectSound, target);
        _solutionContainer.TryAddReagent(injectable.Value, component.Reagent, component.Quantity, out _);
        _popup.PopupEntity(Loc.GetString("hypospray-component-feel-prick-message"), target, target);

        SetReagentCount(uid, component, -5);

        return;
    }

    private void SetReagentCount(EntityUid uid, HipoBiteComponent component, float count)
    {
        component.CountReagent += count;
        _popup.PopupEntity(Loc.GetString("У вас есть ") + component.CountReagent.ToString() + Loc.GetString(" реагента"), uid, uid);
    }
}
