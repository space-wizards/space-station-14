using Content.Client.CombatMode;
using Content.Client.Gameplay;
using Content.Client.Hands;
using Content.Client.Weapons.Melee.Components;
using Content.Shared.MobState.Components;
using Content.Shared.Weapons.Melee;
using Content.Shared.Weapons.Melee.Events;
using Robust.Client.Animations;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.Player;
using Robust.Client.ResourceManagement;
using Robust.Client.State;
using Robust.Shared.Animations;
using Robust.Shared.Input;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Client.Weapons.Melee;

public sealed partial class MeleeWeaponSystem : SharedMeleeWeaponSystem
{
    [Dependency] private readonly IEyeManager _eyeManager = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IInputManager _inputManager = default!;
    [Dependency] private readonly IOverlayManager _overlayManager = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IPrototypeManager _protoManager = default!;
    [Dependency] private readonly IResourceCache _cache = default!;
    [Dependency] private readonly IStateManager _stateManager = default!;
    [Dependency] private readonly AnimationPlayerSystem _animation = default!;
    [Dependency] private readonly InputSystem _inputSystem = default!;

    private const string MeleeLungeKey = "melee-lunge";

    public override void Initialize()
    {
        base.Initialize();
        InitializeEffect();
        _overlayManager.AddOverlay(new MeleeWindupOverlay(EntityManager, _timing, _player, _protoManager, _cache));
        SubscribeNetworkEvent<DamageEffectEvent>(OnDamageEffect);
        SubscribeNetworkEvent<MeleeLungeEvent>(OnMeleeLunge);
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

        if (entityNull == null)
            return;

        var entity = entityNull.Value;
        var weapon = GetWeapon(entity);

        if (weapon == null)
            return;

        if (!CombatMode.IsInCombatMode(entity) || !Blocker.CanAttack(entity))
        {
            weapon.Attacking = false;
            if (weapon.WindUpStart != null)
            {
                EntityManager.RaisePredictiveEvent(new StopHeavyAttackEvent(weapon.Owner));
            }

            return;
        }

        var useDown = _inputSystem.CmdStates.GetState(EngineKeyFunctions.Use);
        var altDown = _inputSystem.CmdStates.GetState(EngineKeyFunctions.UseSecondary);
        var currentTime = Timing.CurTime;

        // Heavy attack.
        if (altDown == BoundKeyState.Down)
        {
            // We did the click to end the attack but haven't pulled the key up.
            if (weapon.Attacking)
            {
                return;
            }

            // If it's an unarmed attack then do a disarm
            if (weapon.Owner == entity)
            {
                EntityUid? target = null;

                var mousePos = _eyeManager.ScreenToMap(_inputManager.MouseScreenPosition);
                EntityCoordinates coordinates;

                if (MapManager.TryFindGridAt(mousePos, out var grid))
                {
                    coordinates = EntityCoordinates.FromMap(grid.GridEntityId, mousePos, EntityManager);
                }
                else
                {
                    coordinates = EntityCoordinates.FromMap(MapManager.GetMapEntityId(mousePos.MapId), mousePos, EntityManager);
                }

                if (_stateManager.CurrentState is GameplayStateBase screen)
                {
                    target = screen.GetEntityUnderPosition(mousePos);
                }

                EntityManager.RaisePredictiveEvent(new DisarmAttackEvent(target, coordinates));
                return;
            }

            // Otherwise do heavy attack if it's a weapon.

            // Start a windup
            if (weapon.WindUpStart == null)
            {
                EntityManager.RaisePredictiveEvent(new StartHeavyAttackEvent(weapon.Owner));
                weapon.WindUpStart = currentTime;
            }

            // Try to do a heavy attack.
            if (useDown == BoundKeyState.Down)
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

                EntityManager.RaisePredictiveEvent(new HeavyAttackEvent(weapon.Owner, coordinates));
            }

            return;
        }

        if (weapon.WindUpStart != null)
        {
            EntityManager.RaisePredictiveEvent(new StopHeavyAttackEvent(weapon.Owner));
        }

