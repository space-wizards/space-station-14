using Content.Client.Viewport;
using Content.Shared.CombatMode;
using Content.Shared.Weapon.Melee;
using Content.Shared.Weapons.Melee;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.Player;
using Robust.Client.State;
using Robust.Shared.Input;
using Robust.Shared.Map;
using Robust.Shared.Player;

namespace Content.Client.Weapons.Melee;

public sealed class NewMeleeWeaponSystem : SharedNewMeleeWeaponSystem
{
    [Dependency] private readonly IEyeManager _eyeManager = default!;
    [Dependency] private readonly IInputManager _inputManager = default!;
    [Dependency] private readonly IOverlayManager _overlayManager = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IStateManager _stateManager = default!;
    [Dependency] private readonly InputSystem _inputSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        _overlayManager.AddOverlay(new MeleeWindupOverlay());
    }

    public override void Shutdown()
    {
        base.Shutdown();
        _overlayManager.RemoveOverlay<MeleeWindupOverlay>();
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (!Timing.IsFirstTimePredicted)
            return;

        var entityNull = _player.LocalPlayer?.ControlledEntity;

        if (!TryComp<SharedCombatModeComponent>(entityNull, out var combatMode))
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
            if (weapon.WindupAccumulator > 0f)
            {
                // Active + windupaccumulator handled in the event handlers.

                if (weapon.WindupAccumulator < weapon.WindupTime)
                {
                    EntityManager.RaisePredictiveEvent(new StopAttackEvent()
                    {
                        Weapon = weapon.Owner,
                    });
                }
                else
                {
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

                    if (combatMode.PrecisionMode)
                    {
                        EntityUid? target = null;

                        // TODO: UI Refactor update I assume
                        if (_stateManager.CurrentState is GameScreen screen)
                        {
                            target = screen.GetEntityUnderPosition(
                                _eyeManager.ScreenToMap(_inputManager.MouseScreenPosition));
                        }

                        EntityManager.RaisePredictiveEvent(new ReleasePreciseAttackEvent()
                        {
                            Weapon = weapon.Owner,
                            Target = target ?? EntityUid.Invalid,
                        });
                    }
                    else
                    {
                        EntityManager.RaisePredictiveEvent(new ReleaseWideAttackEvent()
                        {
                            Weapon = weapon.Owner,
                            Coordinates = coordinates,
                        });
                    }
                }
            }

            return;
        }

        // Started a windup
        if (weapon.WindupAccumulator.Equals(0f))
        {
            EntityManager.RaisePredictiveEvent(new StartAttackEvent()
            {
                Weapon = weapon.Owner,
            });
        }
    }

    protected override void Popup(string message, EntityUid? uid, EntityUid? user)
    {
        if (!Timing.IsFirstTimePredicted || uid == null)
            return;

        PopupSystem.PopupEntity(message, uid.Value, Filter.Local());
    }
}
