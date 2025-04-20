using Content.Shared.Cargo;
using Content.Shared.Cargo.Components;
using JetBrains.Annotations;
using Robust.Client.Animations;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;

namespace Content.Client.Cargo.Systems;

public sealed partial class CargoSystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

    private static readonly Animation CargoTelepadBeamAnimation = new()
    {
        Length = TimeSpan.FromSeconds(0.5),
        AnimationTracks =
        {
            new AnimationTrackSpriteFlick
            {
                LayerKey = CargoTelepadLayers.Beam,
                KeyFrames =
                {
                    new AnimationTrackSpriteFlick.KeyFrame(new RSI.StateId("beam"), 0f)
                }
            }
        }
    };

    private static readonly Animation CargoTelepadIdleAnimation = new()
    {
        Length = TimeSpan.FromSeconds(0.8),
        AnimationTracks =
        {
            new AnimationTrackSpriteFlick
            {
                LayerKey = CargoTelepadLayers.Beam,
                KeyFrames =
                {
                    new AnimationTrackSpriteFlick.KeyFrame(new RSI.StateId("idle"), 0f)
                }
            }
        }
    };

    private const string TelepadBeamKey = "cargo-telepad-beam";
    private const string TelepadIdleKey = "cargo-telepad-idle";

    private void InitializeCargoTelepad()
    {
        SubscribeLocalEvent<CargoTelepadComponent, AppearanceChangeEvent>(OnCargoAppChange);
        SubscribeLocalEvent<CargoTelepadComponent, AnimationCompletedEvent>(OnCargoAnimComplete);
    }

    private void OnCargoAppChange(EntityUid uid, CargoTelepadComponent component, ref AppearanceChangeEvent args)
    {
        OnChangeData(uid, args.Sprite);
    }

    private void OnCargoAnimComplete(EntityUid uid, CargoTelepadComponent component, AnimationCompletedEvent args)
    {
        OnChangeData(uid);
    }

    private void OnChangeData(EntityUid uid, SpriteComponent? sprite = null)
    {
        if (!Resolve(uid, ref sprite))
            return;

        if (!TryComp<AnimationPlayerComponent>(uid, out var player))
            return;

        _appearance.TryGetData<CargoTelepadState?>(uid, CargoTelepadVisuals.State, out var state);

        switch (state)
        {
            case CargoTelepadState.Teleporting:
                _player.Stop((uid, player), TelepadIdleKey);
                if (!_player.HasRunningAnimation(uid, TelepadBeamKey))
                    _player.Play((uid, player), CargoTelepadBeamAnimation, TelepadBeamKey);
                break;
            case CargoTelepadState.Unpowered:
                sprite.LayerSetVisible(CargoTelepadLayers.Beam, false);
                _player.Stop(uid, player, TelepadBeamKey);
                _player.Stop(uid, player, TelepadIdleKey);
                break;
            default:
                sprite.LayerSetVisible(CargoTelepadLayers.Beam, true);

                if (_player.HasRunningAnimation(uid, player, TelepadIdleKey) ||
                    _player.HasRunningAnimation(uid, player, TelepadBeamKey))
                    return;

                _player.Play((uid, player), CargoTelepadIdleAnimation, TelepadIdleKey);
                break;
        }
    }

    [UsedImplicitly]
    private enum CargoTelepadLayers : byte
    {
        Base = 0,
        Beam = 1,
    }
}