        // Light attack
        if (useDown == BoundKeyState.Down)
        {
            if (weapon.Attacking || weapon.NextAttack > Timing.CurTime)
            {
                return;
            }

            var mousePos = _eyeManager.ScreenToMap(_inputManager.MouseScreenPosition);
            var attackerPos = Transform(entity).MapPosition;

            if (mousePos.MapId != attackerPos.MapId ||
                (attackerPos.Position - mousePos.Position).Length > weapon.Range)
            {
                return;
            }

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

            EntityUid? target = null;

            // TODO: UI Refactor update I assume
            if (_stateManager.CurrentState is GameplayStateBase screen)
            {
                target = screen.GetEntityUnderPosition(mousePos);
            }

            EntityManager.RaisePredictiveEvent(new LightAttackEvent(target, weapon.Owner, coordinates));

            return;
        }

        if (weapon.Attacking)
        {
            EntityManager.RaisePredictiveEvent(new StopAttackEvent(weapon.Owner));
        }
    }

    protected override bool DoDisarm(EntityUid user, DisarmAttackEvent ev, MeleeWeaponComponent component)
    {
        if (!base.DoDisarm(user, ev, component))
            return false;

        if (!TryComp<CombatModeComponent>(user, out var combatMode) ||
            combatMode.CanDisarm != true)
        {
            return false;
        }

        // If target doesn't have hands then we can't disarm so will let the player know it's pointless.
        if (!HasComp<HandsComponent>(ev.Target!.Value))
        {
            if (Timing.IsFirstTimePredicted && HasComp<MobStateComponent>(ev.Target.Value))
                PopupSystem.PopupEntity(Loc.GetString("disarm-action-disarmable", ("targetName", ev.Target.Value)), ev.Target.Value, Filter.Local());

            return false;
        }

        return true;
    }

    protected override void Popup(string message, EntityUid? uid, EntityUid? user)
    {
        if (!Timing.IsFirstTimePredicted || uid == null)
            return;

        PopupSystem.PopupEntity(message, uid.Value, Filter.Local());
    }

    private void OnMeleeLunge(MeleeLungeEvent ev)
    {
        // Entity might not have been sent by PVS.
        if (Exists(ev.Entity))
            DoLunge(ev.Entity, ev.Angle, ev.LocalPos, ev.Animation);
    }

    /// <summary>
    /// Does all of the melee effects for a player that are predicted, i.e. character lunge and weapon animation.
    /// </summary>
    public override void DoLunge(EntityUid user, Angle angle, Vector2 localPos, string? animation)
    {
        if (!Timing.IsFirstTimePredicted)
            return;

        var lunge = GetLungeAnimation(localPos);

        // Stop any existing lunges on the user.
        _animation.Stop(user, MeleeLungeKey);
        _animation.Play(user, lunge, MeleeLungeKey);

        // Clientside entity to spawn
        if (animation != null)
        {
            var animationUid = Spawn(animation, new EntityCoordinates(user, Vector2.Zero));

            if (localPos != Vector2.Zero && TryComp<SpriteComponent>(animationUid, out var sprite))
            {
                sprite[0].AutoAnimated = false;

                if (TryComp<WeaponArcVisualsComponent>(animationUid, out var arcComponent))
                {
                    sprite.NoRotation = true;
                    sprite.Rotation = localPos.ToWorldAngle();
                    var distance = Math.Clamp(localPos.Length / 2f, 0.2f, 1f);

                    switch (arcComponent.Animation)
                    {
                        case WeaponArcAnimation.Slash:
                            _animation.Play(animationUid, GetSlashAnimation(sprite, angle), "melee-slash");
                            break;
                        case WeaponArcAnimation.Thrust:
                            _animation.Play(animationUid, GetThrustAnimation(sprite, distance), "melee-thrust");
                            break;
                        case WeaponArcAnimation.None:
                            sprite.Offset = localPos.Normalized * distance;
                            _animation.Play(animationUid, GetStaticAnimation(sprite), "melee-fade");
                            break;
                    }
                }
            }
        }
    }

    private Animation GetSlashAnimation(SpriteComponent sprite, Angle arc)
    {
        var slashStart = 0.03f;
        var slashEnd = 0.065f;
        var length = slashEnd + 0.05f;
        var startRotation = sprite.Rotation - arc / 2;
        var endRotation = sprite.Rotation + arc / 2;
        sprite.NoRotation = true;

        return new Animation()
        {
            Length = TimeSpan.FromSeconds(length),
            AnimationTracks =
            {
                new AnimationTrackComponentProperty()
                {
                    ComponentType = typeof(SpriteComponent),
                    Property = nameof(SpriteComponent.Rotation),
                    KeyFrames =
                    {
                        new AnimationTrackProperty.KeyFrame(startRotation, 0f),
                        new AnimationTrackProperty.KeyFrame(startRotation, slashStart),
                        new AnimationTrackProperty.KeyFrame(endRotation, slashEnd)
                    }
                },
                new AnimationTrackComponentProperty()
                {
                    ComponentType = typeof(SpriteComponent),
                    Property = nameof(SpriteComponent.Offset),
                    KeyFrames =
                    {
                        new AnimationTrackProperty.KeyFrame(startRotation.RotateVec(new Vector2(0f, -1f)), 0f),
                        new AnimationTrackProperty.KeyFrame(startRotation.RotateVec(new Vector2(0f, -1f)), slashStart),
                        new AnimationTrackProperty.KeyFrame(endRotation.RotateVec(new Vector2(0f, -1f)), slashEnd)
                    }
                },
                new AnimationTrackComponentProperty()
                {
                    ComponentType = typeof(SpriteComponent),
                    Property = nameof(SpriteComponent.Color),
                    KeyFrames =
                    {
                        new AnimationTrackProperty.KeyFrame(sprite.Color, slashEnd),
                        new AnimationTrackProperty.KeyFrame(sprite.Color.WithAlpha(0f), length),
                    }
                }
            }
        };
    }

    private Animation GetThrustAnimation(SpriteComponent sprite, float distance)
    {
        var length = 0.15f;
        var thrustEnd = 0.05f;

        return new Animation()
        {
            Length = TimeSpan.FromSeconds(length),
            AnimationTracks =
            {
                new AnimationTrackComponentProperty()
                {
                    ComponentType = typeof(SpriteComponent),
                    Property = nameof(SpriteComponent.Offset),
                    KeyFrames =
                    {
                        new AnimationTrackProperty.KeyFrame(sprite.Rotation.RotateVec(new Vector2(0f, -distance / 5f)), 0f),
                        new AnimationTrackProperty.KeyFrame(sprite.Rotation.RotateVec(new Vector2(0f, -distance)), thrustEnd),
                        new AnimationTrackProperty.KeyFrame(sprite.Rotation.RotateVec(new Vector2(0f, -distance)), length),
                    }
                },
                new AnimationTrackComponentProperty()
                {
                    ComponentType = typeof(SpriteComponent),
                    Property = nameof(SpriteComponent.Color),
                    KeyFrames =
                    {
                        new AnimationTrackProperty.KeyFrame(sprite.Color, thrustEnd),
                        new AnimationTrackProperty.KeyFrame(sprite.Color.WithAlpha(0f), length),
                    }
                }
            }
        };
    }

    /// <summary>
    /// Get the fadeout for static weapon arcs.
    /// </summary>
    private Animation GetStaticAnimation(SpriteComponent sprite)
    {
        var length = 0.15f;

        return new()
        {
            Length = TimeSpan.FromSeconds(length),
            AnimationTracks =
            {
                new AnimationTrackComponentProperty()
                {
                    ComponentType = typeof(SpriteComponent),
                    Property = nameof(SpriteComponent.Color),
                    KeyFrames =
                    {
                        new AnimationTrackProperty.KeyFrame(sprite.Color, 0f),
                        new AnimationTrackProperty.KeyFrame(sprite.Color.WithAlpha(0f), length)
                    }
                }
            }
        };
    }

    /// <summary>
    /// Get the sprite offset animation to use for mob lunges.
    /// </summary>
    private Animation GetLungeAnimation(Vector2 direction)
    {
        var length = 0.1f;

        return new Animation
        {
            Length = TimeSpan.FromSeconds(length),
            AnimationTracks =
            {
                new AnimationTrackComponentProperty()
                {
                    ComponentType = typeof(SpriteComponent),
                    Property = nameof(SpriteComponent.Offset),
                    InterpolationMode = AnimationInterpolationMode.Linear,
                    KeyFrames =
                    {
                        new AnimationTrackProperty.KeyFrame(direction.Normalized * 0.15f, 0f),
                        new AnimationTrackProperty.KeyFrame(Vector2.Zero, length)
                    }
                }
            }
        };
    }
}
