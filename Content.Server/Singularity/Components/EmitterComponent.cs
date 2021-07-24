using System;
using System.Threading;
using System.Threading.Tasks;
using Content.Server.Access.Components;
using Content.Server.Power.Components;
using Content.Server.Projectiles.Components;
using Content.Shared.Audio;
using Content.Shared.Interaction;
using Content.Shared.Notification;
using Content.Shared.Notification.Managers;
using Content.Shared.Singularity.Components;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Log;
using Robust.Shared.Physics;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;
using Timer = Robust.Shared.Timing.Timer;


namespace Content.Server.Singularity.Components
{
    [RegisterComponent]
    [ComponentReference(typeof(IActivate))]
    public class EmitterComponent : Component, IActivate, IInteractUsing
    {
        [Dependency] private readonly IRobustRandom _robustRandom = default!;

        [ComponentDependency] private readonly AppearanceComponent? _appearance = default;
        [ComponentDependency] private readonly AccessReader? _accessReader = default;

        public override string Name => "Emitter";

        private CancellationTokenSource? _timerCancel;

        private PowerConsumerComponent _powerConsumer = default!;

        // whether the power switch is in "on"
        [ViewVariables] public bool IsOn { get; private set; }
        // Whether the power switch is on AND the machine has enough power (so is actively firing)
        [ViewVariables] private bool _isPowered;
        [ViewVariables] private bool _isLocked;

        // For the "emitter fired" sound
        private const float Variation = 0.25f;
        private const float Volume = 0.5f;
        private const float Distance = 3f;

        [ViewVariables(VVAccess.ReadWrite)] private int _fireShotCounter;

        [ViewVariables(VVAccess.ReadWrite)] [DataField("fireSound")] private string _fireSound = "/Audio/Weapons/emitter.ogg";
        [ViewVariables(VVAccess.ReadWrite)] [DataField("boltType")] private string _boltType = "EmitterBolt";
        [ViewVariables(VVAccess.ReadWrite)] [DataField("powerUseActive")] private int _powerUseActive = 500;
        [ViewVariables(VVAccess.ReadWrite)] [DataField("fireBurstSize")] private int _fireBurstSize = 3;
        [ViewVariables(VVAccess.ReadWrite)] [DataField("fireInterval")] private TimeSpan _fireInterval = TimeSpan.FromSeconds(2);
        [ViewVariables(VVAccess.ReadWrite)] [DataField("fireBurstDelayMin")] private TimeSpan _fireBurstDelayMin = TimeSpan.FromSeconds(2);
        [ViewVariables(VVAccess.ReadWrite)] [DataField("fireBurstDelayMax")] private TimeSpan _fireBurstDelayMax = TimeSpan.FromSeconds(10);

        void IActivate.Activate(ActivateEventArgs eventArgs)
        {
            if (_isLocked)
            {
                Owner.PopupMessage(eventArgs.User, Loc.GetString("comp-emitter-access-locked", ("target", Owner)));
                return;
            }

            if (Owner.TryGetComponent(out PhysicsComponent? phys) && phys.BodyType == BodyType.Static)
            {
                if (!IsOn)
                {
                    SwitchOn();
                    Owner.PopupMessage(eventArgs.User, Loc.GetString("comp-emitter-turned-on", ("target", Owner)));
                }
                else
                {
                    SwitchOff();
                    Owner.PopupMessage(eventArgs.User, Loc.GetString("comp-emitter-turned-off", ("target", Owner)));
                }
            }
            else
            {
                Owner.PopupMessage(eventArgs.User, Loc.GetString("comp-emitter-not-anchored", ("target", Owner)));
            }
        }

        Task<bool> IInteractUsing.InteractUsing(InteractUsingEventArgs eventArgs)
        {
            if (_accessReader == null || !eventArgs.Using.TryGetComponent(out IAccess? access))
            {
                return Task.FromResult(false);
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

        public void SwitchOff()
        {
            IsOn = false;
            _powerConsumer.DrawRate = 0;
            PowerOff();
            UpdateAppearance();
        }

        public void SwitchOn()
        {
            IsOn = true;
            _powerConsumer.DrawRate = _powerUseActive;
            // Do not directly PowerOn().
            // OnReceivedPowerChanged will get fired due to DrawRate change which will turn it on.
            UpdateAppearance();
        }

        public void PowerOff()
        {
            if (!_isPowered)
            {
                return;
            }

            _isPowered = false;

            // Must be set while emitter powered.
            DebugTools.AssertNotNull(_timerCancel);
            _timerCancel!.Cancel();

            UpdateAppearance();
        }

        public void PowerOn()
        {
            if (_isPowered)
            {
                return;
            }

            _isPowered = true;

            _fireShotCounter = 0;
            _timerCancel = new CancellationTokenSource();

            Timer.Spawn(_fireBurstDelayMax, ShotTimerCallback, _timerCancel.Token);

            UpdateAppearance();
        }

        private void ShotTimerCallback()
        {
            // Any power-off condition should result in the timer for this method being cancelled
            // and thus not firing
            DebugTools.Assert(_isPowered);
            DebugTools.Assert(IsOn);
            DebugTools.Assert(_powerConsumer.DrawRate <= _powerConsumer.ReceivedPower);

            Fire();

            TimeSpan delay;
            if (_fireShotCounter < _fireBurstSize)
            {
                _fireShotCounter += 1;
                delay = _fireInterval;
            }
            else
            {
                _fireShotCounter = 0;
                var diff = _fireBurstDelayMax - _fireBurstDelayMin;
                // TIL you can do TimeSpan * double.
                delay = _fireBurstDelayMin + _robustRandom.NextFloat() * diff;
            }

            // Must be set while emitter powered.
            DebugTools.AssertNotNull(_timerCancel);
            Timer.Spawn(delay, ShotTimerCallback, _timerCancel!.Token);
        }

        private void Fire()
        {
            var projectile = Owner.EntityManager.SpawnEntity(_boltType, Owner.Transform.Coordinates);

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

            projectileComponent.IgnoreEntity(Owner);

            physicsComponent
                .LinearVelocity = Owner.Transform.WorldRotation.ToWorldVec() * 20f;
            projectile.Transform.WorldRotation = Owner.Transform.WorldRotation;

            // TODO: Move to projectile's code.
            Timer.Spawn(3000, () => projectile.Delete());

            SoundSystem.Play(Filter.Pvs(Owner), _fireSound, Owner,
                AudioHelpers.WithVariation(Variation).WithVolume(Volume).WithMaxDistance(Distance));
        }

        private void UpdateAppearance()
        {
            if (_appearance == null)
            {
                return;
            }

            EmitterVisualState state;
            if (_isPowered)
            {
                state = EmitterVisualState.On;
            }
            else if (IsOn)
            {
                state = EmitterVisualState.Underpowered;
            }
            else
            {
                state = EmitterVisualState.Off;
            }

            _appearance.SetData(EmitterVisuals.VisualState, state);
            _appearance.SetData(EmitterVisuals.Locked, _isLocked);
        }
    }
}
