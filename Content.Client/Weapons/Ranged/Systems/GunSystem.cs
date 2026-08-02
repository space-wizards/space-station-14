using System.Numerics;
using Content.Client.Animations;
using Content.Client.Gameplay;
using Content.Client.Items;
using Content.Client.Weapons.Ranged.Components;
using Content.Shared.Camera;
using Content.Shared.CCVar;
using Content.Shared.CombatMode;
using Content.Shared.Damage;
using Content.Shared.Weapons.Hitscan.Components;
using Content.Shared.Weapons.Hitscan.Events;
using Content.Shared.Weapons.Ranged;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Client.Animations;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.Player;
using Robust.Client.State;
using Robust.Shared.Animations;
using Robust.Shared.Audio;
using Robust.Shared.Configuration;
using Robust.Shared.Input;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using SharedGunSystem = Content.Shared.Weapons.Ranged.Systems.SharedGunSystem;
using TimedDespawnComponent = Robust.Shared.Spawners.TimedDespawnComponent;

namespace Content.Client.Weapons.Ranged.Systems;

public sealed partial class GunSystem : SharedGunSystem
{
    [Dependency] private readonly AnimationPlayerSystem _animPlayer = default!;
    [Dependency] private readonly IEyeManager _eyeManager = default!;
    [Dependency] private readonly IInputManager _inputManager = default!;
    [Dependency] private readonly InputSystem _inputSystem = default!;
    [Dependency] private readonly IOverlayManager _overlayManager = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IStateManager _state = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly SharedCameraRecoilSystem _recoil = default!;
    [Dependency] private readonly SharedMapSystem _maps = default!;
    [Dependency] private readonly SharedTransformSystem _xform = default!;
    [Dependency] private readonly SpriteSystem _sprite = default!;

    public static readonly EntProtoId HitscanProto = "HitscanEffect";

    public bool SpreadOverlay
    {
        get => _spreadOverlay;
        set
        {
            if (_spreadOverlay == value)
                return;

            _spreadOverlay = value;

            if (_spreadOverlay)
            {
                _overlayManager.AddOverlay(new GunSpreadOverlay(
                    EntityManager,
                    _eyeManager,
                    Timing,
                    _inputManager,
                    _player,
                    this,
                    TransformSystem));
            }
            else
            {
                _overlayManager.RemoveOverlay<GunSpreadOverlay>();
            }
        }
    }

    private bool _spreadOverlay;

    public override void Initialize()
    {
        base.Initialize();
        UpdatesOutsidePrediction = true;
        SubscribeLocalEvent<AmmoCounterComponent, ItemStatusCollectMessage>(OnAmmoCounterCollect);
        SubscribeAllEvent<MuzzleFlashEvent>(OnMuzzleFlash);

        // Plays animated effects on the client.
        SubscribeNetworkEvent<HitscanEvent>(OnHitscan);

        InitializeMagazineVisuals();
        InitializeSpentAmmo();
    }


    private void OnMuzzleFlash(MuzzleFlashEvent args)
    {
        var gunUid = GetEntity(args.Uid);

        CreateEffect(gunUid, args, gunUid);
    }

    private void OnHitscan(HitscanEvent ev)
    {
        // DS14-start: animated hitscan traces.
        if (ev.Sprites.Count != 0)
        {
            RenderLegacyHitscanSprites(ev.Sprites);
            return;
        }

        var delay = 0f;
        foreach (var trace in ev.Traces)
        {
            delay = FireHitscanEffect(ev, delay, trace);
        }
        // DS14-end
    }

    // DS14-start: animated hitscan traces.
    private float FireHitscanEffect(HitscanEvent visuals, float delay, HitscanTrace trace)
    {
        var speed = MathF.Max(visuals.Speed, 1f);
        var length = trace.Distance / (speed / 5000f);

        if (trace.MuzzleCoordinates is { } muzzleCoordinates)
        {
            if (visuals.MuzzleFlash is { } muzzle)
                RenderHitscanFlash(muzzleCoordinates, trace.Angle, muzzle, 1f, false, false, length, delay);

            if (visuals.Bullet is { } bullet)
                RenderHitscanBullet(muzzleCoordinates, trace.Angle, bullet, visuals.BulletLight, MathF.Max(trace.Distance - 1.5f, 0f), length, delay);
        }

        if (visuals.TravelFlash is { } travel &&
            trace.TravelCoordinates is { } travelCoordinates &&
            trace.Distance > 1.5f)
        {
            RenderHitscanFlash(travelCoordinates, trace.Angle, travel, MathF.Max(trace.Distance - 1.5f, 0f), true, false, length, delay);
        }

        delay += length;

        if (visuals.ImpactFlash is { } impact)
            RenderHitscanFlash(trace.ImpactCoordinates, trace.Angle, impact, 1f, false, true, length, delay);

        return delay;
    }

