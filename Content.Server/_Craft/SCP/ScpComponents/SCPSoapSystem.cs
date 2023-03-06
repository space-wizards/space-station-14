using Robust.Shared.Physics.Components;
using Robust.Shared.Physics;
using Robust.Shared.Timing;
using Robust.Shared.Player;
using Robust.Server.GameObjects;

using Content.Shared.Physics;
using Content.Shared.Actions;
using Robust.Shared.Physics.Systems;

using Content.Shared.StatusEffect;
using Content.Shared.Stunnable;
using Robust.Shared.Containers;

using Content.Server.Actions;

namespace Content.Server.SCP.Soap
{
    public sealed class SCPSoapSystem : EntitySystem
    {
        [Dependency] private readonly EntityLookupSystem _lookup = default!;
        [Dependency] private readonly SharedAudioSystem _audio = default!;
        [Dependency] private readonly ActionsSystem _actionSys = default!;
        [Dependency] private readonly SharedPhysicsSystem _physics = default!;
        [Dependency] private readonly SharedStunSystem _stunSys = default!;
        [Dependency] private readonly StatusEffectsSystem _statSys = default!;
        [Dependency] private readonly SharedContainerSystem _container = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<SCPSoapComponent, ComponentInit>(OnComponentInit);
            SubscribeLocalEvent<SCPSoapComponent, ComponentRemove>(OnComponentRemove);

            SubscribeLocalEvent<SCPSoapComponent, SlipActionEvent>(OnSlipAction);

        }

        private void OnComponentInit(EntityUid uid, SCPSoapComponent component, ComponentInit args)
        {
            _actionSys.AddAction(uid, component.SlipAction, uid);
            //_actionSys.AddAction(uid, component.CleanAction, uid);
        }
        private void OnComponentRemove(EntityUid uid, SCPSoapComponent component, ComponentRemove args)
        {
            _actionSys.RemoveAction(uid, component.SlipAction);
            //_actionSys.RemoveAction(uid, component.CleanAction);
        }
        private void OnSlipAction(EntityUid uid, SCPSoapComponent component, SlipActionEvent args)
        {
            if (_container.IsEntityInContainer(uid))
                return;
            var xform = Comp<TransformComponent>(uid);
            bool isSlipped = false;
            foreach (var entity in _lookup.GetEntitiesInRange(xform.MapPosition, component.SlipActionRange))
                if (entity == uid)
                    continue;
                else
                    TrySlip(entity, component.SlipActionForce, component.SlipActionStun, ref isSlipped);
            if (isSlipped)
                _audio.PlayPvs(component.SlipActionSound, uid);
            args.Handled = isSlipped;
        }

        private void TrySlip(EntityUid uid, float force, float stuntime, ref bool isSlipped)
        {
            if (HasComp<KnockedDownComponent>(uid) ||
                //HasComp<NoSlipComponent>(uid) ||
                _statSys.HasStatusEffect(uid, "KnockedDown") ||
                !_statSys.CanApplyEffect(uid, "KnockedDown"))
                return;

            if (!TryComp(uid, out PhysicsComponent? physics))
                return;

            var velocity = physics.LinearVelocity;

            if (velocity.Length < 0.00001)
                return;

            _physics.SetLinearVelocity(uid, velocity.Normalized * force, body: physics);
            _stunSys.TryParalyze(uid, TimeSpan.FromSeconds(stuntime), true);
            isSlipped = true;
            return;
        }

    }
    public sealed class SlipActionEvent : InstantActionEvent { }
    //public sealed class CleanActionEvent : InstantActionEvent { }
}
