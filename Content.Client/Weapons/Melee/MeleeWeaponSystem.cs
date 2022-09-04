using Content.Client.Viewport;
using Content.Client.Weapons.Melee.Components;
using Content.Shared.CombatMode;
using Content.Shared.Weapons.Melee;
using Content.Shared.Weapons.Melee.Events;
using Robust.Client.Animations;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.Player;
using Robust.Client.State;
using Robust.Shared.Animations;
using Robust.Shared.Input;
using Robust.Shared.Map;
using Robust.Shared.Player;

namespace Content.Client.Weapons.Melee;

public sealed partial class MeleeWeaponSystem : SharedMeleeWeaponSystem
{
    [Dependency] private readonly IEyeManager _eyeManager = default!;
    [Dependency] private readonly IInputManager _inputManager = default!;
    [Dependency] private readonly IOverlayManager _overlayManager = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IStateManager _stateManager = default!;
    [Dependency] private readonly AnimationPlayerSystem _animation = default!;
    [Dependency] private readonly InputSystem _inputSystem = default!;

    private const string MeleeLungeKey = "melee-lunge";
    private const string MeleeEffectKey = "melee-effect";

    /// <summary>
    /// Plays the red effect whenever the server confirms something is hit
    /// </summary>
    private static readonly Animation MeleeEffectAnimation = new()
    {
        Length = TimeSpan.FromSeconds(0.15),
        AnimationTracks =
        {
            new AnimationTrackComponentProperty()
            {
                ComponentType = typeof(SpriteComponent),
                Property = nameof(SpriteComponent.Color),
                KeyFrames =
                {
                    new AnimationTrackProperty.KeyFrame(Color.Red, 0f),
                    new AnimationTrackProperty.KeyFrame(Color.White, 0.15f)
                }
            }
        }
    };

    public override void Initialize()
    {
        base.Initialize();
        _overlayManager.AddOverlay(new MeleeWindupOverlay());
        SubscribeNetworkEvent<MeleeEffectEvent>(OnMeleeEffect);
        SubscribeNetworkEvent<MeleeLungeEvent>(OnMeleeLunge);
    }

    private void OnMeleeEffect(MeleeEffectEvent msg)
    {
        foreach (var ent in msg.HitEntities)
        {
            if (Deleted(ent))
                continue;

            _animation.Stop(ent, MeleeEffectKey);
            _animation.Play(ent, MeleeEffectAnimation, MeleeEffectKey);
        }
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

        if (!combatMode.IsInCombatMode ||
            !Blocker.CanAttack(entityNull.Value))
        {
            if (weapon.WindupAccumulator > 0f)
            {
                EntityManager.RaisePredictiveEvent(new StopAttackEvent
                {
                    Weapon = weapon.Owner,
                });
            }

            return;
        }

        if (_inputSystem.CmdStates.GetState(EngineKeyFunctions.Use) != BoundKeyState.Down)
        {
            if (weapon.WindupAccumulator > 0f)
            {
                // Active + windupaccumulator handled in the event handlers.

                if (weapon.WindupAccumulator < AttackBuffer)
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
                            target = screen.GetEntityUnderPosition(mousePos);
                        }

                        EntityManager.RaisePredictiveEvent(new ReleasePreciseAttackEvent()
                        {
                            Weapon = weapon.Owner,
                            Target = target ?? EntityUid.Invalid,
                            Coordinates = coordinates,
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
        if (!weapon.Active)
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

    private void OnMeleeLunge(MeleeLungeEvent ev)
    {
        DoLunge(ev.Entity, ev.LocalPos, ev.Animation);
    }

    /// <summary>
    /// Does all of the melee effects for a player that are predicted, i.e. character lunge and weapon animation.
    /// </summary>
    public override void DoLunge(EntityUid user, Vector2 localPos, string? animation)
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
                            _animation.Play(animationUid, GetSlashAnimation(sprite), "melee-slash");
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

    private Animation GetSlashAnimation(SpriteComponent sprite)
    {
        // TODO: Weapon based arc
        var arc = Angle.FromDegrees(45);
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
