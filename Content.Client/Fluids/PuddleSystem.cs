using Content.Client.IconSmoothing;
using Content.Shared.Fluids;
using Content.Shared.Fluids.Components;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Utility;

namespace Content.Client.Fluids
{
    public sealed class PuddleSystem : EntitySystem
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

            if (args.AppearanceData.TryGetValue(PuddleVisuals.Evaporation, out var sparkles) && (bool) sparkles)
            {
                args.Sprite.LayerSetState(1, "sparkles", new ResourcePath("Fluids/wet_floor_sparkles.rsi"));
                args.Sprite.LayerSetVisible(1, true);
            }
            else
            {
                args.Sprite.LayerSetVisible(1, false);
            }

            float volume = 1f;

            if (args.AppearanceData.TryGetValue(PuddleVisuals.CurrentVolume, out var volumeObj))
            {
                volume = (float) volumeObj;
            }

            // Update smoothing and sprite based on volume.
            if (TryComp<IconSmoothComponent>(uid, out var smooth))
            {
                if (volume < 0.3f)
                {
                    args.Sprite.LayerSetState(0, $"{smooth.StateBase}a");
                    _smooth.SetEnabled(uid, false, smooth);
                }
                else if (volume < 0.6f)
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
            const float alpha = 0.8f;

            if (args.AppearanceData.TryGetValue(PuddleVisuals.SolutionColor, out var colorObj))
            {
                var color = (Color) colorObj;
                args.Sprite.LayerSetColor(0, color.WithAlpha(alpha) * baseColor);
            }
            else if (args.Sprite.TryGetLayer(0, out var layer))
            {
                layer.Color = layer.Color.WithAlpha(alpha) * baseColor;
            }
        }
    }
}
