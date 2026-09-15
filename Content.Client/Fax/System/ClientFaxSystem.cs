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
    [Dependency] private SharedAppearanceSystem _appearance = default!;

    private static readonly string FaxKey = "faxecute";

    [SubscribeLocalEvent]
    private void OnAppearanceChanged(Entity<FaxMachineComponent> entity, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null || !Timing.IsFirstTimePredicted)
            return;

        if (_player.HasRunningAnimation(entity, FaxKey))
            return;

        if (_appearance.TryGetData(entity, FaxMachineVisuals.VisualState, out FaxFunctions visuals) &&
            (visuals & FaxFunctions.Inserting) == FaxFunctions.Inserting)
        {
            _player.Play(entity,
                new Animation()
                {
                    Length = TimeSpan.FromSeconds(2.4),
                    AnimationTracks =
                    {
                        new AnimationTrackSpriteFlick()
                        {
                            LayerKey = FaxMachineVisuals.VisualState,
                            KeyFrames =
                            {
                                new AnimationTrackSpriteFlick.KeyFrame(entity.Comp.InsertingState, 0f)
                            },
                        },
                    },
                },
                FaxKey);
        }
    }

    protected override void NotifyAdmins(string faxName)
    {

    }
}
