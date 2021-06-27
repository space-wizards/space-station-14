using System;
using Content.Client.CombatMode;
using Content.Shared.Audio;
using Content.Shared.Camera;
using Content.Shared.Hands.Components;
using Content.Shared.Weapons.Guns;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.Player;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.Input;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Player;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Client.Weapons.Guns
{
    internal sealed class GunSystem : SharedGunSystem
    {
        [Dependency] private readonly IEyeManager _eyeManager = default!;
        [Dependency] private readonly IInputManager _inputManager = default!;
        [Dependency] private readonly IPlayerManager _playerManager = default!;

        private CombatModeSystem _combatModeSystem = default!;
        private InputSystem _inputSystem = default!;

        private SharedGunComponent? _firingWeapon;

        public override void Initialize()
        {
            base.Initialize();
            _combatModeSystem = Get<CombatModeSystem>();
            _inputSystem = Get<InputSystem>();
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);
            GunUpdate(frameTime, GameTiming.InSimulation || GameTiming.IsFirstTimePredicted);
        }

        public override void FrameUpdate(float frameTime)
        {
            base.FrameUpdate(frameTime);
            GunUpdate(frameTime, true);
        }

        private void StopFiring(TimeSpan currentTime)
        {
            if (_firingWeapon != null)
                _firingWeapon.Firing = false;

            _firingWeapon = null;
        }

        private SharedGunComponent? GetRangedWeapon(IEntity entity)
        {
            if (!entity.TryGetComponent(out SharedHandsComponent? handsComponent) ||
                !handsComponent.TryGetActiveHeldEntity(out var item) ||
                !item.TryGetComponent(out SharedGunComponent? gunComponent)) return null;

            return gunComponent;
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

            if (!_firingWeapon.Firing)
            {
                // TODO: Set Firing on weapon?
                _firingWeapon.NextFire = TimeSpan.FromSeconds(Math.Max(_firingWeapon.NextFire.TotalSeconds, currentTime.TotalSeconds));
                _firingWeapon.Firing = true;
            }

            var mouseCoordinates = _eyeManager.ScreenToMap(_inputManager.MouseScreenPosition);
            var fireAngle = (mouseCoordinates.Position - player.Transform.WorldPosition).ToAngle();

            if (TryFire(player, _firingWeapon, mouseCoordinates, out var shots, currentTime) && shots > 0)
            {
                if (prediction)
                {
                    /*
                    var kickBack = _firingWeapon.KickBack;

                    if (kickBack > 0.0f && player.TryGetComponent(out SharedCameraRecoilComponent? cameraRecoil))
                    {
                        cameraRecoil.Kick(-fireAngle.ToVec() * kickBack * shots);
                    }
                    */

                    Logger.DebugS("gun", $"Fired {shots} shots at {currentTime}");
                    //RaiseNetworkEvent(new ShootMessage(_firingWeapon.Owner.Uid, mouseCoordinates, shots, currentTime));

                    if (_firingWeapon.SoundGunshot != null)
                    {
                        for (var i = 0; i < shots; i++)
                        {
                            SoundSystem.Play(Filter.Local(), _firingWeapon.SoundGunshot,
                                AudioHelpers.WithVariation(0.01f));
                        }
                    }

                    // TODO: Muzzle Effect
                }
            }
            else
            {
                StopFiring(currentTime);
            }
        }
    }
}
