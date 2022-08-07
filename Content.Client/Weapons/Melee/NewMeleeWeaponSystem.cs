using Content.Shared.Weapons.Melee;
using Content.Shared.Weapons.Ranged.Events;
using Robust.Client.GameObjects;
using Robust.Client.Player;
using Robust.Shared.Input;
using Robust.Shared.Map;

namespace Content.Client.Weapons.Melee;

public sealed class NewMeleeWeaponSystem : SharedNewMeleeWeaponSystem
{
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly InputSystem _inputSystem = default!;

    public override void Update(float frameTime)
    {
        if (!Timing.IsFirstTimePredicted)
            return;

        var entityNull = _player.LocalPlayer?.ControlledEntity;

        if (entityNull == null)
        {
            return;
        }

        var entity = entityNull.Value;
        var weapon = GetWeapon(entity);

        if (weapon == null)
        {
            return;
        }

        if (_inputSystem.CmdStates.GetState(EngineKeyFunctions.Use) != BoundKeyState.Down)
        {
            if (weapon.WindupAccumulator > 0)
            {
                EntityManager.RaisePredictiveEvent(new ReleaseAttackEvent()
                {
                    Weapon = weapon.Owner,
                    AsAttack = true,
                });
            }

            return;
        }

        weapon.WindupAccumulator = MathF.Min(weapon.WindupTime, weapon.WindupAccumulator + frameTime);

        if (weapon.NextFire > Timing.CurTime)
            return;

        var mousePos = _eyeManager.ScreenToMap(_inputManager.MouseScreenPosition);
        EntityCoordinates coordinates;

        // Bro why would I want a ternary here
        // ReSharper disable once ConvertIfStatementToConditionalTernaryExpression
        if (MapManager.TryFindGridAt(mousePos, out var grid))
        {
            coordinates = EntityCoordinates.FromMap(grid.GridEntityId, mousePos, EntityManager);
        }
        else
        {
            coordinates = EntityCoordinates.FromMap(MapManager.GetMapEntityId(mousePos.MapId), mousePos, EntityManager);
        }

        Sawmill.Debug($"Sending shoot request tick {Timing.CurTick} / {Timing.CurTime}");

        EntityManager.RaisePredictiveEvent(new RequestShootEvent
        {
            Coordinates = coordinates,
            Gun = weapon.Owner,
        });
    }
}
