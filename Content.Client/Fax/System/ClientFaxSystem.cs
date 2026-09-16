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

    private const string FaxKey = "faxecute";

    [SubscribeLocalEvent]
    private void OnAppearanceChanged(Entity<FaxMachineComponent> fax, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        if (_player.HasRunningAnimation(fax, FaxKey))
            return;

        if (!args.TryGetData(FaxMachineVisuals.VisualState, out FaxFunctions visuals))
            return;

        if ((visuals & FaxFunctions.Inserting) == FaxFunctions.Inserting)
        {
            _player.Play(fax,
                new Animation()
                {
                    Length = fax.Comp.InsertionTime,
                    AnimationTracks =
                    {
                        new AnimationTrackSpriteFlick()
                        {
                            LayerKey = FaxMachineVisuals.VisualState,
                            KeyFrames =
                            {
                                new AnimationTrackSpriteFlick.KeyFrame(fax.Comp.InsertingState, 0f)
                            },
                        },
                    },
                },
                FaxKey);
        }
    }

    protected override void NotifyAdmins(string faxName) { }
}
