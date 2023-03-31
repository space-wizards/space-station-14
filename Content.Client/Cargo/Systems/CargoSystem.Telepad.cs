using Content.Shared.Cargo;
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
        OnChangeData(args.Component, args.Sprite);
    }

    private void OnCargoAnimComplete(EntityUid uid, CargoTelepadComponent component, AnimationCompletedEvent args)
    {
        if (!TryComp<AppearanceComponent>(uid, out var appearance)) return;

        OnChangeData(appearance);
    }

    private void OnChangeData(AppearanceComponent component, SpriteComponent? sprite = null)
    {
        if (!Resolve(component.Owner, ref sprite))
            return;

        _appearance.TryGetData<CargoTelepadState?>(component.Owner, CargoTelepadVisuals.State, out var state);
        AnimationPlayerComponent? player = null;

        switch (state)
        {
            case CargoTelepadState.Teleporting:
                if (_player.HasRunningAnimation(component.Owner, TelepadBeamKey)) return;
                _player.Stop(component.Owner, player, TelepadIdleKey);
                _player.Play(component.Owner, player, CargoTelepadBeamAnimation, TelepadBeamKey);
                break;
            case CargoTelepadState.Unpowered:
                sprite.LayerSetVisible(CargoTelepadLayers.Beam, false);
                _player.Stop(component.Owner, player, TelepadBeamKey);
                _player.Stop(component.Owner, player, TelepadIdleKey);
                break;
            default:
                sprite.LayerSetVisible(CargoTelepadLayers.Beam, true);

                if (_player.HasRunningAnimation(component.Owner, player, TelepadIdleKey) ||
                    _player.HasRunningAnimation(component.Owner, player, TelepadBeamKey)) return;

                _player.Play(component.Owner, player, CargoTelepadIdleAnimation, TelepadIdleKey);
                break;
        }
    }

    private enum CargoTelepadLayers : byte
    {
        Base = 0,
        Beam = 1,
    }
}
