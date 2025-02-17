// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Shared.Actions;
using Robust.Shared.Timing;
using Content.Shared.Mobs.Components;
using Content.Server.Beam;
using Content.Server.DeadSpace.Sith.Components;
using Content.Shared.Stunnable;
using Content.Shared.DeadSpace.Sith;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.StatusEffect;

namespace Content.Server.DeadSpace.Sith;

public sealed class SithLightningAbilitySystem : EntitySystem
{
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;
    [Dependency] private readonly BeamSystem _beam = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly StatusEffectsSystem _statusEffect = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SithLightningAbilityComponent, SithLightningEvent>(OnSithLightning);
        SubscribeLocalEvent<SithLightningAbilityComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<SithLightningAbilityComponent, ComponentShutdown>(OnComponentShutdown);
    }

    private void OnComponentInit(EntityUid uid, SithLightningAbilityComponent component, ComponentInit args)
    {
        _actionsSystem.AddAction(uid, ref component.ActionSithLightningEntity, component.ActionSithLightning, uid);
    }

    private void OnComponentShutdown(EntityUid uid, SithLightningAbilityComponent component, ComponentShutdown args)
    {
        _actionsSystem.RemoveAction(uid, component.ActionSithLightningEntity);
    }
    
    private void OnSithLightning(EntityUid uid, SithLightningAbilityComponent component, SithLightningEvent args)
    {
        if (!_gameTiming.IsFirstTimePredicted)
            return;

        if (args.Handled)
            return;

        var target = args.Target;

        if (!TryComp<MobStateComponent>(target, out var stateComponent) && _mobState.IsDead(target))
        {
            _popup.PopupEntity(Loc.GetString("Цель должна быть живым существом."), uid, uid);
            return;
        }

        if (!TryComp<TransformComponent>(target, out var xform))
            return;

        args.Handled = true;

        var targets = _lookup.GetEntitiesInRange<MobStateComponent>(_transform.GetMapCoordinates(target, xform), component.Range);

        _statusEffect.TryAddStatusEffect<StunnedComponent>(uid, "Stun", TimeSpan.FromSeconds(2f), true);

        foreach (var (entity, mobStateComponent) in targets)
        {
            if (entity == uid)
                continue;

            if (_mobState.IsDead(entity))
                continue;

            _beam.TryCreateBeam(uid, entity, component.LightingPrototypeId);
            _stun.TryParalyze(entity, TimeSpan.FromSeconds(5), true);
        }
    }
}
