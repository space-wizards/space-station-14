using Content.Client.Items;
using Content.Client.Weapons.Ranged.Components;
using Content.Shared.Camera;
using Content.Shared.Spawners.Components;
using Content.Shared.Weapons.Ranged;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Client.Animations;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.Player;
using Robust.Shared.Animations;
using Robust.Shared.Audio;
using Robust.Shared.Input;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Utility;
using SharedGunSystem = Content.Shared.Weapons.Ranged.Systems.SharedGunSystem;

namespace Content.Client.Weapons.Ranged.Systems;

public sealed partial class GunSystem : SharedGunSystem
{
    [Dependency] private readonly IEyeManager _eyeManager = default!;
    [Dependency] private readonly IInputManager _inputManager = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly AnimationPlayerSystem _animPlayer = default!;
    [Dependency] private readonly InputSystem _inputSystem = default!;
    [Dependency] private readonly SharedCameraRecoilSystem _recoil = default!;

    public bool SpreadOverlay
    {
        get => _spreadOverlay;
        set
        {
            if (_spreadOverlay == value) return;
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
                    this));
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
        SubscribeAllEvent<MuzzleFlashEvent>(OnMuzzleFlash);

        // Plays animated effects on the client.
        SubscribeNetworkEvent<HitscanEvent>(OnHitscan);

