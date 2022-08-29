using Content.Client.Viewport;
using Content.Shared.CombatMode;
using Content.Shared.Weapons.Melee;
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

public sealed class NewMeleeWeaponSystem : SharedNewMeleeWeaponSystem
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
        Length = TimeSpan.FromSeconds(0.3),
        AnimationTracks =
        {
            new AnimationTrackComponentProperty()
            {
                ComponentType = typeof(SpriteComponent),
                Property = nameof(SpriteComponent.Color),
                KeyFrames =
                {
                    new AnimationTrackProperty.KeyFrame(Color.Red, 0f),
                    new AnimationTrackProperty.KeyFrame(Color.White, 0.3f)
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

    protected override void DoPreciseAttack(EntityUid user, ReleasePreciseAttackEvent ev, NewMeleeWeaponComponent component)
    {
        base.DoPreciseAttack(user, ev, component);
    }

    protected override void DoWideAttack(EntityUid user, ReleaseWideAttackEvent ev, NewMeleeWeaponComponent component)
    {
        base.DoWideAttack(user, ev, component);
    }

    private void OnMeleeLunge(MeleeLungeEvent ev)
    {
        DoLunge(ev.Entity, ev.LocalPos);
    }

    protected override void DoLunge(EntityUid user, Vector2 localPos)
    {
        var animation = GetLungeAnimation(localPos);

        _animation.Stop(user, MeleeLungeKey);
        _animation.Play(user, animation, MeleeLungeKey);
    }

    private Animation GetLungeAnimation(Vector2 direction)
    {
        const float length = 0.3f;

        return new Animation
        {
            Length = TimeSpan.FromSeconds(length),
            AnimationTracks =
            {
                new AnimationTrackComponentProperty()
                {
                    ComponentType = typeof(SpriteComponent),
                    Property = nameof(SpriteComponent.Offset),
                    InterpolationMode = AnimationInterpolationMode.Cubic,
                    KeyFrames =
                    {
                        new AnimationTrackProperty.KeyFrame(direction.Normalized * 0.2f, 0f),
                        new AnimationTrackProperty.KeyFrame(Vector2.Zero, length)
                    }
                }
            }
        };
    }
}
