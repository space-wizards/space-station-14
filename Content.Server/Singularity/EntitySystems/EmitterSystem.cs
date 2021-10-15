using System;
using System.Threading;
using Content.Server.Access.Components;
using Content.Server.Power.EntitySystems;
using Content.Server.Projectiles.Components;
using Content.Server.Singularity.Components;
using Content.Shared.Audio;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Singularity.Components;
using JetBrains.Annotations;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Log;
using Robust.Shared.Physics;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Robust.Shared.Utility;
using Timer = Robust.Shared.Timing.Timer;

namespace Content.Server.Singularity.EntitySystems
{
    [UsedImplicitly]
    public class EmitterSystem : EntitySystem
    {
        [Dependency] private readonly IRobustRandom _random = default!;

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

            if (component.AccessReader == null || !args.Used.TryGetComponent(out IAccess? access))
            {
                return;
            }

            if (component.AccessReader.IsAllowed(access))
            {
                component.IsLocked ^= true;

                if (component.IsLocked)
                {
                    component.Owner.PopupMessage(args.User, Loc.GetString("comp-emitter-lock", ("target", component.Owner)));
                }
                else
                {
                    component.Owner.PopupMessage(args.User, Loc.GetString("comp-emitter-unlock", ("target", component.Owner)));
                }

                UpdateAppearance(component);
            }
            else
            {
                component.Owner.PopupMessage(args.User, Loc.GetString("comp-emitter-access-denied"));
            }

            args.Handled = true;
        }

        private void OnInteractHand(EntityUid uid, EmitterComponent component, InteractHandEvent args)
        {
            args.Handled = true;
            if (component.IsLocked)
            {
                component.Owner.PopupMessage(args.User, Loc.GetString("comp-emitter-access-locked", ("target", component.Owner)));
                return;
            }

            if (component.Owner.TryGetComponent(out PhysicsComponent? phys) && phys.BodyType == BodyType.Static)
            {
                if (!component.IsOn)
                {
                    SwitchOn(component);
                    component.Owner.PopupMessage(args.User, Loc.GetString("comp-emitter-turned-on", ("target", component.Owner)));
                }
                else
                {
                    SwitchOff(component);
                    component.Owner.PopupMessage(args.User, Loc.GetString("comp-emitter-turned-off", ("target", component.Owner)));
                }
            }
            else
            {
                component.Owner.PopupMessage(args.User, Loc.GetString("comp-emitter-not-anchored", ("target", component.Owner)));
            }
        }

        private void ReceivedChanged(
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
                PowerOff(component);
            }
            else
            {
                PowerOn(component);
            }
        }

        public void SwitchOff(EmitterComponent component)
        {
            component.IsOn = false;
            if (component.PowerConsumer != null) component.PowerConsumer.DrawRate = 0;
            PowerOff(component);
            UpdateAppearance(component);
        }

        public void SwitchOn(EmitterComponent component)
        {
            component.IsOn = true;
            if (component.PowerConsumer != null) component.PowerConsumer.DrawRate = component.PowerUseActive;
            // Do not directly PowerOn().
            // OnReceivedPowerChanged will get fired due to DrawRate change which will turn it on.
            UpdateAppearance(component);
        }

        public void PowerOff(EmitterComponent component)
        {
            if (!component.IsPowered)
            {
                return;
            }

            component.IsPowered = false;

            // Must be set while emitter powered.
            DebugTools.AssertNotNull(component.TimerCancel);
            component.TimerCancel?.Cancel();

            UpdateAppearance(component);
        }

        public void PowerOn(EmitterComponent component)
        {
            if (component.IsPowered)
            {
                return;
            }

            component.IsPowered = true;

            component.FireShotCounter = 0;
            component.TimerCancel = new CancellationTokenSource();

            Timer.Spawn(component.FireBurstDelayMax, () => ShotTimerCallback(component), component.TimerCancel.Token);

            UpdateAppearance(component);
        }

        private void ShotTimerCallback(EmitterComponent component)
        {
            if (component.Deleted) return;

            // Any power-off condition should result in the timer for this method being cancelled
            // and thus not firing
            DebugTools.Assert(component.IsPowered);
            DebugTools.Assert(component.IsOn);
            DebugTools.Assert(component.PowerConsumer != null && (component.PowerConsumer.DrawRate <= component.PowerConsumer.ReceivedPower));

            Fire(component);

            TimeSpan delay;
            if (component.FireShotCounter < component.FireBurstSize)
            {
                component.FireShotCounter += 1;
                delay = component.FireInterval;
            }
            else
            {
                component.FireShotCounter = 0;
                var diff = component.FireBurstDelayMax - component.FireBurstDelayMin;
                // TIL you can do TimeSpan * double.
                delay = component.FireBurstDelayMin + _random.NextFloat() * diff;
            }

            // Must be set while emitter powered.
            DebugTools.AssertNotNull(component.TimerCancel);
            Timer.Spawn(delay, () => ShotTimerCallback(component), component.TimerCancel!.Token);
        }

        private void Fire(EmitterComponent component)
        {
            var projectile = component.Owner.EntityManager.SpawnEntity(component.BoltType, component.Owner.Transform.Coordinates);

            if (!projectile.TryGetComponent<PhysicsComponent>(out var physicsComponent))
            {
                Logger.Error("Emitter tried firing a bolt, but it was spawned without a PhysicsComponent");
                return;
            }

            physicsComponent.BodyStatus = BodyStatus.InAir;

            if (!projectile.TryGetComponent<ProjectileComponent>(out var projectileComponent))
            {
                Logger.Error("Emitter tried firing a bolt, but it was spawned without a ProjectileComponent");
                return;
            }

            projectileComponent.IgnoreEntity(component.Owner);

            physicsComponent
                .LinearVelocity = component.Owner.Transform.WorldRotation.ToWorldVec() * 20f;
            projectile.Transform.WorldRotation = component.Owner.Transform.WorldRotation;

            // TODO: Move to projectile's code.
            Timer.Spawn(3000, () => projectile.Delete());

            SoundSystem.Play(Filter.Pvs(component.Owner), component.FireSound.GetSound(), component.Owner,
                AudioHelpers.WithVariation(EmitterComponent.Variation).WithVolume(EmitterComponent.Volume).WithMaxDistance(EmitterComponent.Distance));
        }

        private void UpdateAppearance(EmitterComponent component)
        {
            if (component.Appearance == null)
            {
                return;
            }

            EmitterVisualState state;
            if (component.IsPowered)
            {
                state = EmitterVisualState.On;
            }
            else if (component.IsOn)
            {
                state = EmitterVisualState.Underpowered;
            }
            else
            {
                state = EmitterVisualState.Off;
            }

            component.Appearance.SetData(EmitterVisuals.VisualState, state);
            component.Appearance.SetData(EmitterVisuals.Locked, component.IsLocked);
        }
    }
}
