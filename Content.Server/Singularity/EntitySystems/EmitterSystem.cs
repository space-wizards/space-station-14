using Content.Server.Power.EntitySystems;
using Content.Server.Singularity.Components;
using Content.Shared.Interaction;
using Content.Shared.Notification.Managers;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.Physics;

namespace Content.Server.Singularity.EntitySystems
{
    [UsedImplicitly]
    public class EmitterSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<EmitterComponent, PowerConsumerReceivedChanged>(ReceivedChanged);
            SubscribeLocalEvent<EmitterComponent, InteractHandEvent>(OnInteractHand);
            SubscribeLocalEvent<EmitterComponent, InteractUsingEvent>(OnInteractUsing);
        }

        private void OnInteractUsing(EntityUid uid, EmitterComponent component, InteractUsingEvent args)
        {
            if(args.Handled) return;

            if (_accessReader == null || !eventArgs.Using.TryGetComponent(out IAccess? access))
            {
                args.Handled = false;
                return;
            }

            if (_accessReader.IsAllowed(access))
            {
                _isLocked ^= true;

                if (_isLocked)
                {
                    Owner.PopupMessage(eventArgs.User, Loc.GetString("comp-emitter-lock", ("target", Owner)));
                }
                else
                {
                    Owner.PopupMessage(eventArgs.User, Loc.GetString("comp-emitter-unlock", ("target", Owner)));
                }

                UpdateAppearance();
            }
            else
            {
                Owner.PopupMessage(eventArgs.User, Loc.GetString("comp-emitter-access-denied"));
            }

            return Task.FromResult(true);
        }

        private void OnInteractHand(EntityUid uid, EmitterComponent component, InteractHandEvent args)
        {
            if (_isLocked)
            {
                component.Owner.PopupMessage(args.User, Loc.GetString("comp-emitter-access-locked", ("target", component.Owner)));
                return;
            }

            if (component.Owner.TryGetComponent(out PhysicsComponent? phys) && phys.BodyType == BodyType.Static)
            {
                if (!IsOn)
                {
                    SwitchOn();
                    component.Owner.PopupMessage(args.User, Loc.GetString("comp-emitter-turned-on", ("target", component.Owner)));
                }
                else
                {
                    SwitchOff();
                    component.Owner.PopupMessage(args.User, Loc.GetString("comp-emitter-turned-off", ("target", component.Owner)));
                }
            }
            else
            {
                component.Owner.PopupMessage(args.User, Loc.GetString("comp-emitter-not-anchored", ("target", component.Owner)));
            }

        }

        private static void ReceivedChanged(
            EntityUid uid,
            EmitterComponent component,
            PowerConsumerReceivedChanged args)
        {
            if (!component.IsOn)
            {
                return;
            }

            if (args.ReceivedPower < args.DrawRate)
            {
                component.PowerOff();
            }
            else
            {
                component.PowerOn();
            }
        }
    }
}