        InitializeMagazineVisuals();
        InitializeSpentAmmo();
    }

    private void OnMuzzleFlash(MuzzleFlashEvent args)
    {
        CreateEffect(args.Uid, args);
    }

    private void OnHitscan(HitscanEvent ev)
    {
        // ALL I WANT IS AN ANIMATED EFFECT
        foreach (var a in ev.Sprites)
        {
            if (a.Sprite is not SpriteSpecifier.Rsi rsi ||
                Deleted(a.coordinates.EntityId))
            {
                continue;
            }

            var ent = Spawn("HitscanEffect", a.coordinates);
            var sprite = Comp<SpriteComponent>(ent);
            var xform = Transform(ent);
            xform.LocalRotation = a.angle;
            sprite[EffectLayers.Unshaded].AutoAnimated = false;
            sprite.LayerSetSprite(EffectLayers.Unshaded, rsi);
            sprite.LayerSetState(EffectLayers.Unshaded, rsi.RsiState);
            sprite.Scale = new Vector2(a.Distance, 1f);
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

            _animPlayer.Play(ent, null, anim, "hitscan-effect");
        }
    }

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
        var gun = GetGun(entity);

        if (gun == null)
        {
            return;
        }

        if (_inputSystem.CmdStates.GetState(EngineKeyFunctions.Use) != BoundKeyState.Down)
        {
            if (gun.ShotCounter != 0)
                EntityManager.RaisePredictiveEvent(new RequestStopShootEvent { Gun = gun.Owner });
            return;
        }

        if (gun.NextFire > Timing.CurTime)
            return;

        var mousePos = _eyeManager.ScreenToMap(_inputManager.MouseScreenPosition);

        if (mousePos.MapId == MapId.Nullspace)
        {
            if (gun.ShotCounter != 0)
                EntityManager.RaisePredictiveEvent(new RequestStopShootEvent { Gun = gun.Owner });

            return;
        }

        // Define target coordinates relative to gun entity, so that network latency on moving grids doesn't fuck up the target location.
        var coordinates = EntityCoordinates.FromMap(entity, mousePos, EntityManager);

        Sawmill.Debug($"Sending shoot request tick {Timing.CurTick} / {Timing.CurTime}");

        EntityManager.RaisePredictiveEvent(new RequestShootEvent
        {
            Coordinates = coordinates,
            Gun = gun.Owner,
        });
    }

    public override void Shoot(GunComponent gun, List<IShootable> ammo, EntityCoordinates fromCoordinates, EntityCoordinates toCoordinates, EntityUid? user = null)
    {
        // Rather than splitting client / server for every ammo provider it's easier
        // to just delete the spawned entities. This is for programmer sanity despite the wasted perf.
        // This also means any ammo specific stuff can be grabbed as necessary.
        var direction = fromCoordinates.ToMapPos(EntityManager) - toCoordinates.ToMapPos(EntityManager);

        foreach (var ent in ammo)
        {
            switch (ent)
            {
                case CartridgeAmmoComponent cartridge:
                    if (!cartridge.Spent)
                    {
                        SetCartridgeSpent(cartridge, true);
                        MuzzleFlash(gun.Owner, cartridge, user);
                        Audio.PlayPredicted(gun.SoundGunshot, gun.Owner, user);
                        Recoil(user, direction);
                        // TODO: Can't predict entity deletions.
                        //if (cartridge.DeleteOnSpawn)
                        //    Del(cartridge.Owner);
                    }
                    else
                    {
                        Audio.PlayPredicted(gun.SoundEmpty, gun.Owner, user);
                    }

                    if (cartridge.Owner.IsClientSide())
                        Del(cartridge.Owner);

                    break;
                case AmmoComponent newAmmo:
                    MuzzleFlash(gun.Owner, newAmmo, user);
                    Audio.PlayPredicted(gun.SoundGunshot, gun.Owner, user);
                    Recoil(user, direction);
                    if (newAmmo.Owner.IsClientSide())
                        Del(newAmmo.Owner);
                    else
                        RemComp<AmmoComponent>(newAmmo.Owner);
                    break;
                case HitscanPrototype:
                    Audio.PlayPredicted(gun.SoundGunshot, gun.Owner, user);
                    Recoil(user, direction);
                    break;
            }
        }
    }

    private void Recoil(EntityUid? user, Vector2 recoil)
    {
        if (!Timing.IsFirstTimePredicted || user == null || recoil == Vector2.Zero) return;
        _recoil.KickCamera(user.Value, recoil.Normalized * 0.5f);
    }

    protected override void Popup(string message, EntityUid? uid, EntityUid? user)
    {
        if (uid == null || user == null || !Timing.IsFirstTimePredicted) return;
        PopupSystem.PopupEntity(message, uid.Value, user.Value);
    }

    protected override void CreateEffect(EntityUid uid, MuzzleFlashEvent message, EntityUid? user = null)
    {
        if (!Timing.IsFirstTimePredicted)
            return;

        EntityCoordinates coordinates;

        if (message.MatchRotation)
            coordinates = new EntityCoordinates(uid, Vector2.Zero);
        else if (TryComp<TransformComponent>(uid, out var xform))
            coordinates = xform.Coordinates;
        else
            return;

        var ent = Spawn(message.Prototype, coordinates);

        var effectXform = Transform(ent);
        effectXform.LocalRotation -= MathF.PI / 2;
        effectXform.LocalPosition += new Vector2(0f, -0.5f);

        var lifetime = 0.4f;

        if (TryComp<TimedDespawnComponent>(uid, out var despawn))
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
        var light = EnsureComp<PointLightComponent>(uid);

        light.NetSyncEnabled = false;
        light.Enabled = true;
        light.Color = Color.FromHex("#cc8e2b");
        light.Radius = 2f;
        light.Energy = 5f;

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
                    Property = nameof(PointLightComponent.Enabled),
                    InterpolationMode = AnimationInterpolationMode.Linear,
                    KeyFrames =
                    {
                        new AnimationTrackProperty.KeyFrame(true, 0),
                        new AnimationTrackProperty.KeyFrame(false, lifetime)
                    }
                }
            }
        };

        var uidPlayer = EnsureComp<AnimationPlayerComponent>(uid);

        _animPlayer.Stop(uid, uidPlayer, "muzzle-flash-light");
        _animPlayer.Play(uid, uidPlayer, animTwo,"muzzle-flash-light");
    }
}
