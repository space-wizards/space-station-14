using System;
using System.Diagnostics;
using System.Numerics;
using System.Threading.Tasks;
using Content.Client.Animations;
using Content.Client.DisplacementMap;
using Content.Client.Gameplay;
using Content.Client.Items;
using Content.Client.Weapons.Ranged.Components;
using Content.Shared._Starlight.Effects;
using Content.Shared._Starlight.Weapon.Components;
using Content.Shared.Camera;
using Content.Shared.CombatMode;
using Content.Shared.DisplacementMap;
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
using static Content.Shared.Fax.AdminFaxEuiMsg;
using SharedGunSystem = Content.Shared.Weapons.Ranged.Systems.SharedGunSystem;
using TimedDespawnComponent = Robust.Shared.Spawners.TimedDespawnComponent;

namespace Content.Client.Weapons.Ranged.Systems;

public sealed partial class GunSystem : SharedGunSystem
{
    [Dependency] private readonly IComponentFactory _factory = default!;
    [Dependency] private readonly IEyeManager _eyeManager = default!;
    [Dependency] private readonly IInputManager _inputManager = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IStateManager _state = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly AnimationPlayerSystem _animPlayer = default!;
    [Dependency] private readonly InputSystem _inputSystem = default!;
    [Dependency] private readonly SharedCameraRecoilSystem _recoil = default!;
    [Dependency] private readonly SharedMapSystem _maps = default!;
    [Dependency] private readonly DisplacementMapSystem _displacement = default!;

    [ValidatePrototypeId<EntityPrototype>]
    public const string HitscanProto = "HitscanEffect";
    public const string ImpactProto = "ImpactEffect";
    private DisplacementEffect _displacementEffect = null!;

