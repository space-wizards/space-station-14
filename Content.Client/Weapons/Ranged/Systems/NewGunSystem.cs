using Content.Client.Items;
using Content.Shared.Weapons.Ranged;
using Robust.Client.Animations;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.Player;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.Input;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Utility;

namespace Content.Client.Weapons.Ranged;

public sealed partial class NewGunSystem : SharedNewGunSystem
{
    [Dependency] private readonly IEyeManager _eyeManager = default!;
    [Dependency] private readonly IInputManager _inputManager = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly AnimationPlayerSystem _animPlayer = default!;
    [Dependency] private readonly EffectSystem _effects = default!;
    [Dependency] private readonly InputSystem _inputSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        UpdatesOutsidePrediction = true;
        SubscribeLocalEvent<AmmoCounterComponent, ItemStatusCollectMessage>(OnAmmoCounterCollect);
        SubscribeNetworkEvent<HitscanEvent>(OnHitscan);
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
                EntityManager.RaisePredictiveEvent(new RequestStopShootEvent() { Gun = gun.Owner });
            return;
        }

        if (gun.NextFire > Timing.CurTime)
            return;

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

        Sawmill.Debug($"Sending shoot request tick {Timing.CurTick} / {Timing.CurTime}");

        EntityManager.RaisePredictiveEvent(new RequestShootEvent()
        {
            Coordinates = coordinates,
            Gun = gun.Owner,
        });
    }

    public override void Shoot(EntityUid gun, List<IShootable> ammo, EntityCoordinates fromCoordinates, EntityCoordinates toCoordinates, EntityUid? user = null)
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
                        if (TryComp<AppearanceComponent>(cartridge.Owner, out var appearance))
                            appearance.SetData(AmmoVisuals.Spent, true);

                        cartridge.Spent = true;
                        MuzzleFlash(gun, cartridge, user);

                        if (cartridge.DeleteOnSpawn)
                            Del(cartridge.Owner);
                    }
                    else
                    {
                        PlaySound(gun, Comp<NewGunComponent>(gun).SoundEmpty?.GetSound(), user);
                    }

                    if (cartridge.Owner.IsClientSide())
                        Del(cartridge.Owner);

                    break;
                case NewAmmoComponent newAmmo:
                    MuzzleFlash(gun, newAmmo, user);
                    if (newAmmo.Owner.IsClientSide())
                        Del(newAmmo.Owner);
                    else
                        RemComp<NewAmmoComponent>(newAmmo.Owner);
                    break;
            }
        }
    }

    protected override void PlaySound(EntityUid gun, string? sound, EntityUid? user = null)
    {
        if (sound == null || user == null || !Timing.IsFirstTimePredicted) return;
        SoundSystem.Play(Filter.Local(), sound, gun);
    }

    protected override void Popup(string message, NewGunComponent? gun, EntityUid? user)
    {
        if (gun == null || user == null || !Timing.IsFirstTimePredicted) return;
        PopupSystem.PopupEntity(message, gun.Owner, Filter.Entities(user.Value));
    }

    protected override void CreateEffect(EffectSystemMessage message, EntityUid? user = null)
    {
        _effects.CreateEffect(message);
    }
}
