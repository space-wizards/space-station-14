#nullable enable
using System;
using Content.Client.GameObjects.Components.Items;
using Content.Shared.GameObjects.Components.Projectiles;
using Content.Shared.GameObjects.Components.Weapons.Ranged;
using Content.Shared.GameObjects.Components.Weapons.Ranged.Barrels;
using Content.Shared.GameObjects.EntitySystems;
using JetBrains.Annotations;
using Robust.Client.GameObjects.EntitySystems;
using Robust.Client.Interfaces.Graphics.ClientEye;
using Robust.Client.Interfaces.Input;
using Robust.Client.Player;
using Robust.Shared.GameObjects.EntitySystemMessages;
using Robust.Shared.Input;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Timing;
using Robust.Shared.IoC;
using Robust.Shared.Maths;

namespace Content.Client.GameObjects.EntitySystems
{
    [UsedImplicitly]
    internal sealed class RangedWeaponSystem : SharedRangedWeaponSystem
    {
        [Dependency] private readonly IPlayerManager _playerManager = default!;
        [Dependency] private readonly IEyeManager _eyeManager = default!;
        [Dependency] private readonly IInputManager _inputManager = default!;
        [Dependency] private readonly IGameTiming _gameTiming = default!;

        private InputSystem _inputSystem = default!;
        private CombatModeSystem _combatModeSystem = default!;
        private SharedRangedWeaponComponent? _firingWeapon;
        
        private bool _lastFireResult = true;

        public override void Initialize()
        {
            base.Initialize();

            IoCManager.InjectDependencies(this);
            _inputSystem = EntitySystemManager.GetEntitySystem<InputSystem>();
            _combatModeSystem = EntitySystemManager.GetEntitySystem<CombatModeSystem>();
        }

        private SharedRangedWeaponComponent? GetRangedWeapon(IEntity entity)
        {
            if (!entity.TryGetComponent(out HandsComponent? hands))
                return null;
            

            var held = hands.ActiveHand;
            if (held == null || !held.TryGetComponent(out SharedRangedWeaponComponent? weapon))
                return null;

            return weapon;
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            if (!_gameTiming.IsFirstTimePredicted)
                return;

            var currentTime = _gameTiming.CurTime;
            var state = _inputSystem.CmdStates.GetState(EngineKeyFunctions.Use);
            if (!_combatModeSystem.IsInCombatMode() || state != BoundKeyState.Down)
            {
                // Result this so we can queue up more firing.
                _lastFireResult = false;
                
                if (_firingWeapon != null)
                {
                    StopFiring(_firingWeapon);
                    _firingWeapon.ShotCounter = 0;
                    _firingWeapon = null;
                }
                
                return;
            }

            var player = _playerManager.LocalPlayer?.ControlledEntity;
            if (player == null)
                return;

            var lastFiringWeapon = _firingWeapon;
            _firingWeapon = GetRangedWeapon(player);

            if (lastFiringWeapon != _firingWeapon && lastFiringWeapon != null)
                StopFiring(lastFiringWeapon);

            if (_firingWeapon == null)
                return;
            
            var mouseCoordinates = _eyeManager.ScreenToMap(_inputManager.MouseScreenPosition);

            if (_firingWeapon.TryFire(currentTime, player, mouseCoordinates, out var shots))
            {
                // First shot we'll send timestamp
                if (!_firingWeapon.Firing)
                {
                    _firingWeapon.Firing = true;
                    RaiseNetworkEvent(new StartFiringMessage(_firingWeapon.Owner.Uid, mouseCoordinates));
                } 
                else if (shots > 0)
                {
                    RaiseNetworkEvent(new RangedFireMessage(_firingWeapon.Owner.Uid, mouseCoordinates));
                }
            }
            else
            {
                StopFiring(_firingWeapon);
            }
        }
        
        private void StopFiring(SharedRangedWeaponComponent weaponComponent)
        {
            if (weaponComponent.Firing)
                RaiseNetworkEvent(new StopFiringMessage(weaponComponent.Owner.Uid, weaponComponent.ShotCounter));

            weaponComponent.Firing = false;
        }
        
        public override void MuzzleFlash(IEntity? user, SharedRangedWeaponComponent weapon, Angle angle, TimeSpan? currentTime = null, bool predicted = true, float alphaRatio = 1)
        {
            var texture = weapon.MuzzleFlash;
            if (texture == null || !predicted)
                return;
            
            var offset = angle.ToVec().Normalized / 2;

            var message = new EffectSystemMessage
            {
                EffectSprite = texture,
                Born = _gameTiming.CurTime,
                DeathTime = _gameTiming.CurTime + TimeSpan.FromSeconds(0.2),
                AttachedEntityUid = weapon.Owner.Uid,
                AttachedOffset = offset,
                //Rotated from east facing
                Rotation = (float) angle.Theta,
                Color = Vector4.Multiply(new Vector4(255, 255, 255, 750), alphaRatio),
                ColorDelta = new Vector4(0, 0, 0, -1500f),
                Shaded = false
            };

            RaiseLocalEvent(message);
        }

        // TODO: Won't be used until container prediction
        public override void EjectCasing(IEntity? user, IEntity casing, bool playSound = true, Direction[]? ejectDirections = null)
        {
            throw new InvalidOperationException();
        }

        public override void ShootHitscan(IEntity? user, SharedRangedWeaponComponent weapon, HitscanPrototype hitscan, Angle angle, float damageRatio = 1, float alphaRatio = 1)
        {
            throw new NotImplementedException();
        }

        public override void ShootAmmo(IEntity? user, SharedRangedWeaponComponent weapon, Angle angle, SharedAmmoComponent ammoComponent)
        {
            throw new NotImplementedException();
        }

        public override void ShootProjectile(IEntity? user, SharedRangedWeaponComponent weapon, Angle angle, SharedProjectileComponent projectileComponent, float velocity)
        {
            throw new NotImplementedException();
        }
    }
}