    public bool SpreadOverlay
    {
        get => _spreadOverlay;
        set
        {
            if (_spreadOverlay == value)
                return;

            _spreadOverlay = value;
            var overlayManager = IoCManager.Resolve<IOverlayManager>();

            if (_spreadOverlay)
            {
                overlayManager.AddOverlay(new GunSpreadOverlay(
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
                overlayManager.RemoveOverlay<GunSpreadOverlay>();
            }
        }
    }

    private bool _spreadOverlay;

    public override void Initialize()
    {
        base.Initialize();
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
        if (ev.MuzzleFlash is not null)
            RenderFlash(ev.MuzzleFlash.Value.coordinates, ev.MuzzleFlash.Value.angle, ev.MuzzleFlash.Value.Sprite, ev.MuzzleFlash.Value.Distance, false, false);
        if (ev.Bullet is not null)
            RenderBullet(ev.Bullet.Value.coordinates, ev.Bullet.Value.angle, ev.Bullet.Value.Sprite, ev.Bullet.Value.Distance);
        if (ev.TravelFlash is not null)
            RenderFlash(ev.TravelFlash.Value.coordinates, ev.TravelFlash.Value.angle, ev.TravelFlash.Value.Sprite, ev.TravelFlash.Value.Distance, true, false);
        if (ev.ImpactFlash is not null || ev.Impact is not null)
            Timer.Spawn(100, () =>
            {
                if (ev.ImpactFlash is not null)
                    RenderFlash(ev.ImpactFlash.Value.coordinates, ev.ImpactFlash.Value.angle, ev.ImpactFlash.Value.Sprite, ev.ImpactFlash.Value.Distance, false, true);
                if (ev.Impact is not null && GetEntity(ev.Impact.Value.target) is EntityUid target)
                    RenderDisplacementImpact(GetCoordinates(ev.Impact.Value.coordinates), ev.Impact.Value.angle, target);
            });
    }
    private void RenderDisplacementImpact(EntityCoordinates coords, Angle angle, EntityUid target)
    {
        if (!TryComp<SpriteComponent>(target, out var sprite))
            return;

        if (Deleted(coords.EntityId))
            return;

        if (!sprite!.AllLayers.TryFirstOrDefault(x => (x.ActualRsi ?? x.Rsi) != null && x.RsiState != null, out var layer))
            return;

        if (layer.PixelSize.X != 32 || layer.PixelSize.Y != 32)
            return;

        var ent = Spawn(ImpactProto, coords);
        var spriteComp = Comp<SpriteComponent>(ent);
        var xform = Transform(ent);
        xform.LocalRotation = angle;
        spriteComp.LayerSetRSI("unshaded", (layer!.ActualRsi ?? layer.Rsi)!);
        spriteComp.LayerSetState("unshaded", layer.RsiState);
        spriteComp["unshaded"].Visible = true;
        _displacement.TryAddDisplacement(_displacementEffect.Displacement, spriteComp, 0, "unshaded", new HashSet<string>());
    }
    private void RenderBullet(NetCoordinates coordinates, Angle angle, ExtendedSpriteSpecifier sprite, float distance)
    {
        if (sprite.Sprite is not SpriteSpecifier.Rsi rsi)
        {
            Logger.Warning("Sprite is not Rsi Type");
            return;
        }

        var coords = GetCoordinates(coordinates);

        if (Deleted(coords.EntityId))
            return;

        var ent = Spawn(HitscanProto, coords);
        var spriteComp = Comp<SpriteComponent>(ent);
        var xform = Transform(ent);
        xform.LocalRotation = angle;
        spriteComp[EffectLayers.Unshaded].AutoAnimated = false;
        spriteComp.LayerSetSprite(EffectLayers.Unshaded, rsi);
        spriteComp.LayerSetState(EffectLayers.Unshaded, rsi.RsiState);
        spriteComp.Offset = new Vector2(1f, 0f);
        spriteComp.Rotation = 1.5708f;
        spriteComp[EffectLayers.Unshaded].Visible = true;
        spriteComp.Color = sprite.SpriteColor;

        var anim = new Animation()
        {
            Length = TimeSpan.FromSeconds(0.15f),
            AnimationTracks =
                {
                    new AnimationTrackComponentProperty()
                    {
                        ComponentType = typeof(SpriteComponent),
                        Property = nameof(SpriteComponent.Offset),
                        KeyFrames =
                        {
                            new AnimationTrackProperty.KeyFrame(new Vector2(1f, 0f), 0),
                            new AnimationTrackProperty.KeyFrame(new Vector2(distance + 1.0f, 0f), 0.09f),
                        },
                        InterpolationMode = AnimationInterpolationMode.Cubic
                    }
                }
        };

        _animPlayer.Play(ent, anim, "hitscan-effect");
    }
    private void RenderFlash(NetCoordinates coordinates, Angle angle, SpriteSpecifier sprite, float distance, bool travel, bool end)
    {
        if (sprite is not SpriteSpecifier.Rsi rsi)
            return;

        var coords = GetCoordinates(coordinates);

        if (Deleted(coords.EntityId))
            return;

        var ent = Spawn(HitscanProto, coords);
        var spriteComp = Comp<SpriteComponent>(ent);
        var xform = Transform(ent);
        xform.LocalRotation = angle;
        spriteComp[EffectLayers.Unshaded].AutoAnimated = false;
        spriteComp.LayerSetSprite(EffectLayers.Unshaded, rsi);
        spriteComp.LayerSetState(EffectLayers.Unshaded, rsi.RsiState);
        if (travel)
        {
            spriteComp.Scale = new Vector2(0.05f, 0.5f);
            spriteComp.Offset = new Vector2(distance * -0.5f, 0f);
        }
        else
            spriteComp.Scale = new Vector2(1f, 0.5f);

        spriteComp[EffectLayers.Unshaded].Visible = true;

        var anim = new Animation()
        {
            Length = end ? TimeSpan.FromSeconds(0.05f)
            : TimeSpan.FromSeconds(0.15f),
            AnimationTracks =
                {
                    new AnimationTrackSpriteFlick()
                    {
                        LayerKey = EffectLayers.Unshaded,
                        KeyFrames =
                        {
                            new AnimationTrackSpriteFlick.KeyFrame(rsi.RsiState, end? 0f: 0.10f),
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
                            new AnimationTrackProperty.KeyFrame(new Vector2(0.05f, 0.5f), 0),
                            new AnimationTrackProperty.KeyFrame(new Vector2(distance, 0.5f), 0.10f),
                            new AnimationTrackProperty.KeyFrame(new Vector2(distance, 0.5f), 0.15f),
                        },
                InterpolationMode = AnimationInterpolationMode.Cubic
            });
            anim.AnimationTracks.Add(new AnimationTrackComponentProperty()
            {
                ComponentType = typeof(SpriteComponent),
                Property = nameof(SpriteComponent.Offset),
                KeyFrames =
                        {
                            new AnimationTrackProperty.KeyFrame(new Vector2(distance * -0.5f, 0f), 0),
                            new AnimationTrackProperty.KeyFrame(new Vector2(0, 0f), 0.10f),
                            new AnimationTrackProperty.KeyFrame(new Vector2(0, 0f), 0.15f),
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
                EntityManager.RaisePredictiveEvent(new RequestStopShootEvent { Gun = GetNetEntity(gunUid) });
            return;
        }

        if (gun.NextFire > Timing.CurTime)
            return;

        var mousePos = _eyeManager.PixelToMap(_inputManager.MouseScreenPosition);

        if (mousePos.MapId == MapId.Nullspace)
        {
            if (gun.ShotCounter != 0)
                EntityManager.RaisePredictiveEvent(new RequestStopShootEvent { Gun = GetNetEntity(gunUid) });

            return;
        }

        // Define target coordinates relative to gun entity, so that network latency on moving grids doesn't fuck up the target location.
        var coordinates = TransformSystem.ToCoordinates(entity, mousePos);

        NetEntity? target = null;
        if (_state.CurrentState is GameplayStateBase screen)
            target = GetNetEntity(screen.GetClickedEntity(mousePos));

        Log.Debug($"Sending shoot request tick {Timing.CurTick} / {Timing.CurTime}");

        EntityManager.RaisePredictiveEvent(new RequestShootEvent
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
            light = (PointLightComponent)_factory.GetComponent(typeof(PointLightComponent));
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