    private void RenderLegacyHitscanSprites(List<(NetCoordinates coordinates, Angle angle, SpriteSpecifier Sprite, float Distance)> sprites)
    {
        foreach (var spriteData in sprites)
        {
            if (spriteData.Sprite is not SpriteSpecifier.Rsi rsi)
                continue;

            var coords = GetCoordinates(spriteData.coordinates);

            if (!TryComp(coords.EntityId, out TransformComponent? relativeXform))
                continue;

            var ent = Spawn(HitscanProto, coords);
            var sprite = Comp<SpriteComponent>(ent);

            var xform = Transform(ent);
            var targetWorldRot = spriteData.angle + _xform.GetWorldRotation(relativeXform);
            var delta = targetWorldRot - _xform.GetWorldRotation(xform);
            _xform.SetLocalRotationNoLerp(ent, xform.LocalRotation + delta, xform);

            sprite[EffectLayers.Unshaded].AutoAnimated = false;
            _sprite.LayerSetSprite((ent, sprite), EffectLayers.Unshaded, rsi);
            _sprite.LayerSetRsiState((ent, sprite), EffectLayers.Unshaded, rsi.RsiState);
            _sprite.SetScale((ent, sprite), new Vector2(spriteData.Distance, 1f));
            sprite[EffectLayers.Unshaded].Visible = true;

            var anim = new Animation()
            {
                Length = TimeSpan.FromSeconds(0.48f),
                AnimationTracks =
                {
                    new AnimationTrackSpriteFlick()
                    {
                        LayerKey = EffectLayers.Unshaded,
                        KeyFrames =
                        {
                            new AnimationTrackSpriteFlick.KeyFrame(rsi.RsiState, 0f),
                        }
                    }
                }
            };

            _animPlayer.Play(ent, anim, "hitscan-effect");
        }
    }

