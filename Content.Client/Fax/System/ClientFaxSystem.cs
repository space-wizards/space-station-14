using Robust.Client.GameObjects;
using Content.Shared.Fax.Components;
using Content.Shared.Fax;
using Robust.Client.Animations;

namespace Content.Client.Fax.System;

/// <summary>
/// Visualizer for the fax machine which displays the correct sprite based on the inserted entity.
/// </summary>
public sealed partial class ClientFaxSystem : FaxSystem
{
    [Dependency] private AnimationPlayerSystem _player = default!;
    [Dependency] private SpriteSystem _sprite = default!;

    [SubscribeLocalEvent]
    private void OnAppearanceChanged(Entity<FaxVisualsComponent> fax, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        if (!args.TryGetData(FaxMachineVisuals.VisualState, out FaxFunctions visuals))
            return;

        if (!FaxQuery.TryComp(fax, out var faxComp))
            return;

        foreach (var function in Enum.GetValues<FaxFunctions>())
        {
            if (!_sprite.LayerMapTryGet((fax.Owner, args.Sprite), function, out var index, false))
                continue;

            _sprite.LayerSetVisible((fax, args.Sprite), index, (visuals & function) == function);
        }

        // Next do animations
        if ((visuals & FaxFunctions.Inserting) == FaxFunctions.Inserting && !_player.HasRunningAnimation(fax, nameof(FaxFunctions.Inserting)))
        {
            if (!args.TryGetData(FaxMachineVisuals.Inserting, out string? state))
                state = fax.Comp.InsertingState;

            _player.Play(fax,
                new Animation
                {
                    Length = faxComp.InsertionTime,
                    AnimationTracks =
                    {
                        new AnimationTrackSpriteFlick
                        {
                            LayerKey = FaxFunctions.Inserting,
                            KeyFrames =
                            {
                                new AnimationTrackSpriteFlick.KeyFrame(state, 0f)
                            },
                        },
                    },
                },
                nameof(FaxFunctions.Inserting));
        }

        if ((visuals & FaxFunctions.Printing) == FaxFunctions.Printing && !_player.HasRunningAnimation(fax, nameof(FaxFunctions.Printing)))
        {
            _player.Play(fax,
                new Animation
                {
                    Length = faxComp.PrintingTime,
                    AnimationTracks =
                    {
                        new AnimationTrackSpriteFlick
                        {
                            LayerKey = FaxFunctions.Printing,
                            KeyFrames =
                            {
                                new AnimationTrackSpriteFlick.KeyFrame(fax.Comp.PrintingState, 0f)
                            },
                        },
                    },
                },
                nameof(FaxFunctions.Printing));
        }
    }

    protected override void NotifyAdmins(string faxName) { }
}
