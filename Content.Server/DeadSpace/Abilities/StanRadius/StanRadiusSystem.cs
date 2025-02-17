// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Shared.Actions;
using Content.Shared.DeadSpace.Abilities.StunRadius;
using Content.Shared.DeadSpace.Abilities.StunRadius.Components;
using Content.Shared.Mobs.Components;
using Content.Shared.Stunnable;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Content.Shared.NPC.Systems;

namespace Content.Server.DeadSpace.Abilities.StunRadius;

public sealed partial class StunRadiusSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly NpcFactionSystem _npcFaction = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StunRadiusComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<StunRadiusComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<StunRadiusComponent, StunRadiusActionEvent>(DoStunRadius);
    }

    private void OnComponentInit(EntityUid uid, StunRadiusComponent component, ComponentInit args)
    {
        _actionsSystem.AddAction(uid, ref component.ActionStunRadiusEntity, component.ActionStunRadius, uid);
    }

    private void OnShutdown(EntityUid uid, StunRadiusComponent component, ComponentShutdown args)
    {
        _actionsSystem.RemoveAction(uid, component.ActionStunRadiusEntity);
    }

    private void DoStunRadius(EntityUid uid, StunRadiusComponent component, StunRadiusActionEvent args)
    {
        if (args.Handled)
        {
            return;
        }
        args.Handled = true;
        var entities = _lookup.GetEntitiesInRange(uid, component.RangeStun);

        foreach (var ent in entities)
        {
            if (EntityManager.HasComponent<MobStateComponent>(ent))
            {
                if (component.IgnorAlien && _npcFaction.IsEntityFriendly(uid, ent))
                {
                    continue;
                }
                if (TryComp(ent, out PhysicsComponent? physics))
                {
                    _physics.SetLinearVelocity(ent, physics.LinearVelocity * component.LaunchForwardsMultiplier, body: physics);
                }

                _stun.TryParalyze(ent, TimeSpan.FromSeconds(component.ParalyzeTime), true);
            }
        }

        if (component.StunRadiusSound == null)
        {
            return;
        }

        _audio.PlayPvs(component.StunRadiusSound, uid, AudioParams.Default.WithVolume(3).WithMaxDistance(component.RangeStun * 2));
    }
}
