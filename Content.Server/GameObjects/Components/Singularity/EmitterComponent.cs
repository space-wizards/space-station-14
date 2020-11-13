using System;
using System.Threading;
using System.Threading.Tasks;
using Content.Server.GameObjects.Components.Access;
using Content.Server.GameObjects.Components.Power.PowerNetComponents;
using Content.Server.GameObjects.Components.Projectiles;
using Content.Server.Interfaces;
using Content.Server.Utility;
using Content.Shared.GameObjects.Components.Singularity;
using Content.Shared.Interfaces;
using Content.Shared.Interfaces.GameObjects.Components;
using Content.Shared.Physics;
using Robust.Server.GameObjects;
using Robust.Server.GameObjects.EntitySystems;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.ComponentDependencies;
using Robust.Shared.GameObjects.Components;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Random;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Log;
using Robust.Shared.Random;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;
using Timer = Robust.Shared.Timers.Timer;

#nullable enable

namespace Content.Server.GameObjects.Components.Singularity
{
    [RegisterComponent]
    [ComponentReference(typeof(IActivate))]
    public class EmitterComponent : Component, IActivate, IInteractUsing
    {
        [Dependency] private readonly IEntityManager _entityManager = default!;
        [Dependency] private readonly IRobustRandom _robustRandom = default!;

        [ComponentDependency] private AppearanceComponent? _appearance = default;
        [ComponentDependency] private AccessReader? _accessReader = default;

        public override string Name => "Emitter";

        private CancellationTokenSource? _timerCancel;

        private PhysicsComponent _collidableComponent = default!;
        private PowerConsumerComponent _powerConsumer = default!;

        // whether the power switch is in "on"
        [ViewVariables] private bool _isOn;
        // Whether the power switch is on AND the machine has enough power (so is actively firing)
        [ViewVariables] private bool _isPowered;
        [ViewVariables] private bool _isLocked;

        [ViewVariables(VVAccess.ReadWrite)] private int _fireShotCounter;

        [ViewVariables(VVAccess.ReadWrite)] private string _fireSound = default!;
        [ViewVariables(VVAccess.ReadWrite)] private string _boltType = default!;
        [ViewVariables(VVAccess.ReadWrite)] private int _powerUseActive;
        [ViewVariables(VVAccess.ReadWrite)] private int _fireBurstSize;
        [ViewVariables(VVAccess.ReadWrite)] private TimeSpan _fireInterval;
        [ViewVariables(VVAccess.ReadWrite)] private TimeSpan _fireBurstDelayMin;
        [ViewVariables(VVAccess.ReadWrite)] private TimeSpan _fireBurstDelayMax;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(ref _fireBurstDelayMin, "fireBurstDelayMin", TimeSpan.FromSeconds(2));
            serializer.DataField(ref _fireBurstDelayMax, "fireBurstDelayMax", TimeSpan.FromSeconds(10));
            serializer.DataField(ref _fireInterval, "fireInterval", TimeSpan.FromSeconds(2));
            serializer.DataField(ref _fireBurstSize, "fireBurstSize", 3);
            serializer.DataField(ref _powerUseActive, "powerUseActive", 500);
            serializer.DataField(ref _boltType, "boltType", "EmitterBolt");
            serializer.DataField(ref _fireSound, "fireSound", "/Audio/Weapons/emitter.ogg");
        }

        public override void Initialize()
        {
            base.Initialize();

            if (!Owner.TryGetComponent(out _collidableComponent!))
            {
                Logger.Error($"EmitterComponent {Owner} created with no CollidableComponent");
                return;
            }

            if (!Owner.TryGetComponent(out _powerConsumer!))
            {
                Logger.Error($"EmitterComponent {Owner} created with no PowerConsumerComponent");
                return;
            }

            _collidableComponent.AnchoredChanged += OnAnchoredChanged;
            _powerConsumer.OnReceivedPowerChanged += OnReceivedPowerChanged;
        }

        private void OnReceivedPowerChanged(object? sender, ReceivedPowerChangedEventArgs e)
        {
            if (!_isOn)
            {
                return;
            }

            if (e.ReceivedPower < e.DrawRate)
            {
                PowerOff();
            }
            else
            {
                PowerOn();
            }
        }

        private void OnAnchoredChanged()
        {
            if (_collidableComponent.Anchored)
                Owner.SnapToGrid();
        }

        void IActivate.Activate(ActivateEventArgs eventArgs)
        {
            if (_isLocked)
            {
                Owner.PopupMessage(eventArgs.User, Loc.GetString("{0:TheName} is access locked!", Owner));
                return;
            }

            if (!_isOn)
            {
                SwitchOn();
                Owner.PopupMessage(eventArgs.User, Loc.GetString("{0:TheName} turns on.", Owner));
            }
            else
            {
                SwitchOff();
                Owner.PopupMessage(eventArgs.User, Loc.GetString("{0:TheName} turns off.", Owner));
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
                    Owner.PopupMessage(eventArgs.User, Loc.GetString("You lock {0:TheName}.", Owner));
                }
                else
                {
                    Owner.PopupMessage(eventArgs.User, Loc.GetString("You unlock {0:TheName}.", Owner));
                }

                UpdateAppearance();
            }
            else
            {
                Owner.PopupMessage(eventArgs.User, Loc.GetString("Access denied."));
            }

            return Task.FromResult(true);
        }

        private void SwitchOff()
        {
            _isOn = false;
            _powerConsumer.DrawRate = 0;
            PowerOff();
            UpdateAppearance();
        }

        private void SwitchOn()
        {
            _isOn = true;
            _powerConsumer.DrawRate = _powerUseActive;
            // Do not directly PowerOn().
            // OnReceivedPowerChanged will get fired due to DrawRate change which will turn it on.
            UpdateAppearance();
        }

        private void PowerOff()
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

        private void PowerOn()
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
            DebugTools.Assert(_isOn);
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
            var projectile = _entityManager.SpawnEntity(_boltType, Owner.Transform.Coordinates);

            if (!projectile.TryGetComponent<PhysicsComponent>(out var physicsComponent))
            {
                Logger.Error("Emitter tried firing a bolt, but it was spawned without a CollidableComponent");
                return;
            }

            physicsComponent.Status = BodyStatus.InAir;

            if (!projectile.TryGetComponent<ProjectileComponent>(out var projectileComponent))
            {
                Logger.Error("Emitter tried firing a bolt, but it was spawned without a ProjectileComponent");
                return;
            }

            projectileComponent.IgnoreEntity(Owner);

            physicsComponent
                .EnsureController<BulletController>()
                .LinearVelocity = Owner.Transform.WorldRotation.ToVec() * 20f;

            projectile.Transform.LocalRotation = Owner.Transform.WorldRotation;

            // TODO: Move to projectile's code.
            Timer.Spawn(3000, () => projectile.Delete());

            EntitySystem.Get<AudioSystem>().PlayFromEntity(_fireSound, Owner);
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
            else if (_isOn)
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
