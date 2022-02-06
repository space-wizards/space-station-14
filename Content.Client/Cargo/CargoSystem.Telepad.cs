using System;
using Content.Shared.Cargo;
using Robust.Client.Animations;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Content.Client.Cargo;

public sealed partial class CargoSystem
{
    [Dependency] private readonly AnimationPlayerSystem _player = default!;

    private static readonly Animation CargoTelepadAnimation = new()
    {
        Length = TimeSpan.FromSeconds(0.5),
        AnimationTracks =
        {
            new AnimationTrackSpriteFlick
            {
                LayerKey = CargoTelepadLayers.Base,
                KeyFrames =
                {
                    new AnimationTrackSpriteFlick.KeyFrame(new RSI.StateId("beam"), 0f)
                }
            }
        }
    };

    private void InitializeCargoTelepad()
    {
        SubscribeLocalEvent<CargoTelepadComponent, AppearanceChangeEvent>(OnCargoAppChange);
        SubscribeLocalEvent<CargoTelepadComponent, AnimationCompletedEvent>(OnCargoAnimComplete);
    }

    private void OnCargoAppChange(EntityUid uid, CargoTelepadComponent component, AppearanceChangeEvent args)
    {
        OnChangeData(args.Component);
    }

    private void OnCargoAnimComplete(EntityUid uid, CargoTelepadComponent component, AnimationCompletedEvent args)
    {
        if (!TryComp<AppearanceComponent>(uid, out var appearance)) return;

        OnChangeData(appearance);
    }

    private void OnChangeData(AppearanceComponent component)
    {

        if (!TryComp<SpriteComponent>(component.Owner, out var sprite)) return;

        component.TryGetData(CargoTelepadVisuals.State, out CargoTelepadState? state);

        switch (state)
        {
            case CargoTelepadState.Teleporting:
                _player.Play(component.Owner, CargoTelepadAnimation, "cargo-telepad");
                break;
            case CargoTelepadState.Unpowered:
                sprite.LayerSetVisible(CargoTelepadLayers.Beam, false);
                break;
            default:
                sprite.LayerSetVisible(CargoTelepadLayers.Beam, true);
                break;
        }
    }

    private enum CargoTelepadLayers : byte
    {
        Base = 0,
        Beam = 1,
    }
}
