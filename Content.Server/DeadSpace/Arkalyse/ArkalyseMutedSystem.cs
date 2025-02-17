// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Shared.Actions;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs;
using Content.Shared.Weapons.Melee.Events;
using System.Linq;
using Content.Shared.Speech.Muting;
using Robust.Shared.Timing;
using Content.Server.Body.Components;
using Content.Shared.DeadSpace.Arkalyse;
using Content.Server.DeadSpace.Arkalyse.Components;

namespace Content.Server.DeadSpace.Arkalyse;

public sealed class ArkalyseMutedSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actionSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ArkalyseMutedComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<ArkalyseMutedComponent, ComponentShutdown>(OnComponentShutdown);
        SubscribeLocalEvent<ArkalyseMutedComponent, MeleeHitEvent>(OnMeleeHit);
        SubscribeLocalEvent<ArkalyseMutedComponent, MutedAtackArkalyseActionEvent>(OnActionActivated);
    }
    private void OnComponentInit(EntityUid uid, ArkalyseMutedComponent component, ComponentInit args)
    {
        _actionSystem.AddAction(uid, ref component.ActionMutedArkalyseAttackEntity, component.ActionMutedArkalyseAttack, uid);
    }
    private void OnComponentShutdown(EntityUid uid, ArkalyseMutedComponent component, ComponentShutdown args)
    {
        _actionSystem.RemoveAction(uid, component.ActionMutedArkalyseAttackEntity);
    }
    private void OnActionActivated(EntityUid uid, ArkalyseMutedComponent component, MutedAtackArkalyseActionEvent args)
    {
        if (args.Handled)
            return;

        component.IsMutedAttack = !component.IsMutedAttack;
        args.Handled = true;
    }
    private void OnMeleeHit(EntityUid uid, ArkalyseMutedComponent component, MeleeHitEvent args)
    {
        if (component.IsMutedAttack && args.HitEntities.Any())
        {
            foreach (var entity in args.HitEntities)
            {
                if (args.User == entity)
                    continue;

                if (TryComp<MobStateComponent>(entity, out var mobState) && mobState.CurrentState == MobState.Alive)
                {
                    if (!HasComp<MutedComponent>(entity))
                    {
                        AddComp<MutedComponent>(entity);
                        Timer.Spawn(TimeSpan.FromSeconds(component.TimeMuted), () => { if (Exists(entity)) RemComp<MutedComponent>(entity); });
                    }
                    if (HasComp<RespiratorComponent>(entity))
                    {
                        var breath = EntityManager.GetComponent<RespiratorComponent>(entity);
                        Timer.Spawn(TimeSpan.FromSeconds(component.TimeSuffocation), () =>
                        {
                            if (Exists(entity))
                            {
                                for (var i = 0; i <= 4; i++)
                                {
                                    breath.Saturation--;
                                    breath.SuffocationCycles++;
                                }
                            }
                        });
                    }

                    component.IsMutedAttack = false;
                }
            }
        }
    }
}
