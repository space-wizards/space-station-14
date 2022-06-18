using Content.Client.Items;
using Content.Client.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Client.Animations;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.Player;
using Robust.Shared.Audio;
using Robust.Shared.Input;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using SharedGunSystem = Content.Shared.Weapons.Ranged.Systems.SharedGunSystem;

namespace Content.Client.Weapons.Ranged.Systems;

public sealed partial class GunSystem : SharedGunSystem
{
    [Dependency] private readonly IEyeManager _eyeManager = default!;
    [Dependency] private readonly IInputManager _inputManager = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly AnimationPlayerSystem _animPlayer = default!;
    [Dependency] private readonly EffectSystem _effects = default!;
    [Dependency] private readonly InputSystem _inputSystem = default!;

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
                    IoCManager.Resolve<IEyeManager>(),
                    IoCManager.Resolve<IGameTiming>(),
                    IoCManager.Resolve<IInputManager>(),
                    IoCManager.Resolve<IPlayerManager>(),
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

        // Plays animated effects on the client.
        SubscribeNetworkEvent<HitscanEvent>(OnHitscan);

        InitializeMagazineVisuals();
        InitializeSpentAmmo();
    }

    private void OnHitscan(HitscanEvent ev)
    {
        // ALL I WANT IS AN ANIMATED EFFECT
        foreach (var a in ev.Sprites)
        {
            if (a.Sprite is not SpriteSpecifier.Rsi rsi) continue;

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
            Gun = gun.Owner,
        });
    }

    public override void Shoot(GunComponent gun, List<IShootable> ammo, EntityCoordinates fromCoordinates, EntityCoordinates toCoordinates, EntityUid? user = null)
    {
        // Rather than splitting client / server for every ammo provider it's easier
        // to just delete the spawned entities. This is for programmer sanity despite the wasted perf.
        // This also means any ammo specific stuff can be grabbed as necessary.
        foreach (var ent in ammo)
        {
            switch (ent)
            {
                case CartridgeAmmoComponent cartridge:
                    if (!cartridge.Spent)
                    {
                        SetCartridgeSpent(cartridge, true);
                        MuzzleFlash(gun.Owner, cartridge, user);
                        PlaySound(gun.Owner, gun.SoundGunshot?.GetSound(Random, ProtoManager), user);
                        // TODO: Can't predict entity deletions.
                        //if (cartridge.DeleteOnSpawn)
                        //    Del(cartridge.Owner);
                    }
                    else
                    {
                        PlaySound(gun.Owner, gun.SoundEmpty?.GetSound(Random, ProtoManager), user);
                    }

                    if (cartridge.Owner.IsClientSide())
                        Del(cartridge.Owner);

                    break;
                case AmmoComponent newAmmo:
                    MuzzleFlash(gun.Owner, newAmmo, user);
                    PlaySound(gun.Owner, gun.SoundGunshot?.GetSound(Random, ProtoManager), user);
                    if (newAmmo.Owner.IsClientSide())
                        Del(newAmmo.Owner);
                    else
                        RemComp<AmmoComponent>(newAmmo.Owner);
                    break;
                case HitscanPrototype:
                    PlaySound(gun.Owner, gun.SoundGunshot?.GetSound(Random, ProtoManager), user);
                    break;
            }
        }
    }

    protected override void PlaySound(EntityUid gun, string? sound, EntityUid? user = null)
    {
        if (sound == null || user == null || !Timing.IsFirstTimePredicted) return;
        SoundSystem.Play(sound, Filter.Local(), gun);
    }

    protected override void Popup(string message, EntityUid? uid, EntityUid? user)
    {
        if (uid == null || user == null || !Timing.IsFirstTimePredicted) return;
        PopupSystem.PopupEntity(message, uid.Value, Filter.Entities(user.Value));
    }

    protected override void CreateEffect(EffectSystemMessage message, EntityUid? user = null)
    {
        _effects.CreateEffect(message);
    }
}
