#nullable enable
using Content.Server.GameObjects.Components.Atmos;
using Content.Server.GameObjects.Components.Items.Clothing;
using Content.Server.GameObjects.Components.Items.Storage;
using Content.Shared.Actions;
using Content.Shared.Atmos;
using Content.Shared.GameObjects.Components.Movement;
using Content.Shared.GameObjects.EntitySystems.ActionBlocker;
using Content.Shared.Interfaces.GameObjects.Components;
using Content.Shared.Physics;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Server.GameObjects.EntitySystems;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.ComponentDependencies;
using Robust.Shared.GameObjects.Components;
using Robust.Shared.GameObjects.Components.Transform;
using Robust.Shared.GameObjects.EntitySystemMessages;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Timing;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Players;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;
using System;
using System.Collections.Generic;
using Robust.Shared.Log;

namespace Content.Server.GameObjects.Components.Jetpack
{
    [RegisterComponent]
    public class JetpackComponent : Component, IEquipped, IUnequipped
    {
        public override string Name => "Jetpack";

        [ViewVariables(VVAccess.ReadWrite)] private float VolumeUsage { get; set; } = Atmospherics.BreathVolume;

        [ViewVariables] [ComponentDependency] private readonly GasTankComponent? _gasTank = default!;

        [ComponentDependency] private readonly ItemComponent? _itemComponent = default!;

        [ComponentDependency] private readonly SpriteComponent? _sprite = default!;

        [ComponentDependency] private readonly ClothingComponent? _clothing = default!;

        [Dependency] private readonly IGameTiming _gameTiming = default!;

        private bool _active;

        private float _jetpackSpeed = 5f;

        private IEntity? _user;

        private EffectSystem? _effectSystem;

        private readonly TimeSpan _effectCooldown = TimeSpan.FromSeconds(0.3);
        private TimeSpan _lastEffectTime = TimeSpan.FromSeconds(0);

        private readonly IDictionary<Direction, bool> _dirStates = new Dictionary<Direction, bool>();

        [ViewVariables(VVAccess.ReadWrite)]
        public bool Active
        {
            get => _active;
            private set
            {
                _active = value;
                var state = "icon" + (Active ? "-on" : "");
                _sprite?.LayerSetState(0, state);
                if (_clothing != null)
                    _clothing.ClothingEquippedPrefix = Active ? "on" : null;
                if (_user == null || !_user.TryGetComponent<IPhysicsComponent>(out var physics)) return;
                if (Active)
                {
                    physics.EnsureController<JetpackController>();
                }
                else
                {
                    physics.TryRemoveController<JetpackController>();
                }
            }
        }

        public override void Initialize()
        {
            base.Initialize();
            _effectSystem = EntitySystem.Get<EffectSystem>();

            if (_itemComponent != null) _itemComponent.OnInventoryRelayMove += OnInventoryRelayMove;
        }

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
            serializer.DataField(ref _jetpackSpeed, "jetpackSpeed", 5f);
        }

        public void HandleMoveEvent(MoveEvent moveEvent)
        {
            // If the jetpack is equipped and the _user is on a tile with no gravity, we create the particles
            if (Active && _effectCooldown + _lastEffectTime < _gameTiming.CurTime && _user != null &&
                _user.IsWeightless())
            {
                CreateParticles(moveEvent.NewPosition);
            }
        }

        private void CreateParticles(EntityCoordinates coordinates)
        {
            var startTime = _gameTiming.CurTime;
            var deathTime = startTime + TimeSpan.FromSeconds(2);
            var effect = new EffectSystemMessage
            {
                EffectSprite = "Effects/atmospherics.rsi",
                Born = startTime,
                DeathTime = deathTime,
                Coordinates = coordinates,
                RsiState = "freon_old",
                Color = Vector4.Multiply(new Vector4(255, 255, 255, 125), 1.0f),
                ColorDelta = Vector4.Multiply(new Vector4(0, 0, 0, -10), 1.0f),
                AnimationLoops = true
            };
            _effectSystem?.CreateParticle(effect);
            _lastEffectTime = _gameTiming.CurTime;
        }

        public void Update()
        {
            if (!Active)
                return;
            if (_gasTank?.Air?.Pressure <= VolumeUsage)
                Active = false;
            foreach (var (dir, state) in _dirStates)
            {
                DoMovement(dir, state);
            }
        }

        public void ToggleJetpack()
        {
            if (!Active && _gasTank?.Air?.Pressure <= VolumeUsage)
                return;
            Active = !Active;
        }

        void IEquipped.Equipped(EquippedEventArgs eventArgs)
        {
            _user = eventArgs.User;
            if (Active && _user.TryGetComponent<IPhysicsComponent>(out var physics))
            {
                physics.EnsureController<JetpackController>();
            }
        }

        void IUnequipped.Unequipped(UnequippedEventArgs eventArgs)
        {
            _user = null;
        }

        private void OnInventoryRelayMove(ICommonSession session, Direction dir, bool state)
        {
            _dirStates[dir] = state;
            DoMovement(dir, state);
        }

        private void DoMovement(Direction dir, bool state)
        {
            if (!Active || !state || _user == null ||
                !_user.TryGetComponent<IPhysicsComponent>(out var physics)) return;
            if (ActionBlockerSystem.CanMove(_user) && _user.IsWeightless())
            {
                var controller = physics.EnsureController<JetpackController>();
                var newVel = controller.LinearVelocity.GetDir().ToVec() + dir.ToVec();
                controller.Push(newVel, _jetpackSpeed);
                _user.Transform.LocalRotation = dir.ToAngle();
                _gasTank?.RemoveAirVolume(VolumeUsage);
            }
            else if (!_user.IsWeightless())
            {
                physics.TryRemoveController<JetpackController>();
            }
        }
    }

    [UsedImplicitly]
    public sealed class ToggleJetpackAction : IToggleItemAction
    {
        public void ExposeData(ObjectSerializer serializer) { }

        public bool DoToggleAction(ToggleItemActionEventArgs args)
        {
            if (!args.Item.TryGetComponent<JetpackComponent>(out var jetpackComponent)) return false;
            // no change
            if (jetpackComponent.Active == args.ToggledOn) return false;
            jetpackComponent.ToggleJetpack();
            // did we successfully toggle to the desired status?
            return jetpackComponent.Active == args.ToggledOn;
        }
    }
}
