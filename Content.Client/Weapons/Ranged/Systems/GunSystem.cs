using System.Numerics;
using Content.Client.Animations;
using Content.Client.DisplacementMap;
using Content.Client.Gameplay;
using Content.Client.Items;
using Content.Client.Weapons.Ranged.Components;
using Content.Shared._Starlight.Effects;
using Content.Shared._Starlight.Weapon.Components;
using Content.Shared.Camera;
using Content.Shared.CombatMode;
using Content.Shared.Mech.Components;
using Content.Shared.Weapons.Ranged;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Weapons.Ranged.Systems;
using Content.Shared.Starlight.Utility;
using Robust.Client.Animations;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.Player;
using Robust.Client.State;
using Robust.Shared.Animations;
using Robust.Shared.Input;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using SharedGunSystem = Content.Shared.Weapons.Ranged.Systems.SharedGunSystem;
using TimedDespawnComponent = Robust.Shared.Spawners.TimedDespawnComponent;
using Robust.Shared.Configuration;
using Content.Shared.Starlight.CCVar;

namespace Content.Client.Weapons.Ranged.Systems;

// There’ve been so many radical changes here that you can basically consider the entire file as being under the Starlight folder now.
public sealed partial class GunSystem : SharedGunSystem
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IComponentFactory _factory = default!;
    [Dependency] private readonly IEyeManager _eyeManager = default!;
    [Dependency] private readonly IInputManager _inputManager = default!;
    [Dependency] private readonly IOverlayManager _overlayManager = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IStateManager _state = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly AnimationPlayerSystem _animPlayer = default!;
    [Dependency] private readonly InputSystem _inputSystem = default!;
    [Dependency] private readonly SharedCameraRecoilSystem _recoil = default!;
    [Dependency] private readonly SharedMapSystem _maps = default!;
    [Dependency] private readonly SharedTransformSystem _xform = default!;
    [Dependency] private readonly DisplacementMapSystem _displacement = default!;
    [Dependency] private readonly SpriteSystem _sprite = default!;

    public static readonly EntProtoId HitscanProto = "HitscanEffect";
    public const string ImpactProto = "ImpactEffect";
    private DisplacementEffect _displacementEffect = null!;
    private bool _tracesEnabled = true;
    public override void Shutdown()
    {
        base.Shutdown();
        _cfg.UnsubValueChanged(StarlightCCVars.TracesEnabled, OnTracesEnabledChanged);
    }
    private void OnTracesEnabledChanged(bool tracesEnabled)
        => _tracesEnabled = tracesEnabled;

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
        _cfg.OnValueChanged(StarlightCCVars.TracesEnabled, OnTracesEnabledChanged, true);

        UpdatesOutsidePrediction = true;
        SubscribeLocalEvent<AmmoCounterComponent, ItemStatusCollectMessage>(OnAmmoCounterCollect);
        SubscribeLocalEvent<AmmoCounterComponent, UpdateClientAmmoEvent>(OnUpdateClientAmmo);
        SubscribeAllEvent<MuzzleFlashEvent>(OnMuzzleFlash);

        // Plays animated effects on the client.
        SubscribeNetworkEvent<HitscanEvent>(OnHitscan);

        InitializeMagazineVisuals();
        InitializeSpentAmmo();

        _displacementEffect = _proto.Index<DisplacementEffect>("displacementEffect");
    }

    private void OnUpdateClientAmmo(EntityUid uid, AmmoCounterComponent ammoComp, ref UpdateClientAmmoEvent args)
    {
        UpdateAmmoCount(uid, ammoComp);
    }

    private void OnMuzzleFlash(MuzzleFlashEvent args)
    {
        var gunUid = GetEntity(args.Uid);

        CreateEffect(gunUid, args, gunUid);
    }

    private void OnHitscan(HitscanEvent ev)
    {
        var hitscan = _proto.Index(ev.Hitscan);
        //The real bullet speed is so high that the bullet isn’t visible at all. So, let's slow it down 5x.
        var bulletSpeed = hitscan.Speed / 5000;
        foreach (var effects in ev.Effects)
        {
            var delay = 0f;
            foreach (var effect in effects)
                delay = FireEffect(hitscan, bulletSpeed, delay, effect);
        }
    }

    private float FireEffect(HitscanPrototype hitscan, float bulletSpeed, float delay, Effect effect)
    {
        var length = effect.Distance / bulletSpeed;
        if (effect.MuzzleCoordinates is { } muzzleCoordinates)
        {
            if (hitscan.MuzzleFlash is { } mozzle && (_tracesEnabled || hitscan.Bullet is null))
                RenderFlash(muzzleCoordinates, effect.Angle, mozzle, 1f, false, false, length, delay);

            if (hitscan.Bullet is { } bullet)
                RenderBullet(muzzleCoordinates, effect.Angle, bullet, effect.Distance - 1.5f, length, delay);
        }
        if (hitscan.TravelFlash is { } travel && effect.TravelCoordinates is { } travelCoordinates && (_tracesEnabled || hitscan.Bullet is null))
            RenderFlash(travelCoordinates, effect.Angle, travel, effect.Distance - 1.5f, true, false, length, delay);
        delay += length;

        if ((hitscan.ImpactFlash is not null || effect.ImpactEnt is not null) && (_tracesEnabled || hitscan.Bullet is null))
            Timer.Spawn((int)delay, () =>
            {
                if (hitscan.ImpactFlash is { } impact)
                    RenderFlash(effect.ImpactCoordinates, effect.Angle, impact, 1f, false, true, length, delay);

                if (effect.ImpactEnt is { } netEnt && GetEntity(netEnt) is EntityUid ent)
                    RenderDisplacementImpact(GetCoordinates(effect.ImpactCoordinates), effect.Angle, ent);
            });
        return delay;
    }

    private void RenderDisplacementImpact(EntityCoordinates coords, Angle angle, EntityUid target)
    {
        if (!TryComp<SpriteComponent>(target, out var sprite))
            return;

        if (!TryComp(coords.EntityId, out TransformComponent? relativeXform))
            return;

        if (!sprite!.AllLayers.TryFirstOrDefault(x => (x.ActualRsi ?? x.Rsi) != null && x.RsiState != null, out var layer))
            return;

        if (layer.PixelSize.X != 32 || layer.PixelSize.Y != 32)
            return;

        var ent = Spawn(ImpactProto, coords);
        var spriteComp = Comp<SpriteComponent>(ent);

        var xform = Transform(ent);
        var targetWorldRot = angle + _xform.GetWorldRotation(relativeXform);
        var delta = targetWorldRot - _xform.GetWorldRotation(xform);
        _xform.SetLocalRotationNoLerp(ent, xform.LocalRotation + delta, xform);

        _sprite.LayerSetRsi((ent, spriteComp), "unshaded", (layer!.ActualRsi ?? layer.Rsi)!);
        _sprite.LayerSetRsiState((ent, spriteComp), "unshaded", layer.RsiState);
        spriteComp["unshaded"].Visible = true;
        _displacement.TryAddDisplacement(_displacementEffect.Displacement, (ent, spriteComp), 0, "unshaded", out _);
    }
    private void RenderBullet(NetCoordinates coordinates, Angle angle, ExtendedSpriteSpecifier sprite, float distance, float length, float delay)
    {
        if (sprite.Sprite is not SpriteSpecifier.Rsi rsi)
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
        spriteComp[EffectLayers.Unshaded].Visible = true;
        _sprite.LayerSetSprite(spriteEnt, EffectLayers.Unshaded, rsi);
        _sprite.LayerSetRsiState(spriteEnt, EffectLayers.Unshaded, rsi.RsiState);
        _sprite.SetOffset(spriteEnt, new Vector2(1f, 0f));
        _sprite.SetRotation(spriteEnt, 1.5708f);
        _sprite.SetColor(spriteEnt, sprite.SpriteColor);
        _sprite.SetVisible(spriteEnt, delay == 0);

        var time = delay + length;

        var despawn = Comp<TimedDespawnComponent>(ent);
        despawn.Lifetime = (time / 1000) + 1000;

        if (delay != 0)
            Timer.Spawn((int)delay, () =>
            {
                if (TryComp(ent, out spriteComp))
                    _sprite.SetVisible((ent, spriteComp), true);
            });

        Timer.Spawn((int)time, () =>
        {
            if (TryComp(ent, out spriteComp))
                _sprite.SetVisible((ent, spriteComp), false);
        });

        var anim = new Animation()
        {
            Length = TimeSpan.FromMilliseconds(time),
            AnimationTracks =
                {
                    new AnimationTrackComponentProperty()
                    {
                        ComponentType = typeof(SpriteComponent),
                        Property = nameof(SpriteComponent.Offset),
                        KeyFrames =
                        {
                            new AnimationTrackProperty.KeyFrame(new Vector2(1f, 0f), delay / 1000),
                            new AnimationTrackProperty.KeyFrame(new Vector2(distance + 1.0f, 0f), time / 1000),
                            new AnimationTrackProperty.KeyFrame(new Vector2(distance + 1.0f, 0f), (time + 1000) / 1000),
                        },
                        InterpolationMode = AnimationInterpolationMode.Linear
                    }
                }
        };

        _animPlayer.Play(ent, anim, "hitscan-effect");
    }
    private void RenderFlash(NetCoordinates coordinates, Angle angle, SpriteSpecifier sprite, float distance, bool travel, bool end, float length, float delay)
    {
        if (end) length = 0;
        var time = delay + length + 100;

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
            _sprite.SetScale(spriteEnt, new Vector2(1f, 0.5f));

        spriteComp[EffectLayers.Unshaded].Visible = true;

        var despawn = Comp<TimedDespawnComponent>(ent);
        despawn.Lifetime = (time / 1000) + 1000;

        if (delay != 0)
            Timer.Spawn((int)delay, () => spriteComp.Visible = true);

        Timer.Spawn((int)time, () =>
        {
            if (!Deleted(ent))
                _sprite.SetVisible(spriteEnt, false);
        });

        var anim = new Animation()
        {
            Length = TimeSpan.FromMilliseconds(time),
            AnimationTracks =
                {
                    new AnimationTrackSpriteFlick()
                    {
                        LayerKey = EffectLayers.Unshaded,
                        KeyFrames =
                        {
                            new AnimationTrackSpriteFlick.KeyFrame(rsi.RsiState, (time - 100) / 1000),
                        }
                    }
                }
        };

        if (travel)
        {
            anim.AnimationTracks.Add(new AnimationTrackComponentProperty()
            {
                ComponentType = typeof(SpriteComponent),
                Property = nameof(SpriteComponent.Scale),
                KeyFrames =
                        {
                            new AnimationTrackProperty.KeyFrame(new Vector2(0.05f, 0.5f), delay / 1000),
                            new AnimationTrackProperty.KeyFrame(new Vector2(distance, 0.5f), (time - 100) / 1000),
                            new AnimationTrackProperty.KeyFrame(new Vector2(distance, 0.5f), time / 1000),
                        },
                InterpolationMode = AnimationInterpolationMode.Cubic
            });
            anim.AnimationTracks.Add(new AnimationTrackComponentProperty()
            {
                ComponentType = typeof(SpriteComponent),
                Property = nameof(SpriteComponent.Offset),
                KeyFrames =
                        {
                            new AnimationTrackProperty.KeyFrame(new Vector2(distance * -0.5f, 0f), delay / 1000),
                            new AnimationTrackProperty.KeyFrame(new Vector2(0, 0f), (time - 100) / 1000),
                            new AnimationTrackProperty.KeyFrame(new Vector2(0, 0f), time / 1000),
                        },
                InterpolationMode = AnimationInterpolationMode.Cubic
            });
        }

        _animPlayer.Play(ent, anim, "hitscan-effect");
    }

    public override void Update(float frameTime)
    {
        if (!Timing.IsFirstTimePredicted)
            return;

        var entityNull = _player.LocalEntity;

        if (entityNull == null || !TryComp<CombatModeComponent>(entityNull, out var combat) || !combat.IsInCombatMode)
        {
            return;
        }

        var entity = entityNull.Value;

        if (TryComp<MechPilotComponent>(entity, out var mechPilot))
        {
            entity = mechPilot.Mech;
        }

        if (!TryGetGun(entity, out var gunUid, out var gun))
        {
            return;
        }

        var useKey = gun.UseKey ? EngineKeyFunctions.Use : EngineKeyFunctions.UseSecondary;

        if (_inputSystem.CmdStates.GetState(useKey) != BoundKeyState.Down && !gun.BurstActivated)
        {
            if (gun.ShotCounter != 0)
                RaisePredictiveEvent(new RequestStopShootEvent { Gun = GetNetEntity(gunUid) });
            return;
        }

        if (gun.NextFire > Timing.CurTime)
            return;

        var mousePos = _eyeManager.PixelToMap(_inputManager.MouseScreenPosition);

        if (mousePos.MapId == MapId.Nullspace)
        {
            if (gun.ShotCounter != 0)
                RaisePredictiveEvent(new RequestStopShootEvent { Gun = GetNetEntity(gunUid) });

            return;
        }

        // Define target coordinates relative to gun entity, so that network latency on moving grids doesn't fuck up the target location.
        var coordinates = TransformSystem.ToCoordinates(entity, mousePos);

        NetEntity? target = null;
        if (_state.CurrentState is GameplayStateBase screen)
            target = GetNetEntity(screen.GetClickedEntity(mousePos));

        Log.Debug($"Sending shoot request tick {Timing.CurTick} / {Timing.CurTime}");

        RaisePredictiveEvent(new RequestShootEvent
        {
            Target = target,
            Coordinates = GetNetCoordinates(coordinates),
            Gun = GetNetEntity(gunUid),
        });
    }

    public override void Shoot(EntityUid gunUid, GunComponent gun, List<(EntityUid? Entity, IShootable Shootable)> ammo,
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
                Recoil(user, direction, gun.CameraRecoilScalarModified);
                if (IsClientSide(ent!.Value))
                    Del(ent.Value);
                else
                    RemoveShootable(ent.Value);
                continue;
            }

            switch (shootable)
            {
                //🌟Starlight🌟
                case HitScanCartridgeAmmoComponent cartridge:
                    if (!cartridge.Spent)
                    {
                        SetCartridgeSpent(ent!.Value, cartridge, true);
                        MuzzleFlash(gunUid, cartridge, worldAngle, user);
                        Audio.PlayPredicted(gun.SoundGunshotModified, gunUid, user);
                        Recoil(user, direction, gun.CameraRecoilScalarModified);
                    }
                    else
                    {
                        userImpulse = false;
                        Audio.PlayPredicted(gun.SoundEmpty, gunUid, user);
                    }

                    if (IsClientSide(ent!.Value))
                        Del(ent.Value);

                    break;

                case CartridgeAmmoComponent cartridge:
                    if (!cartridge.Spent)
                    {
                        SetCartridgeSpent(ent!.Value, cartridge, true);
                        MuzzleFlash(gunUid, cartridge, worldAngle, user);
                        Audio.PlayPredicted(gun.SoundGunshotModified, gunUid, user);
                        Recoil(user, direction, gun.CameraRecoilScalarModified);
                        // TODO: Can't predict entity deletions.
                        //if (cartridge.DeleteOnSpawn)
                        //    Del(cartridge.Owner);
                    }
                    else
                    {
                        userImpulse = false;
                        Audio.PlayPredicted(gun.SoundEmpty, gunUid, user);
                    }

                    if (IsClientSide(ent!.Value))
                        Del(ent.Value);

                    break;
                case AmmoComponent newAmmo:
                    MuzzleFlash(gunUid, newAmmo, worldAngle, user);
                    Audio.PlayPredicted(gun.SoundGunshotModified, gunUid, user);
                    Recoil(user, direction, gun.CameraRecoilScalarModified);
                    if (IsClientSide(ent!.Value))
                        Del(ent.Value);
                    else
                        RemoveShootable(ent.Value);
                    break;
                case HitscanPrototype:
                    Audio.PlayPredicted(gun.SoundGunshotModified, gunUid, user);
                    Recoil(user, direction, gun.CameraRecoilScalarModified);
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
}
