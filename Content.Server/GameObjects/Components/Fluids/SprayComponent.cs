using System;
using System.Threading.Tasks;
using Content.Server.GameObjects.Components.Chemistry;
using Content.Shared.Audio;
using Content.Shared.Chemistry;
using Content.Shared.GameObjects.Components;
using Content.Shared.GameObjects.Components.Fluids;
using Content.Shared.GameObjects.Components.Items;
using Content.Shared.GameObjects.EntitySystems.ActionBlocker;
using Content.Shared.Interfaces;
using Content.Shared.Interfaces.GameObjects.Components;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Player;
using Robust.Shared.Timing;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Fluids
{
    [RegisterComponent]
    class SprayComponent : SharedSprayComponent, IAfterInteract, IUse, IActivate, IDropped
    {
        public const float SprayDistance = 3f;

        [Dependency] private readonly IGameTiming _gameTiming = default!;
        [Dependency] private readonly IServerEntityManager _serverEntityManager = default!;

        [DataField("transferAmount")]
        private ReagentUnit _transferAmount = ReagentUnit.New(10);
        [DataField("spraySound")]
        private string? _spraySound;
        [DataField("sprayVelocity")]
        private float _sprayVelocity = 1.5f;
        [DataField("sprayAliveTime")]
        private float _sprayAliveTime = 0.75f;
        private TimeSpan _lastUseTime;
        private TimeSpan _cooldownEnd;
        [DataField("cooldownTime")]
        private float _cooldownTime = 0.5f;
        [DataField("sprayedPrototype")]
        private string _vaporPrototype = "Vapor";
        [DataField("vaporAmount")]
        private int _vaporAmount = 1;
        [DataField("vaporSpread")]
        private float _vaporSpread = 90f;
        [DataField("hasSafety")]
        private bool _hasSafety;
        [DataField("safety")]
        private bool _safety = true;
        [DataField("impulse")]
        private float _impulse = 0f;

        /// <summary>
        ///     The amount of solution to be sprayer from this solution when using it
        /// </summary>
        [ViewVariables]
        public ReagentUnit TransferAmount
        {
            get => _transferAmount;
            set => _transferAmount = value;
        }

        /// <summary>
        ///     The speed at which the vapor starts when sprayed
        /// </summary>
        [ViewVariables]
        public float Velocity
        {
            get => _sprayVelocity;
            set => _sprayVelocity = value;
        }

        public string? SpraySound => _spraySound;

        public ReagentUnit CurrentVolume => Owner.GetComponentOrNull<SolutionContainerComponent>()?.CurrentVolume ?? ReagentUnit.Zero;

        public override void Initialize()
        {
            base.Initialize();

            Owner.EnsureComponentWarn(out SolutionContainerComponent _);

            if (_hasSafety)
            {
                SetSafety(Owner, _safety);
            }
        }

        async Task<bool> IAfterInteract.AfterInteract(AfterInteractEventArgs eventArgs)
        {
            if (!ActionBlockerSystem.CanInteract(eventArgs.User))
                return false;

            if (_hasSafety && _safety)
            {
                Owner.PopupMessage(eventArgs.User, Loc.GetString("Its safety is on!"));
                return true;
            }

            if (CurrentVolume <= 0)
            {
                Owner.PopupMessage(eventArgs.User, Loc.GetString("It's empty!"));
                return true;
            }

            var curTime = _gameTiming.CurTime;

            if(curTime < _cooldownEnd)
                return true;

            var playerPos = eventArgs.User.Transform.Coordinates;
            if (eventArgs.ClickLocation.GetGridId(_serverEntityManager) != playerPos.GetGridId(_serverEntityManager))
                return true;

            if (!Owner.TryGetComponent(out SolutionContainerComponent? contents))
                return true;

            var direction = (eventArgs.ClickLocation.Position - playerPos.Position).Normalized;
            var threeQuarters = direction * 0.75f;
            var quarter = direction * 0.25f;

            var amount = Math.Max(Math.Min((contents.CurrentVolume / _transferAmount).Int(), _vaporAmount), 1);

            var spread = _vaporSpread / amount;

            for (var i = 0; i < amount; i++)
            {
                var rotation = new Angle(direction.ToAngle() + Angle.FromDegrees(spread * i) - Angle.FromDegrees(spread * (amount-1)/2));

                var (_, diffPos) = eventArgs.ClickLocation - playerPos;
                var diffNorm = diffPos.Normalized;
                var diffLength = diffPos.Length;

                var target = eventArgs.User.Transform.Coordinates.Offset((diffNorm + rotation.ToVec()).Normalized * diffLength + quarter);

                if (target.TryDistance(Owner.EntityManager, playerPos, out var distance) && distance > SprayDistance)
                    target = eventArgs.User.Transform.Coordinates.Offset(diffNorm * SprayDistance);

                var solution = contents.SplitSolution(_transferAmount);

                if (solution.TotalVolume <= ReagentUnit.Zero)
                    break;

                var vapor = _serverEntityManager.SpawnEntity(_vaporPrototype, playerPos.Offset(distance < 1 ? quarter : threeQuarters));
                vapor.Transform.LocalRotation = rotation;

                if (vapor.TryGetComponent(out AppearanceComponent? appearance)) // Vapor sprite should face down.
                {
                    appearance.SetData(VaporVisuals.Rotation, -Angle.South + rotation);
                    appearance.SetData(VaporVisuals.Color, contents.Color.WithAlpha(1f));
                    appearance.SetData(VaporVisuals.State, true);
                }

                // Add the solution to the vapor and actually send the thing
                var vaporComponent = vapor.GetComponent<VaporComponent>();
                vaporComponent.TryAddSolution(solution);

                vaporComponent.Start(rotation.ToVec(), _sprayVelocity, target, _sprayAliveTime);

                if (_impulse > 0f && eventArgs.User.TryGetComponent(out IPhysBody? body))
                {
                    body.ApplyLinearImpulse(-direction * _impulse);
                }
            }

            //Play sound
            if (!string.IsNullOrEmpty(_spraySound))
            {
                SoundSystem.Play(Filter.Pvs(Owner), _spraySound, Owner, AudioHelpers.WithVariation(0.125f));
            }

            _lastUseTime = curTime;
            _cooldownEnd = _lastUseTime + TimeSpan.FromSeconds(_cooldownTime);

            if (Owner.TryGetComponent(out ItemCooldownComponent? cooldown))
            {
                cooldown.CooldownStart = _lastUseTime;
                cooldown.CooldownEnd = _cooldownEnd;
            }

            return true;
        }

        bool IUse.UseEntity(UseEntityEventArgs eventArgs)
        {
            ToggleSafety(eventArgs.User);
            return true;
        }

        void IActivate.Activate(ActivateEventArgs eventArgs)
        {
            ToggleSafety(eventArgs.User);
        }

        private void ToggleSafety(IEntity user)
        {
            SetSafety(user, !_safety);
        }

        private void SetSafety(IEntity user, bool state)
        {
            if (!ActionBlockerSystem.CanInteract(user) || !_hasSafety)
                return;

            _safety = state;

            if(Owner.TryGetComponent(out AppearanceComponent? appearance))
                appearance.SetData(SprayVisuals.Safety, _safety);
        }

        void IDropped.Dropped(DroppedEventArgs eventArgs)
        {
            if(_hasSafety && Owner.TryGetComponent(out AppearanceComponent? appearance))
                appearance.SetData(SprayVisuals.Safety, _safety);
        }
    }
}
