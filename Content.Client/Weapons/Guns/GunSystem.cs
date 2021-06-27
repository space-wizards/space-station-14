using System;
using System.Runtime.CompilerServices;
using Content.Client.CombatMode;
using Content.Shared.Camera;
using Content.Shared.Weapons.Guns;
using Robust.Client.GameObjects;
using Robust.Shared.Input;
using Robust.Shared.Timing;

namespace Content.Client.Weapons.Guns
{
    internal sealed class GunSystem : SharedGunSystem
    {
        private CombatModeSystem _combatModeSystem = default!;
        private InputSystem _inputSystem = default!;

        public override void Initialize()
        {
            base.Initialize();
            _combatModeSystem = Get<CombatModeSystem>();
            _inputSystem = Get<InputSystem>();
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);
        }

        public override void FrameUpdate(float frameTime)
        {
            base.FrameUpdate(frameTime);
        }

        private void GunUpdate(float frametime, bool prediction)
        {
            var currentTime = GameTiming.CurTime;
            var state = _inputSystem.CmdStates.GetState(EngineKeyFunctions.Use);

            if (!_combatModeSystem.IsInCombatMode() || state != BoundKeyState.Down)
            {
                StopFiring(currentTime);
                _firingWeapon = null;
                return;
            }

            var player = _playerManager.LocalPlayer?.ControlledEntity;
            if (player == null)
                return;

            var lastFiringWeapon = _firingWeapon;
            _firingWeapon = GetRangedWeapon(player);

            if (lastFiringWeapon != _firingWeapon && lastFiringWeapon != null)
            {
                StopFiring(currentTime);
            }

            if (_firingWeapon == null)
                return;

            if (!_firing)
            {
                // TODO: Set Firing on weapon?
                _firingWeapon.NextFire = TimeSpan.FromSeconds(Math.Max(_firingWeapon.NextFire.TotalSeconds, currentTime.TotalSeconds));
                _firing = true;
            }

            var mouseCoordinates = _eyeManager.ScreenToMap(_inputManager.MouseScreenPosition);
            var fireAngle = (mouseCoordinates.Position - player.Transform.WorldPosition).ToAngle();

            if (TryFire(player, _firingWeapon, mouseCoordinates, out var shots, currentTime) && shots > 0)
            {
                switch (_firingWeapon)
                {
                    case ChamberedGunComponent chamberedGun:
                        var mag = chamberedGun.Magazine;

                        EntityManager.EventBus.RaiseLocalEvent(
                            _firingWeapon.Owner.Uid,
                            new AmmoUpdateEvent(chamberedGun.Chamber != null, mag?.AmmoCount, mag?.AmmoMax));

                        break;
                }

                if (_prediction)
                {
                    var kickBack = _firingWeapon.KickBack;

                    if (kickBack > 0.0f && player.TryGetComponent(out SharedCameraRecoilComponent? cameraRecoil))
                    {
                        cameraRecoil.Kick(-fireAngle.ToVec() * kickBack * shots);
                    }

                    Logger.DebugS("gun", $"Fired {shots} shots at {currentTime}");
                    RaiseNetworkEvent(new ShootMessage(_firingWeapon.Owner.Uid, mouseCoordinates, shots, currentTime));
                }
            }
        }
    }
}
