using System;
using Content.Client.CombatMode;
using Content.Shared.Hands.Components;
using Content.Shared.Weapons.Ranged.Components;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.Input;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Timing;

namespace Content.Client.Weapons.Ranged
{
    [UsedImplicitly]
    public sealed class RangedWeaponSystem : EntitySystem
    {
        [Dependency] private readonly IPlayerManager _playerManager = default!;
        [Dependency] private readonly IEyeManager _eyeManager = default!;
        [Dependency] private readonly IMapManager _mapManager = default!;
        [Dependency] private readonly IInputManager _inputManager = default!;
        [Dependency] private readonly IGameTiming _gameTiming = default!;
        [Dependency] private readonly InputSystem _inputSystem = default!;
        [Dependency] private readonly CombatModeSystem _combatModeSystem = default!;

        private bool _blocked;
        private int _shotCounter;

        public override void Initialize()
        {
            base.Initialize();

            UpdatesOutsidePrediction = true;
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            if (!_gameTiming.IsFirstTimePredicted)
            {
                return;
            }

            var state = _inputSystem.CmdStates.GetState(EngineKeyFunctions.Use);
            if (!_combatModeSystem.IsInCombatMode() || state != BoundKeyState.Down)
            {
                _shotCounter = 0;
                _blocked = false;
                return;
            }

            var entity = _playerManager.LocalPlayer?.ControlledEntity;
            if (!EntityManager.TryGetComponent(entity, out SharedHandsComponent? hands))
            {
                return;
            }

            if (!hands.TryGetActiveHeldEntity(out var held) || !EntityManager.TryGetComponent(held, out ClientRangedWeaponComponent? weapon))
            {
                _blocked = true;
                return;
            }

            switch (weapon.FireRateSelector)
            {
                case FireRateSelector.Safety:
                    _blocked = true;
                    return;
                case FireRateSelector.Single:
                    if (_shotCounter >= 1)
                    {
                        _blocked = true;
                        return;
                    }

                    break;
                case FireRateSelector.Automatic:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            if (_blocked)
                return;

            var mapCoordinates = _eyeManager.ScreenToMap(_inputManager.MouseScreenPosition);
            EntityCoordinates coordinates;

            if (_mapManager.TryFindGridAt(mapCoordinates, out var grid))
            {
                coordinates = EntityCoordinates.FromMap(grid.GridEntityId, mapCoordinates);
            }
            else
            {
                coordinates = EntityCoordinates.FromMap(_mapManager.GetMapEntityId(mapCoordinates.MapId), mapCoordinates);
            }

            SyncFirePos(coordinates);
        }

        private void SyncFirePos(EntityCoordinates coordinates)
        {
            RaiseNetworkEvent(new FirePosEvent(coordinates));
        }
    }
}
