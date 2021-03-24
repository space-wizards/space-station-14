using System;
using Content.Client.GameObjects.Components.Items;
using Content.Client.GameObjects.Components.Weapons.Ranged;
using Content.Shared.GameObjects.Components.Items;
using Content.Shared.GameObjects.Components.Weapons.Ranged;
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

namespace Content.Client.GameObjects.EntitySystems
{
    [UsedImplicitly]
    public class RangedWeaponSystem : EntitySystem
    {
        [Dependency] private readonly IPlayerManager _playerManager = default!;
        [Dependency] private readonly IEyeManager _eyeManager = default!;
        [Dependency] private readonly IMapManager _mapManager = default!;
        [Dependency] private readonly IInputManager _inputManager = default!;
        [Dependency] private readonly IGameTiming _gameTiming = default!;

        private InputSystem _inputSystem = default!;
        private CombatModeSystem _combatModeSystem = default!;
        private bool _blocked;
        private int _shotCounter;

        public override void Initialize()
        {
            base.Initialize();

            IoCManager.InjectDependencies(this);
            _inputSystem = Get<InputSystem>();
            _combatModeSystem = Get<CombatModeSystem>();
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
            if (entity == null || !entity.TryGetComponent(out SharedHandsComponent? hands))
            {
                return;
            }

            if (!hands.TryGetActiveHeldEntity(out var held) || !held.TryGetComponent(out ClientRangedWeaponComponent? weapon))
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
            {
                return;
            }

            var worldPos = _eyeManager.ScreenToMap(_inputManager.MouseScreenPosition);

            if (!_mapManager.TryFindGridAt(worldPos, out var grid))
            {
                weapon.SyncFirePos(GridId.Invalid, worldPos.Position);
            }
            else
            {
                weapon.SyncFirePos(grid.Index, grid.MapToGrid(worldPos).Position);
            }
        }
    }
}