    private void RenderHitscanBullet(NetCoordinates coordinates, Angle angle, ExtendedSpriteSpecifier sprite, HitscanLightVisual? lightVisual, float distance, float length, float delay)
    {
        if (sprite.Sprite is not SpriteSpecifier.Rsi rsi)
            return;

        var coords = GetCoordinates(coordinates);

        if (!TryComp(coords.EntityId, out TransformComponent? relativeXform))
            return;

        var ent = Spawn(HitscanProto, coords);
        if (lightVisual != null)
            SetupHitscanBulletLight(ent, lightVisual, new Vector2(1f, 0f), delay == 0f);

        var spriteComp = Comp<SpriteComponent>(ent);
        var spriteEnt = (ent, spriteComp);

        var xform = Transform(ent);
        var targetWorldRot = angle + _xform.GetWorldRotation(relativeXform);
        var delta = targetWorldRot - _xform.GetWorldRotation(xform);
        _xform.SetLocalRotationNoLerp(ent, xform.LocalRotation + delta, xform);

        spriteComp[EffectLayers.Unshaded].AutoAnimated = false;
        spriteComp[EffectLayers.Unshaded].Visible = true;
        _sprite.LayerSetSprite(spriteEnt, EffectLayers.Unshaded, rsi);
        _sprite.LayerSetRsiState(spriteEnt, EffectLayers.Unshaded, rsi.RsiState);
        _sprite.SetOffset(spriteEnt, new Vector2(1f, 0f));
        _sprite.SetScale(spriteEnt, sprite.SpriteScale);
        _sprite.SetRotation(spriteEnt, sprite.SpriteRotation ? 1.5708f : 0f);
        _sprite.SetColor(spriteEnt, sprite.SpriteColor);
        _sprite.SetVisible(spriteEnt, delay == 0f);

        var time = delay + length;
        var despawn = EnsureComp<TimedDespawnComponent>(ent);
        despawn.Lifetime = (time / 1000f) + 0.25f;

        if (delay != 0f)
        {
            Timer.Spawn((int) delay, () =>
            {
                if (TryComp(ent, out SpriteComponent? currentSprite))
                    _sprite.SetVisible((ent, currentSprite), true);

                if (TryComp(ent, out PointLightComponent? currentLight))
                    Lights.SetEnabled(ent, true, currentLight);
            });
        }

        Timer.Spawn((int) time, () =>
        {
            if (TryComp(ent, out SpriteComponent? currentSprite))
                _sprite.SetVisible((ent, currentSprite), false);

            if (TryComp(ent, out PointLightComponent? currentLight))
                Lights.SetEnabled(ent, false, currentLight);
        });

        var anim = new Animation
        {
            Length = TimeSpan.FromMilliseconds(time),
            AnimationTracks =
            {
                new AnimationTrackComponentProperty
                {
                    ComponentType = typeof(SpriteComponent),
                    Property = nameof(SpriteComponent.Offset),
                    InterpolationMode = AnimationInterpolationMode.Linear,
                    KeyFrames =
                    {
                        new AnimationTrackProperty.KeyFrame(new Vector2(1f, 0f), delay / 1000f),
                        new AnimationTrackProperty.KeyFrame(new Vector2(distance + 1f, 0f), time / 1000f),
                    }
                }
            }
        };

        // DS14-start: keep optional projectile light on the moving bullet visual.
        if (lightVisual != null)
        {
            anim.AnimationTracks.Add(new AnimationTrackComponentProperty
            {
                ComponentType = typeof(PointLightComponent),
                Property = nameof(PointLightComponent.Offset),
                InterpolationMode = AnimationInterpolationMode.Linear,
                KeyFrames =
                {
                    new AnimationTrackProperty.KeyFrame(lightVisual.Offset + new Vector2(1f, 0f), delay / 1000f),
                    new AnimationTrackProperty.KeyFrame(lightVisual.Offset + new Vector2(distance + 1f, 0f), time / 1000f),
                }
            });
        }
        // DS14-end

        _animPlayer.Play(ent, anim, "hitscan-effect");
    }

    private SharedPointLightComponent SetupHitscanBulletLight(EntityUid uid, HitscanLightVisual lightVisual, Vector2 offset, bool enabled)
    {
        var light = Lights.EnsureLight(uid);
        light.Offset = lightVisual.Offset + offset;
        Lights.SetEnabled(uid, enabled, light);
        Lights.SetColor(uid, lightVisual.Color, light);
        Lights.SetRadius(uid, lightVisual.Radius, light);
        Lights.SetEnergy(uid, lightVisual.Energy, light);
        Lights.SetSoftness(uid, lightVisual.Softness, light);
        Lights.SetFalloff(uid, lightVisual.Falloff, light);
        Lights.SetCurveFactor(uid, lightVisual.CurveFactor, light);
        Lights.SetCastShadows(uid, lightVisual.CastShadows, light);
        return light;
    }

