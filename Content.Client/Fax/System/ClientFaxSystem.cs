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

        if (!args.TryGetData(FaxMachineVisuals.VisualState, out FaxState visuals))
            return;

        if (!FaxQuery.TryComp(fax, out var faxComp))
            return;

        foreach (var function in Enum.GetValues<FaxState>())
        {
            if (!_sprite.LayerMapTryGet((fax.Owner, args.Sprite), function, out var index, false))
                continue;

            _sprite.LayerSetVisible((fax, args.Sprite), index, (visuals & function) == function);
        }

        // Don't play insert animation if we're not inserting
        if ((visuals & FaxState.Inserting) == 0)
        {
            _player.Stop(fax.Owner, nameof(FaxState.Inserting));
        } // Start animation if we weren't playing one.
        else if (!_player.HasRunningAnimation(fax, nameof(FaxState.Inserting)))
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
                            LayerKey = FaxState.Inserting,
                            KeyFrames =
                            {
                                new AnimationTrackSpriteFlick.KeyFrame(state, 0f)
                            },
                        },
                    },
                },
                nameof(FaxState.Inserting));
        }

        // Don't play print animation if we're not inserting
        if ((visuals & FaxState.Printing) == 0)
        {
            _player.Stop(fax.Owner, nameof(FaxState.Printing));
        } // Start animation if we weren't playing one.
        else if (!_player.HasRunningAnimation(fax, nameof(FaxState.Printing)))
        {
            _player.Play(fax,
                new Animation
                {
                    Length = faxComp.PrintingTime,
                    AnimationTracks =
                    {
                        new AnimationTrackSpriteFlick
                        {
                            LayerKey = FaxState.Printing,
                            KeyFrames =
                            {
                                new AnimationTrackSpriteFlick.KeyFrame(fax.Comp.PrintingState, 0f)
                            },
                        },
                    },
                },
                nameof(FaxState.Printing));
        }
    }

    protected override void NotifyAdmins(string faxName) { }
}
