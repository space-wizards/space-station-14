using System;
using Content.Shared.Cargo;
using Robust.Client.Animations;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Content.Client.Cargo;

public class CargoTelepadVisualizer : AppearanceVisualizer
{
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

    public override void OnChangeData(AppearanceComponent component)
    {
        base.OnChangeData(component);

        var entManager = IoCManager.Resolve<IEntityManager>();

        if (!entManager.TryGetComponent<SpriteComponent>(component.Owner, out var sprite)) return;

        component.TryGetData(CargoTelepadVisuals.State, out CargoTelepadState? state);

        switch (state)
        {
            case CargoTelepadState.Teleporting:
                EntitySystem.Get<AnimationPlayerSystem>().Play(component.Owner, CargoTelepadAnimation, "cargo-telepad");
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
