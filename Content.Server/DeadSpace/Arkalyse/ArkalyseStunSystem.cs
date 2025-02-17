// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Shared.Mobs.Components;
using Content.Shared.Stunnable;
using Content.Shared.Mobs;
using Content.Shared.Weapons.Melee.Events;
using System.Linq;
using Content.Shared.Actions;
using Content.Shared.DeadSpace.Arkalyse;
using Content.Server.DeadSpace.Arkalyse.Components;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Audio;

namespace Content.Server.DeadSpace.Arkalyse;

public sealed class ArkalyseStunSystem : EntitySystem
{
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly SharedActionsSystem _actionSystem = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ArkalyseStunComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<ArkalyseStunComponent, MeleeHitEvent>(OnMeleeHit);
        SubscribeLocalEvent<ArkalyseStunComponent, ComponentShutdown>(OnComponentShutdown);
        SubscribeLocalEvent<ArkalyseStunComponent, StunedAtackArkalyseActionEvent>(OnActionActivated);
    }

    private void OnComponentInit(EntityUid uid, ArkalyseStunComponent component, ComponentInit args)
    {
        _actionSystem.AddAction(uid, ref component.ActionStunArkalyseAttackEntity, component.ActionStunArkalyseAttack, uid);
    }

    private void OnComponentShutdown(EntityUid uid, ArkalyseStunComponent component, ComponentShutdown args)
    {
        _actionSystem.RemoveAction(uid, component.ActionStunArkalyseAttackEntity);
    }

    private void OnActionActivated(EntityUid uid, ArkalyseStunComponent component, StunedAtackArkalyseActionEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;
        component.IsStunedAttack = !component.IsStunedAttack;
    }

    private void OnMeleeHit(EntityUid uid, ArkalyseStunComponent component, MeleeHitEvent args)
    {
        if (component.IsStunedAttack && args.HitEntities.Any())
        {
            foreach (var entity in args.HitEntities)
            {
                if (args.User == entity)
                    continue;

                if (TryComp<MobStateComponent>(entity, out var mobState) && mobState.CurrentState == MobState.Alive)
                {
                    _audio.PlayPvs(component.HitSound, args.User, AudioParams.Default.WithVolume(0.5f));
                    _stun.TryParalyze(entity, TimeSpan.FromSeconds(component.ParalyzeTime), true);
                    component.IsStunedAttack = false;
                }
            }
        }
    }
}
