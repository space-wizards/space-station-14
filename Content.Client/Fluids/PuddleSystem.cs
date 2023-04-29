using Content.Client.IconSmoothing;
using Content.Shared.Fluids;
using Content.Shared.Fluids.Components;
using Robust.Client.GameObjects;

namespace Content.Client.Fluids;

public sealed class PuddleSystem : SharedPuddleSystem
{
    [Dependency] private readonly IconSmoothSystem _smooth = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PuddleComponent, AppearanceChangeEvent>(OnPuddleAppearance);
    }

    private void OnPuddleAppearance(EntityUid uid, PuddleComponent component, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        float volume = 1f;

        if (args.AppearanceData.TryGetValue(PuddleVisuals.CurrentVolume, out var volumeObj))
        {
            volume = (float) volumeObj;
        }

        // Update smoothing and sprite based on volume.
        if (TryComp<IconSmoothComponent>(uid, out var smooth))
        {
            if (volume < LowThreshold)
            {
                args.Sprite.LayerSetState(0, $"{smooth.StateBase}a");
                _smooth.SetEnabled(uid, false, smooth);
            }
            else if (volume < MediumThreshold)
            {
                args.Sprite.LayerSetState(0, $"{smooth.StateBase}b");
                _smooth.SetEnabled(uid, false, smooth);
            }
            else
            {
                if (!smooth.Enabled)
                {
                    args.Sprite.LayerSetState(0, $"{smooth.StateBase}0");
                    _smooth.SetEnabled(uid, true, smooth);
                    _smooth.DirtyNeighbours(uid);
                }
            }
        }

        var baseColor = Color.White;

        if (args.AppearanceData.TryGetValue(PuddleVisuals.SolutionColor, out var colorObj))
        {
            var color = (Color) colorObj;
            args.Sprite.Color = color * baseColor;
        }
        else
        {
            args.Sprite.Color *= baseColor;
        }
    }
}