    private void RenderHitscanFlash(NetCoordinates coordinates, Angle angle, SpriteSpecifier sprite, float distance, bool travel, bool end, float length, float delay)
    {
        if (end)
            length = 0f;

        var time = delay + length + 100f;

        if (sprite is not SpriteSpecifier.Rsi rsi)
            return;

        var coords = GetCoordinates(coordinates);

        if (!TryComp(coords.EntityId, out TransformComponent? relativeXform))
            return;

        var ent = Spawn(HitscanProto, coords);
        var spriteComp = Comp<SpriteComponent>(ent);
        var spriteEnt = (ent, spriteComp);

        var xform = Transform(ent);
        var targetWorldRot = angle + _xform.GetWorldRotation(relativeXform);
        var delta = targetWorldRot - _xform.GetWorldRotation(xform);
        _xform.SetLocalRotationNoLerp(ent, xform.LocalRotation + delta, xform);

        spriteComp[EffectLayers.Unshaded].AutoAnimated = false;
        _sprite.LayerSetSprite(spriteEnt, EffectLayers.Unshaded, rsi);
        _sprite.LayerSetRsiState(spriteEnt, EffectLayers.Unshaded, rsi.RsiState);

        if (travel)
        {
            _sprite.SetScale(spriteEnt, new Vector2(0.05f, 0.5f));
            _sprite.SetOffset(spriteEnt, new Vector2(distance * -0.5f, 0f));
        }
        else
        {
            _sprite.SetScale(spriteEnt, new Vector2(1f, 0.5f));
        }

        spriteComp[EffectLayers.Unshaded].Visible = true;

        var despawn = EnsureComp<TimedDespawnComponent>(ent);
        despawn.Lifetime = (time / 1000f) + 0.25f;

        if (delay != 0f)
        {
            _sprite.SetVisible(spriteEnt, false);
            Timer.Spawn((int) delay, () =>
            {
                if (TryComp(ent, out SpriteComponent? currentSprite))
                    _sprite.SetVisible((ent, currentSprite), true);
            });
        }

        Timer.Spawn((int) time, () =>
        {
            if (TryComp(ent, out SpriteComponent? currentSprite))
                _sprite.SetVisible((ent, currentSprite), false);
        });

        var anim = new Animation
        {
            Length = TimeSpan.FromMilliseconds(time),
            AnimationTracks =
            {
                new AnimationTrackSpriteFlick
                {
                    LayerKey = EffectLayers.Unshaded,
                    KeyFrames =
                    {
                        new AnimationTrackSpriteFlick.KeyFrame(rsi.RsiState, (time - 100f) / 1000f),
                    }
                }
            }
        };

        if (travel)
        {
            anim.AnimationTracks.Add(new AnimationTrackComponentProperty
            {
                ComponentType = typeof(SpriteComponent),
                Property = nameof(SpriteComponent.Scale),
                InterpolationMode = AnimationInterpolationMode.Cubic,
                KeyFrames =
                {
                    new AnimationTrackProperty.KeyFrame(new Vector2(0.05f, 0.5f), delay / 1000f),
                    new AnimationTrackProperty.KeyFrame(new Vector2(distance, 0.5f), (time - 100f) / 1000f),
                    new AnimationTrackProperty.KeyFrame(new Vector2(distance, 0.5f), time / 1000f),
                }
            });
            anim.AnimationTracks.Add(new AnimationTrackComponentProperty
            {
                ComponentType = typeof(SpriteComponent),
                Property = nameof(SpriteComponent.Offset),
                InterpolationMode = AnimationInterpolationMode.Cubic,
                KeyFrames =
                {
                    new AnimationTrackProperty.KeyFrame(new Vector2(distance * -0.5f, 0f), delay / 1000f),
                    new AnimationTrackProperty.KeyFrame(new Vector2(0f, 0f), (time - 100f) / 1000f),
                    new AnimationTrackProperty.KeyFrame(new Vector2(0f, 0f), time / 1000f),
                }
            });
        }

        _animPlayer.Play(ent, anim, "hitscan-effect");
    }
    // DS14-end

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (!Timing.IsFirstTimePredicted)
            return;

        var entityNull = _player.LocalEntity;

        if (entityNull == null || !TryComp<CombatModeComponent>(entityNull, out var combat) || !combat.IsInCombatMode)
        {
            return;
        }

        var entity = entityNull.Value;

        if (!TryGetGun(entity, out var gun))
        {
            return;
        }

        var useKey = gun.Comp.UseKey ? EngineKeyFunctions.Use : EngineKeyFunctions.UseSecondary;

        if (_inputSystem.CmdStates.GetState(useKey) != BoundKeyState.Down && !gun.Comp.BurstActivated)
        {
            if (gun.Comp.ShotCounter != 0)
                RaisePredictiveEvent(new RequestStopShootEvent { Gun = GetNetEntity(gun) });
            return;
        }

        if (gun.Comp.NextFire > Timing.CurTime)
            return;

        var mousePos = _eyeManager.PixelToMap(_inputManager.MouseScreenPosition);

        if (mousePos.MapId == MapId.Nullspace)
        {
            if (gun.Comp.ShotCounter != 0)
                RaisePredictiveEvent(new RequestStopShootEvent { Gun = GetNetEntity(gun) });

            return;
        }

        // Keep aim coordinates in the map/grid frame instead of the player's frame.
        // The player rotates in combat mode, so player-relative aim can be replayed by the
        // server with a different rotation under latency and send shots sideways or backwards.
        EntityCoordinates coordinates;
        if (MapManager.TryFindGridAt(mousePos, out var gridUid, out _))
        {
            coordinates = TransformSystem.ToCoordinates(gridUid, mousePos);
        }
        else
        {
            coordinates = TransformSystem.ToCoordinates(_maps.GetMap(mousePos.MapId), mousePos);
        }

        NetEntity? target = null;
        if (_state.CurrentState is GameplayStateBase screen)
            target = GetNetEntity(screen.GetClickedEntity(mousePos));

        Log.Debug($"Sending shoot request tick {Timing.CurTick} / {Timing.CurTime}");


        RaisePredictiveEvent(new RequestShootEvent
        {
            Target = target,
            Coordinates = GetNetCoordinates(coordinates),
            Gun = GetNetEntity(gun),
            Continuous = _cfg.GetCVar(CCVars.ControlHoldToAttackRanged),
        });
    }

    public override void Shoot(Entity<GunComponent> gun, List<(EntityUid? Entity, IShootable Shootable)> ammo,
        EntityCoordinates fromCoordinates, EntityCoordinates toCoordinates, out bool userImpulse, EntityUid? user = null, bool throwItems = false)
    {
        userImpulse = true;

        // Rather than splitting client / server for every ammo provider it's easier
        // to just delete the spawned entities. This is for programmer sanity despite the wasted perf.
        // This also means any ammo specific stuff can be grabbed as necessary.
        var direction = TransformSystem.ToMapCoordinates(fromCoordinates).Position - TransformSystem.ToMapCoordinates(toCoordinates).Position;
        var worldAngle = direction.ToAngle().Opposite();

        foreach (var (ent, shootable) in ammo)
        {
            if (throwItems)
            {
                Recoil(user, direction, gun.Comp.CameraRecoilScalarModified);
                if (IsClientSide(ent!.Value))
                    Del(ent.Value);
                else
                    RemoveShootable(ent.Value);
                continue;
            }

            // TODO: Clean this up in a gun refactor at some point - too much copy pasting
            switch (shootable)
            {
                case CartridgeAmmoComponent cartridge:
                    if (!cartridge.Spent)
                    {
                        SetCartridgeSpent(ent!.Value, cartridge, true);
                        MuzzleFlash(gun, cartridge, worldAngle, user);
                        Audio.PlayPredicted(gun.Comp.SoundGunshotModified, gun, user);
                        Recoil(user, direction, gun.Comp.CameraRecoilScalarModified);
                        // TODO: Can't predict entity deletions.
                        //if (cartridge.DeleteOnSpawn)
                        //    Del(cartridge.Owner);
                    }
                    else
                    {
                        userImpulse = false;
                        Audio.PlayPredicted(gun.Comp.SoundEmpty, gun, user);
                    }

                    if (IsClientSide(ent!.Value))
                        Del(ent.Value);

                    break;
                case AmmoComponent newAmmo:
                    MuzzleFlash(gun, newAmmo, worldAngle, user);
                    Audio.PlayPredicted(gun.Comp.SoundGunshotModified, gun, user);
                    Recoil(user, direction, gun.Comp.CameraRecoilScalarModified);
                    if (IsClientSide(ent!.Value))
                        Del(ent.Value);
                    else
                        RemoveShootable(ent.Value);
                    break;
                case HitscanAmmoComponent:
                    Audio.PlayPredicted(gun.Comp.SoundGunshotModified, gun, user);
                    Recoil(user, direction, gun.Comp.CameraRecoilScalarModified);
                    break;
            }
        }
    }

    private void Recoil(EntityUid? user, Vector2 recoil, float recoilScalar)
    {
        if (!Timing.IsFirstTimePredicted || user == null || recoil == Vector2.Zero || recoilScalar == 0)
            return;

        _recoil.KickCamera(user.Value, recoil.Normalized() * 0.5f * recoilScalar);
    }

    protected override void Popup(string message, EntityUid? uid, EntityUid? user)
    {
        if (uid == null || user == null || !Timing.IsFirstTimePredicted)
            return;

        PopupSystem.PopupEntity(message, uid.Value, user.Value);
    }

    protected override void CreateEffect(EntityUid gunUid, MuzzleFlashEvent message, EntityUid? tracked = null)
    {
        if (!Timing.IsFirstTimePredicted)
            return;

        // EntityUid check added to stop throwing exceptions due to https://github.com/space-wizards/space-station-14/issues/28252
        // TODO: Check to see why invalid entities are firing effects.
        if (gunUid == EntityUid.Invalid)
        {
            Log.Debug($"Invalid Entity sent MuzzleFlashEvent (proto: {message.Prototype}, gun: {ToPrettyString(gunUid)})");
            return;
        }

        var gunXform = Transform(gunUid);
        var gridUid = gunXform.GridUid;
        EntityCoordinates coordinates;

        if (TryComp(gridUid, out MapGridComponent? mapGrid))
        {
            coordinates = new EntityCoordinates(gridUid.Value, _maps.LocalToGrid(gridUid.Value, mapGrid, gunXform.Coordinates));
        }
        else if (gunXform.MapUid != null)
        {
            coordinates = new EntityCoordinates(gunXform.MapUid.Value, TransformSystem.GetWorldPosition(gunXform));
        }
        else
        {
            return;
        }

        var ent = Spawn(message.Prototype, coordinates);
        TransformSystem.SetWorldRotationNoLerp(ent, message.Angle);

        if (tracked != null)
        {
            var track = EnsureComp<TrackUserComponent>(ent);
            track.User = tracked;
            track.Offset = Vector2.UnitX / 2f;
        }

        var lifetime = 0.4f;

        if (TryComp<TimedDespawnComponent>(gunUid, out var despawn))
        {
            lifetime = despawn.Lifetime;
        }

        var anim = new Animation()
        {
            Length = TimeSpan.FromSeconds(lifetime),
            AnimationTracks =
            {
                new AnimationTrackComponentProperty
                {
                    ComponentType = typeof(SpriteComponent),
                    Property = nameof(SpriteComponent.Color),
                    InterpolationMode = AnimationInterpolationMode.Linear,
                    KeyFrames =
                    {
                        new AnimationTrackProperty.KeyFrame(Color.White.WithAlpha(1f), 0),
                        new AnimationTrackProperty.KeyFrame(Color.White.WithAlpha(0f), lifetime)
                    }
                }
            }
        };

        _animPlayer.Play(ent, anim, "muzzle-flash");
        if (!TryComp(gunUid, out PointLightComponent? light))
        {
            light = Factory.GetComponent<PointLightComponent>();
            light.NetSyncEnabled = false;
            AddComp(gunUid, light);
        }

        Lights.SetEnabled(gunUid, true, light);
        Lights.SetRadius(gunUid, 2f, light);
        Lights.SetColor(gunUid, Color.FromHex("#cc8e2b"), light);
        Lights.SetEnergy(gunUid, 5f, light);

        var animTwo = new Animation()
        {
            Length = TimeSpan.FromSeconds(lifetime),
            AnimationTracks =
            {
                new AnimationTrackComponentProperty
                {
                    ComponentType = typeof(PointLightComponent),
                    Property = nameof(PointLightComponent.Energy),
                    InterpolationMode = AnimationInterpolationMode.Linear,
                    KeyFrames =
                    {
                        new AnimationTrackProperty.KeyFrame(5f, 0),
                        new AnimationTrackProperty.KeyFrame(0f, lifetime)
                    }
                },
                new AnimationTrackComponentProperty
                {
                    ComponentType = typeof(PointLightComponent),
                    Property = nameof(PointLightComponent.AnimatedEnable),
                    InterpolationMode = AnimationInterpolationMode.Linear,
                    KeyFrames =
                    {
                        new AnimationTrackProperty.KeyFrame(true, 0),
                        new AnimationTrackProperty.KeyFrame(false, lifetime)
                    }
                }
            }
        };

        var uidPlayer = EnsureComp<AnimationPlayerComponent>(gunUid);

        _animPlayer.Stop(gunUid, uidPlayer, "muzzle-flash-light");
        _animPlayer.Play((gunUid, uidPlayer), animTwo, "muzzle-flash-light");
    }

    // TODO: Move RangedDamageSoundComponent to shared so this can be predicted.
    public override void PlayImpactSound(EntityUid otherEntity, DamageSpecifier? modifiedDamage, SoundSpecifier? weaponSound, bool forceWeaponSound) { }
}
